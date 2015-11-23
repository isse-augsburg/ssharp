// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.Runtime.Serialization
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Text;
	using Analysis;
	using Analysis.FormulaVisitors;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Serializes a <see cref="RuntimeModel" /> instance into a <see cref="Stream" />.
	/// </summary>
	internal static class RuntimeModelSerializer
	{
		#region Serialization

		/// <summary>
		///   Saves the serialized <paramref name="model" /> and the <paramref name="formulas" /> to the <paramref name="stream" />.
		/// </summary>
		/// <param name="stream">The stream the serialized specification should be written to.</param>
		/// <param name="model">The model that should be serialized into the <paramref name="stream" />.</param>
		/// <param name="formulas">The formulas that should be serialized into the <paramref name="stream" />.</param>
		public static void Save(Stream stream, Model model, params Formula[] formulas)
		{
			Requires.NotNull(stream, nameof(stream));
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
				SerializeModel(writer, model, formulas);
		}

		/// <summary>
		///   Serializes the <paramref name="model" />.
		/// </summary>
		private static unsafe void SerializeModel(BinaryWriter writer, Model model, Formula[] formulas)
		{
			var stateFormulas = CollectStateFormulas(formulas);
			var objectTable = CreateObjectTable(model, stateFormulas);

			// Prepare the serialization of the model's initial state
			var slotCount = SerializationRegistry.Default.GetStateSlotCount(objectTable, SerializationMode.Full);
			var serializer = SerializationRegistry.Default.CreateStateSerializer(objectTable, SerializationMode.Full);

			// Serialize the object table
			SerializeObjectTable(objectTable, writer);

			// Serialize object identifiers of the root components
			writer.Write(model.Count);
			foreach (var root in model)
				writer.Write(objectTable.GetObjectIdentifier(root));

			// Serialize the initial state
			var serializedState = stackalloc int[slotCount];
			serializer(serializedState);

			// Copy the serialized state to the stream
			writer.Write(slotCount);
			for (var i = 0; i < slotCount; ++i)
				writer.Write(serializedState[i]);

			SerializeFormulas(writer, objectTable, stateFormulas);
		}

		/// <summary>
		///   Serializes the <paramref name="stateFormulas" />.
		/// </summary>
		private static void SerializeFormulas(BinaryWriter writer, ObjectTable objectTable, StateFormula[] stateFormulas)
		{
			writer.Write(stateFormulas.Length);
			foreach (var formula in stateFormulas)
			{
				// Serialize the object identifier of the closure as well as the method name
				writer.Write(objectTable.GetObjectIdentifier(formula.Expression.Target));
				writer.Write(formula.Expression.Method.Name);

				// Serialize the state label name
				writer.Write(formula.Label);
			}
		}

		/// <summary>
		///   Collects all state formulas contained in the <paramref name="formulas" />.
		/// </summary>
		private static StateFormula[] CollectStateFormulas(Formula[] formulas)
		{
			var visitor = new StateFormulaCollector();
			foreach (var formula in formulas)
				visitor.Visit(formula);

			// Check that the state formula has a closure -- the current version of the C# compiler 
			// always does that, but future versions might not.
			foreach (var formula in visitor.StateFormulas)
				Assert.NotNull(formula.Expression.Target, "Unexpected state formula without closure object.");

			return visitor.StateFormulas.ToArray();
		}

		/// <summary>
		///   Creates the object table for the <paramref name="model" /> and <paramref name="stateFormulas" />.
		/// </summary>
		private static ObjectTable CreateObjectTable(Model model, StateFormula[] stateFormulas)
		{
			var modelObjects = model
				.SelectMany(component => SerializationRegistry.Default.GetReferencedObjects(component, SerializationMode.Full));

			var formulaObjects = stateFormulas
				.SelectMany(formula => SerializationRegistry.Default.GetReferencedObjects(formula.Expression.Target, SerializationMode.Full));

			return new ObjectTable(modelObjects.Concat(formulaObjects));
		}

		/// <summary>
		///   Serializes the <paramref name="objectTable" /> using the <paramref name="writer" />.
		/// </summary>
		/// <param name="objectTable">The object table that should be serialized.</param>
		/// <param name="writer">The writer the serialized information should be written to.</param>
		private static void SerializeObjectTable(ObjectTable objectTable, BinaryWriter writer)
		{
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(writer, nameof(writer));

			// Serialize the objects contained in the table
			writer.Write(objectTable.Count);
			foreach (var obj in objectTable)
			{
				var serializerIndex = SerializationRegistry.Default.GetSerializerIndex(obj);
				writer.Write(serializerIndex);
				SerializationRegistry.Default.GetSerializer(serializerIndex).SerializeType(obj, writer);
			}
		}

		#endregion

		#region Deserialization

		/// <summary>
		///   Loads a <see cref="RuntimeModel" /> from the <paramref name="stream" />.
		/// </summary>
		/// <param name="stream">The stream the model should be loaded from.</param>
		public static RuntimeModel Load(Stream stream)
		{
			Requires.NotNull(stream, nameof(stream));

			using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
				return DeserializeModel(reader);
		}

		/// <summary>
		///   Deserializes a <see cref="RuntimeModel" /> from the <paramref name="reader" />.
		/// </summary>
		private static unsafe RuntimeModel DeserializeModel(BinaryReader reader)
		{
			// Deserialize the object table
			var objectTable = DeserializeObjectTable(reader);

			// Deserialize the object identifiers of the root components
			var roots = new Component[reader.ReadInt32()];
			for (var i = 0; i < roots.Length; ++i)
				roots[i] = (Component)objectTable.GetObject(reader.ReadInt32());

			// Copy the serialized initial state from the stream
			var slotCount = reader.ReadInt32();
			var serializedState = stackalloc int[slotCount];

			for (var i = 0; i < slotCount; ++i)
				serializedState[i] = reader.ReadInt32();

			// Deserialize the model's initial state
			var deserializer = SerializationRegistry.Default.CreateStateDeserializer(objectTable, SerializationMode.Full);
			deserializer(serializedState);

			// Deserialize the state formulas and instantiate the runtime model
			var stateFormulas = DeserializeFormulas(reader, objectTable);
			return new RuntimeModel(roots, objectTable, stateFormulas);
		}

		/// <summary>
		///   Deserializes the <see cref="StateFormula" />s from the <paramref name="reader" />.
		/// </summary>
		private static StateFormula[] DeserializeFormulas(BinaryReader reader, ObjectTable objectTable)
		{
			var stateFormulas = new StateFormula[reader.ReadInt32()];
			for (var i = 0; i < stateFormulas.Length; ++i)
			{
				// Deserialize the closure object and method name to generate the delegate
				var closure = objectTable.GetObject(reader.ReadInt32());
				var method = closure.GetType().GetMethod(reader.ReadString(), BindingFlags.NonPublic | BindingFlags.Instance);
				var expression = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), closure, method);

				// Deserialize the label name and instantiate the state formula
				stateFormulas[i] = new StateFormula(expression, reader.ReadString());
			}

			return stateFormulas;
		}

		/// <summary>
		///   Deserializes the <see cref="ObjectTable" /> from the <paramref name="reader" />.
		/// </summary>
		/// <param name="reader">The reader the <see cref="ObjectTable" /> should be deserialized from.</param>
		private static ObjectTable DeserializeObjectTable(BinaryReader reader)
		{
			Requires.NotNull(reader, nameof(reader));

			// Deserialize the objects contained in the table
			var objects = new object[reader.ReadInt32()];
			for (var i = 0; i < objects.Length; ++i)
			{
				var serializer = SerializationRegistry.Default.GetSerializer(reader.ReadInt32());
				objects[i] = serializer.InstantiateType(reader);
			}

			return new ObjectTable(objects);
		}

		#endregion
	}
}