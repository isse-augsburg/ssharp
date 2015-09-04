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
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Represents a registry of <see cref="Serializer" />s.
	/// </summary>
	public sealed class SerializationRegistry
	{
		/// <summary>
		///   The default serializer that can serialize all objects.
		/// </summary>
		private readonly ObjectSerializer _objectSerializer = new ObjectSerializer();

		/// <summary>
		///   The list of registered serializers.
		/// </summary>
		private readonly List<Serializer> _serializers = new List<Serializer>();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="registerDefaultSerializers">Indicates whether the default serializers should be registered.</param>
		internal SerializationRegistry(bool registerDefaultSerializers)
		{
			if (!registerDefaultSerializers)
				return;

			RegisterSerializer(new FaultEffectSerializer());
			RegisterSerializer(new ComponentSerializer());
			RegisterSerializer(new ArraySerializer());
		}

		/// <summary>
		///   Registers the <paramref name="serializer" />.
		/// </summary>
		/// <param name="serializer">The serializer that should be registered.</param>
		public void RegisterSerializer(Serializer serializer)
		{
			Requires.NotNull(serializer, nameof(serializer));
			Requires.That(!_serializers.Contains(serializer), nameof(serializer),
				$"The serializer '{serializer.GetType().FullName}' has already been registered.");

			_serializers.Add(serializer);
		}

		/// <summary>
		///   Tries to find a serializer that is able to serialize the <paramref name="obj" />.
		/// </summary>
		private Serializer GetSerializer(object obj)
		{
			return GetSerializer(GetSerializerIndex(obj));
		}

		/// <summary>
		///   Gets the serializer at the <paramref name="index" />.
		/// </summary>
		private Serializer GetSerializer(int index)
		{
			return index == -1 ? _objectSerializer : _serializers[index];
		}

		/// <summary>
		///   Tries to find the index of a serializer that is able to serialize the <paramref name="obj" />.
		/// </summary>
		private int GetSerializerIndex(object obj)
		{
			var type = obj.GetType();
			return _serializers.FindIndex(s => s.CanSerialize(type));
		}

		/// <summary>
		///   Gets the number of state slots required by the serialized data of the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects consisting of state values that should be serialized.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal int GetStateSlotCount(ObjectTable objects, SerializationMode mode)
		{
			Requires.NotNull(objects, nameof(objects));
			Requires.InRange(mode, nameof(mode));

			return objects.Select(obj => GetSerializer(obj).GetStateSlotCount(obj, mode)).Sum();
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to serialize the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects consisting of state values that should be serialized.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal unsafe SerializationDelegate CreateStateSerializer(ObjectTable objects, SerializationMode mode)
		{
			Requires.NotNull(objects, nameof(objects));
			Requires.InRange(mode, nameof(mode));

			var generator = new SerializationGenerator("Serialize");

			foreach (var obj in objects)
				GetSerializer(obj).Serialize(generator, obj, objects.GetObjectIdentifier(obj), mode);

			return generator.Compile(objects);
		}

		/// <summary>
		///   Dynamically generates a delegate that can be used to deserialize the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects consisting of state values that should be deserialized.</param>
		/// <param name="mode">The serialization mode that should be used to deserialize the objects.</param>
		internal unsafe SerializationDelegate CreateStateDeserializer(ObjectTable objects, SerializationMode mode)
		{
			Requires.NotNull(objects, nameof(objects));
			Requires.InRange(mode, nameof(mode));

			var generator = new SerializationGenerator("Deserialize");

			foreach (var obj in objects)
				GetSerializer(obj).Deserialize(generator, obj, objects.GetObjectIdentifier(obj), mode);

			return generator.Compile(objects);
		}

		/// <summary>
		///   Serializes the <paramref name="objectTable" /> using the <paramref name="writer" />.
		/// </summary>
		/// <param name="objectTable">The object table that should be serialized.</param>
		/// <param name="writer">The writer the serialized information should be written to.</param>
		internal void SerializeObjectTable(ObjectTable objectTable, BinaryWriter writer)
		{
			Requires.NotNull(objectTable, nameof(objectTable));
			Requires.NotNull(writer, nameof(writer));

			// Write the serializer types that are required to deserialize the object table
			writer.Write(_serializers.Count);
			foreach (var serializer in _serializers)
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				writer.Write(serializer.GetType().AssemblyQualifiedName);
			}

			// Serialize the objects contained in the table
			writer.Write(objectTable.Count);
			foreach (var obj in objectTable)
			{
				var serializerIndex = GetSerializerIndex(obj);
				writer.Write(serializerIndex);
				GetSerializer(serializerIndex).SerializeType(obj, writer);
			}
		}

		/// <summary>
		///   Deserializes the <see cref="ObjectTable" /> from the <paramref name="reader" />.
		/// </summary>
		/// <param name="reader">The reader the <see cref="ObjectTable" /> should be deserialized from.</param>
		internal ObjectTable DeserializeObjectTable(BinaryReader reader)
		{
			Requires.NotNull(reader, nameof(reader));

			// Read the serializer types that are required to deserialize the object table
			var serializerCount = reader.ReadInt32();
			for (var i = 0; i < serializerCount; ++i)
				RegisterSerializer((Serializer)Activator.CreateInstance(Type.GetType(reader.ReadString(), throwOnError: true)));

			// Deserialize the objects contained in the table
			var objects = new object[reader.ReadInt32()];
			for (var i = 0; i < objects.Length; ++i)
			{
				var serializer = GetSerializer(reader.ReadInt32());
				objects[i] = serializer.InstantiateType(reader);
			}

			return new ObjectTable(objects);
		}

		/// <summary>
		///   Gets all <see cref="IComponent" /> instances referenced by <paramref name="component" />, excluding
		///   <paramref name="component" /> itself.
		/// </summary>
		/// <param name="component">The component the referenced subcomponent should be returned for.</param>
		internal IEnumerable<Component> GetSubcomponents(Component component)
		{
			Requires.NotNull(component, nameof(component));

			var subcomponents = new HashSet<Component>();
			GetSubcomponents(subcomponents, component);

			// Some objects may have backward references to the component itself, so we have to remove it here
			subcomponents.Remove(component);
			return subcomponents;
		}

		/// <summary>
		///   Adds all subcomponents referenced by <paramref name="obj" />, excluding <paramref name="obj" /> itself, to the set of
		///   <paramref name="subcomponents" />.
		/// </summary>
		/// <param name="subcomponents">The set of referenced objects.</param>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		private void GetSubcomponents(HashSet<Component> subcomponents, object obj)
		{
			foreach (var referencedObject in GetSerializer(obj).GetReferencedObjects(obj, SerializationMode.Full))
			{
				if (referencedObject == null)
					continue;

				var component = referencedObject as Component;
				if (component != null && !component.GetType().HasAttribute<FaultEffectAttribute>())
					subcomponents.Add(component);
				else
					GetSubcomponents(subcomponents, referencedObject);
			}
		}

		/// <summary>
		///   Gets all objects referenced by <paramref name="obj" />, including <paramref name="obj" /> itself.
		/// </summary>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal IEnumerable<object> GetReferencedObjects(object obj, SerializationMode mode)
		{
			Requires.NotNull(obj, nameof(obj));

			var referencedObjects = new HashSet<object> { obj };
			GetReferencedObjects(referencedObjects, obj, mode);
			return referencedObjects;
		}

		/// <summary>
		///   Adds all objects referenced by <paramref name="obj" />, excluding <paramref name="obj" /> itself, to the set of
		///   <paramref name="referencedObjects" />.
		/// </summary>
		/// <param name="referencedObjects">The set of referenced objects.</param>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		private void GetReferencedObjects(HashSet<object> referencedObjects, object obj, SerializationMode mode)
		{
			foreach (var referencedObject in GetSerializer(obj).GetReferencedObjects(obj, mode))
			{
				if (referencedObject != null && referencedObjects.Add(referencedObject))
					GetReferencedObjects(referencedObjects, referencedObject, mode);
			}
		}
	}
}