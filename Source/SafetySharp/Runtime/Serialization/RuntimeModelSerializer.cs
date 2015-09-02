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
	using Modeling;
	using Utilities;

	/// <summary>
	///   Serializes a <see cref="RuntimeModel" /> instance into a <see cref="Stream" />.
	/// </summary>
	internal sealed class RuntimeModelSerializer
	{
		/// <summary>
		///   The serialized information about the runtime model.
		/// </summary>
		private readonly byte[] _runtimeModel;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be serialized.</param>
		public RuntimeModelSerializer(Model model)
		{
			Requires.NotNull(model, nameof(model));
			_runtimeModel = SerializeModel(model);
		}

		/// <summary>
		///   Saves the serialized model and the <paramref name="stateLabels" /> to the <paramref name="stream" />.
		/// </summary>
		/// <param name="stream">The stream the serialized specification should be written to.</param>
		/// <param name="stateLabels">The state labels that should be serialized into the <paramref name="stream" />.</param>
		public void Save(Stream stream, Func<bool>[] stateLabels)
		{
			Requires.NotNull(stream, nameof(stream));
			Requires.NotNull(stateLabels, nameof(stateLabels));

			var serializedStateLabels = new byte[0]; // TODO
			stream.Write(_runtimeModel, 0, _runtimeModel.Length);
			stream.Write(serializedStateLabels, 0, serializedStateLabels.Length);
		}

		/// <summary>
		///   Loads a <see cref="RuntimeModel" /> from the <paramref name="stream" />.
		/// </summary>
		/// <param name="stream">The stream the model should be loaded from.</param>
		public static unsafe RuntimeModel Load(Stream stream)
		{
			Requires.NotNull(stream, nameof(stream));

			using (var reader = new BinaryReader(stream))
			{
				// Deserialize the object table
				var model = new Model();
				var objectTable = model.SerializationRegistry.DeserializeObjectTable(reader);

				// Deserialize the object identifiers of the root components
				var rootCount = reader.ReadInt32();
				for (var i = 0; i < rootCount; ++i)
					model.RootComponents.Add((IComponent)objectTable.GetObject(reader.ReadInt32()));

				// Copy the serialized initial state from the stream
				var slotCount = reader.ReadInt32();
				var serializedState = stackalloc int[slotCount];

				for (var i = 0; i < slotCount; ++i)
					serializedState[i] = reader.ReadInt32();

				// Deserialize the model's initial state
				var deserializer = model.SerializationRegistry.CreateStateDeserializer(objectTable, SerializationMode.Full);
				deserializer(serializedState);

				// Deserialize the state labels
				var stateLabels = new Func<bool>[0]; // TODO

				// Instantiate the runtime model
				return new RuntimeModel(model, objectTable, stateLabels);
			}
		}

		/// <summary>
		///   Serializes the <paramref name="model" />.
		/// </summary>
		private static unsafe byte[] SerializeModel(Model model)
		{
			// Construct the object table for the model
			var objects = model.RootComponents.SelectMany(component => model.SerializationRegistry.GetReferencedObjects(component));
			var objectTable = new ObjectTable(objects);

			// Prepare the serialization of the model's initial state
			var slotCount = model.SerializationRegistry.GetStateSlotCount(objectTable, SerializationMode.Full);
			var serializer = model.SerializationRegistry.CreateStateSerializer(objectTable, SerializationMode.Full);

			using (var memoryStream = new MemoryStream())
			using (var writer = new BinaryWriter(memoryStream))
			{
				// Serialize the object table
				model.SerializationRegistry.SerializeObjectTable(objectTable, writer);

				// Serialize object identifiers of the root components
				writer.Write(model.RootComponents.Count);
				foreach (var root in model.RootComponents)
					writer.Write(objectTable.GetObjectIdentifier(root));

				// Serialize the initial state
				var serializedState = stackalloc int[slotCount];
				serializer(serializedState);

				// Copy the serialized state to the stream
				writer.Write(slotCount);
				for (var i = 0; i < slotCount; ++i)
					writer.Write(serializedState[i]);

				// Return the serialized model
				return memoryStream.ToArray();
			}
		}
	}
}