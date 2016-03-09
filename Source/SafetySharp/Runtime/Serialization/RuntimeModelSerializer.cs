// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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
	using System.Runtime.Serialization;
	using System.Text;
	using Analysis;
	using Analysis.FormulaVisitors;
	using Modeling;
	using Reflection;
	using Utilities;

	/// <summary>
	///   Serializes a <see cref="RuntimeModel" /> instance into a <see cref="Stream" />.
	/// </summary>
	internal class RuntimeModelSerializer
	{
		private Func<ObjectTable, SerializationDelegate> _deserializerFactory;
		private byte[] _serializedModel;
		private StateVectorLayout _stateVector;

		#region Serialization

		/// <summary>
		///   Serializes the <paramref name="model" /> and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="model">The model that should be serialized.</param>
		/// <param name="formulas">The formulas that should be serialized.</param>
		public void Serialize(Model model, params Formula[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			using (var buffer = new MemoryStream())
			using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
			{
				SerializeModel(writer, model, formulas);
				_serializedModel = buffer.ToArray();
			}
		}

		/// <summary>
		///   Returns the serialized <paramref name="model" /> and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="model">The model that should be serialized.</param>
		/// <param name="formulas">The formulas that should be serialized.</param>
		public static byte[] Save(Model model, params Formula[] formulas)
		{
			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, formulas);
			return serializer._serializedModel;
		}

		/// <summary>
		///   Serializes the <paramref name="model" />.
		/// </summary>
		private unsafe void SerializeModel(BinaryWriter writer, Model model, Formula[] formulas)
		{
			//  Make sure that all auto-bound fault effects have been bound and that all bindings have been created
			model.AssignFaultIdentifiers();
			model.BindFaultEffects();
			model.CreateBindings();

			// Collect all objects contained in the model
			var stateFormulas = CollectStateFormulas(formulas);
			var objectTable = CreateObjectTable(model, formulas, stateFormulas);

			// Prepare the serialization of the model's initial state
			_stateVector = SerializationRegistry.Default.GetStateVectorLayout(objectTable, SerializationMode.Full);
			_deserializerFactory = null;

			var stateVectorSize = _stateVector.SizeInBytes;
			var serializer = _stateVector.CreateSerializer(objectTable);

			// Serialize the object table
			SerializeObjectTable(objectTable, writer);

			// Serialize the object identifiers of the root components
			writer.Write(model.Count);
			foreach (var root in model)
				writer.Write(objectTable.GetObjectIdentifier(root));

			// Serialize the object identifiers of the root formulas
			writer.Write(formulas.Length);
			foreach (var formula in formulas)
				writer.Write(objectTable.GetObjectIdentifier(formula));

			// Serialize the initial state
			var serializedState = stackalloc byte[stateVectorSize];
			serializer(serializedState);

			// Copy the serialized state to the stream
			writer.Write(stateVectorSize);
			for (var i = 0; i < stateVectorSize; ++i)
				writer.Write(serializedState[i]);

			SerializeStateFormulas(writer, objectTable, stateFormulas);
		}

		/// <summary>
		///   Serializes the <paramref name="stateFormulas" />.
		/// </summary>
		private static void SerializeStateFormulas(BinaryWriter writer, ObjectTable objectTable, StateFormula[] stateFormulas)
		{
			writer.Write(stateFormulas.Length);
			foreach (var formula in stateFormulas)
			{
				// Serialize the object identifier of the closure as well as the method name
				writer.Write(objectTable.GetObjectIdentifier(formula));
				writer.Write(objectTable.GetObjectIdentifier(formula.Expression.Target));
				writer.Write(formula.Expression.Method.Name);
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
				Requires.NotNull(formula.Expression.Target, "Unexpected state formula without closure object.");

			return visitor.StateFormulas.ToArray();
		}

		/// <summary>
		///   Creates the object table for the <paramref name="model" /> and <paramref name="formulas" />.
		/// </summary>
		private static ObjectTable CreateObjectTable(Model model, Formula[] formulas, StateFormula[] stateFormulas)
		{
			var objects = model
				.Cast<object>()
				.Concat(formulas)
				.Concat(stateFormulas.Select(formula => formula.Expression.Target));

			return new ObjectTable(SerializationRegistry.Default.GetReferencedObjects(objects.ToArray(), SerializationMode.Full));
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
		///   Loads a <see cref="SerializedRuntimeModel" /> instance.
		/// </summary>
		/// <param name="serializedModel">The serialized model that should be loaded.</param>
		public static SerializedRuntimeModel LoadSerializedData(byte[] serializedModel)
		{
			Requires.NotNull(serializedModel, nameof(serializedModel));
			return new RuntimeModelSerializer { _serializedModel = serializedModel }.LoadSerializedData();
		}

		/// <summary>
		///   Loads a <see cref="SerializedRuntimeModel" /> instance.
		/// </summary>
		public SerializedRuntimeModel LoadSerializedData()
		{
			Requires.That(_serializedModel != null, "No model is loaded that could be serialized.");

			using (var reader = new BinaryReader(new MemoryStream(_serializedModel), Encoding.UTF8, leaveOpen: true))
				return DeserializeModel(_serializedModel, reader);
		}

		/// <summary>
		///   Loads a <see cref="RuntimeModel" /> instance.
		/// </summary>
		public RuntimeModel Load()
		{
			return new RuntimeModel(LoadSerializedData());
		}

		/// <summary>
		///   Deserializes a <see cref="RuntimeModel" /> from the <paramref name="reader" />.
		/// </summary>
		private unsafe SerializedRuntimeModel DeserializeModel(byte[] buffer, BinaryReader reader)
		{
			// Deserialize the object table
			var objectTable = DeserializeObjectTable(reader);

			// Deserialize the object identifiers of the root components
			var roots = new Component[reader.ReadInt32()];
			for (var i = 0; i < roots.Length; ++i)
				roots[i] = (Component)objectTable.GetObject(reader.ReadUInt16());

			// Deserialize the object identifiers of the root formulas
			var formulas = new Formula[reader.ReadInt32()];
			for (var i = 0; i < formulas.Length; ++i)
				formulas[i] = (Formula)objectTable.GetObject(reader.ReadUInt16());

			// Copy the serialized initial state from the stream
			var stateVectorSize = reader.ReadInt32();
			var serializedState = stackalloc byte[stateVectorSize];

			for (var i = 0; i < stateVectorSize; ++i)
				serializedState[i] = reader.ReadByte();

			// Deserialize the model's initial state
			if (_stateVector == null)
				_stateVector = SerializationRegistry.Default.GetStateVectorLayout(objectTable, SerializationMode.Full);

			// Generate the deserializer
			if (_deserializerFactory == null)
				_deserializerFactory = _stateVector.CreateDeserializerFactory();

			var deserializer = _deserializerFactory(objectTable);
			deserializer(serializedState);

			// We instantiate the runtime type for each component and replace the original component
			// instance with the new runtime instance; we also replace all of the component's fault effects
			// with that instance and deserialize the initial state again. Afterwards, we have completely
			// replaced the original instance with its runtime instance, taking over all serialized data
			SubstituteRuntimeInstances(objectTable, roots);
			deserializer(serializedState);

			// Deserialize the state formulas and instantiate the runtime model
			DeserializeStateFormulas(reader, objectTable);
			return new SerializedRuntimeModel(buffer, roots, objectTable, formulas);
		}

		/// <summary>
		///   Substitutes the components and fault effects in the <paramref name="objectTable" /> and <paramref name="roots" /> array
		///   with their corresponding runtime instances.
		/// </summary>
		private static void SubstituteRuntimeInstances(ObjectTable objectTable, Component[] roots)
		{
			foreach (var component in objectTable.OfType<Component>().ToArray())
			{
				if (component.IsFaultEffect())
					continue;

				var runtimeType = component.GetRuntimeType();
				var runtimeObj = (Component)FormatterServices.GetUninitializedObject(runtimeType);
				var rootIndex = Array.IndexOf(roots, component);

				if (rootIndex != -1)
					roots[rootIndex] = runtimeObj;

				objectTable.Substitute(component, runtimeObj);
				foreach (var faultEffect in component.FaultEffects)
					objectTable.Substitute(faultEffect, runtimeObj);
			}
		}

		/// <summary>
		///   Deserializes the <see cref="StateFormula" />s from the <paramref name="reader" />.
		/// </summary>
		private static void DeserializeStateFormulas(BinaryReader reader, ObjectTable objectTable)
		{
			var count = reader.ReadInt32();
			for (var i = 0; i < count; ++i)
			{
				// Deserialize the closure object and method name to generate the delegate
				var formula = (StateFormula)objectTable.GetObject(reader.ReadUInt16());
				var closure = objectTable.GetObject(reader.ReadUInt16());
				var method = closure.GetType().GetMethod(reader.ReadString(), BindingFlags.NonPublic | BindingFlags.Instance);
				var expression = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), closure, method);

				typeof(StateFormula)
					.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
					.Single(field => field.FieldType == typeof(Func<bool>))
					.SetValue(formula, expression);
			}
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