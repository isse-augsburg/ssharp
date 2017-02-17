// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using System.IO;
	using System.Linq;
	using System.Text;
	using Analysis;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Utilities;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Serializes a <see cref="SafetySharpRuntimeModel" /> instance into a <see cref="Stream" />.
	/// </summary>
	internal class RuntimeModelSerializer
	{
		private readonly object _syncObject = new object();
		private OpenSerializationDelegate _deserializer;
		private byte[] _serializedModel;
		private StateVectorLayout _stateVector;

		#region Serialization

		/// <summary>
		///   Serializes the <paramref name="model" /> and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="model">The model that should be serialized.</param>
		/// <param name="formulas">The formulas that should be serialized.</param>
		public void Serialize(ModelBase model, params Formula[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			using (var buffer = new MemoryStream())
			using (var writer = new BinaryWriter(buffer, Encoding.UTF8, leaveOpen: true))
			{
				SerializeModel(writer, model, formulas);

				lock (_syncObject)
					_serializedModel = buffer.ToArray();
			}
		}

		/// <summary>
		///   Returns the serialized <paramref name="model" /> and the <paramref name="formulas" />.
		/// </summary>
		/// <param name="model">The model that should be serialized.</param>
		/// <param name="formulas">The formulas that should be serialized.</param>
		public static byte[] Save(ModelBase model, params Formula[] formulas)
		{
			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, formulas);

			lock (serializer._syncObject)
				return serializer._serializedModel;
		}

		/// <summary>
		///   Serializes the <paramref name="model" />.
		/// </summary>
		private unsafe void SerializeModel(BinaryWriter writer, ModelBase model, Formula[] formulas)
		{
			// Collect all objects contained in the model
			var objectTable = CreateObjectTable(model, formulas);

			// Prepare the serialization of the model's initial state
			lock (_syncObject)
			{
				_stateVector = SerializationRegistry.Default.GetStateVectorLayout(model, objectTable, SerializationMode.Full);
				_deserializer = null;
			}

			var stateVectorSize = _stateVector.SizeInBytes;
			var serializer = _stateVector.CreateSerializer(objectTable);

			// Serialize the object table
			SerializeObjectTable(objectTable, writer);

			// Serialize the object identifier of the model itself and the formulas
			writer.Write(objectTable.GetObjectIdentifier(model));
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
		}

		/// <summary>
		///   Creates the object table for the <paramref name="model" /> and <paramref name="formulas" />.
		/// </summary>
		private static ObjectTable CreateObjectTable(ModelBase model, Formula[] formulas)
		{
			var objects = model.Roots.Cast<object>().Concat(formulas).Concat(new[] { model });
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
		///   Loads a <see cref="SerializedRuntimeModel" /> from the <paramref name="serializedModel" />.
		/// </summary>
		/// <param name="serializedModel">The serialized model that should be loaded.</param>
		public static SerializedRuntimeModel LoadSerializedData(byte[] serializedModel)
		{
			Requires.NotNull(serializedModel, nameof(serializedModel));

			var serializer = new RuntimeModelSerializer { _serializedModel = serializedModel };
			return serializer.LoadSerializedData();
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
		///   Loads a <see cref="SafetySharpRuntimeModel" /> instance.
		/// </summary>
		public SafetySharpRuntimeModel Load(int stateHeaderBytes)
		{
			return new SafetySharpRuntimeModel(LoadSerializedData(), stateHeaderBytes);
		}

		/// <summary>
		///   Deserializes a <see cref="SafetySharpRuntimeModel" /> from the <paramref name="reader" />.
		/// </summary>
		private unsafe SerializedRuntimeModel DeserializeModel(byte[] buffer, BinaryReader reader)
		{
			// Deserialize the object table
			var objectTable = DeserializeObjectTable(reader);

			// Deserialize the object identifiers of the model itself and the root formulas
			var model = (ModelBase)objectTable.GetObject(reader.ReadUInt16());
			var formulas = new Formula[reader.ReadInt32()];
			for (var i = 0; i < formulas.Length; ++i)
				formulas[i] = (Formula)objectTable.GetObject(reader.ReadUInt16());

			// Copy the serialized initial state from the stream
			var stateVectorSize = reader.ReadInt32();
			var serializedState = stackalloc byte[stateVectorSize];

			for (var i = 0; i < stateVectorSize; ++i)
				serializedState[i] = reader.ReadByte();

			// Deserialize the model's initial state
			OpenSerializationDelegate deserializer;
			lock (_syncObject)
			{
				if (_stateVector == null)
					_stateVector = SerializationRegistry.Default.GetStateVectorLayout(model, objectTable, SerializationMode.Full);

				if (_deserializer == null)
					_deserializer = _stateVector.CreateDeserializer();

				deserializer = _deserializer;
			}

			deserializer(objectTable, serializedState);

			// We instantiate the runtime type for each component and replace the original component
			// instance with the new runtime instance; we also replace all of the component's fault effects
			// with that instance and deserialize the initial state again. Afterwards, we have completely
			// replaced the original instance with its runtime instance, taking over all serialized data
			objectTable.SubstituteRuntimeInstances();
			deserializer(objectTable, serializedState);

			// We substitute the dummy delegate objects with the actual instances obtained from the DelegateMetadata instances
			objectTable.SubstituteDelegates();
			deserializer(objectTable, serializedState);

			// Return the serialized model data
			return new SerializedRuntimeModel(model, buffer, objectTable, formulas);
		}

		/// <summary>
		///   Deserializes the <see cref="ObjectTable" /> from the <paramref name="reader" />.
		/// </summary>
		/// <param name="reader">The reader the objects should be deserialized from.</param>
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