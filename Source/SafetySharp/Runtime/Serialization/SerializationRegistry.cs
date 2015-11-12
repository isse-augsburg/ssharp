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
	using System.Collections.Generic;
	using System.Linq;
	using Serializers;
	using Utilities;

	/// <summary>
	///   Represents a registry of <see cref="Serializer" />s.
	/// </summary>
	public sealed class SerializationRegistry
	{
		/// <summary>
		///   The default serialization registry instance.
		/// </summary>
		public static readonly SerializationRegistry Default = new SerializationRegistry();

		/// <summary>
		///   The list of registered serializers.
		/// </summary>
		private readonly List<Serializer> _serializers = new List<Serializer>();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		private SerializationRegistry()
		{
			RegisterSerializer(new ObjectSerializer());
			RegisterSerializer(new ComponentSerializer());
			RegisterSerializer(new FaultEffectSerializer());
			RegisterSerializer(new ArraySerializer());
			RegisterSerializer(new StringSerializer());
			RegisterSerializer(new TypeSerializer());
		}

		/// <summary>
		///   Registers the <paramref name="serializer" />. The newly added <paramref name="serializer" /> is the first one to be
		///   considered for the serialization of an object; the previously added serializers are only considered if the new
		///   <paramref name="serializer" />'s <see cref="Serializer.CanSerialize" /> method returns <c>false</c> for an object.
		/// </summary>
		/// <param name="serializer">The serializer that should be registered.</param>
		private void RegisterSerializer(Serializer serializer)
		{
			Requires.NotNull(serializer, nameof(serializer));
			Requires.That(!_serializers.Contains(serializer), nameof(serializer),
				$"The serializer '{serializer.GetType().FullName}' has already been registered.");

			_serializers.Add(serializer);
		}

		/// <summary>
		///   Tries to find a serializer that is able to serialize the <paramref name="obj" />.
		/// </summary>
		internal Serializer GetSerializer(object obj)
		{
			return GetSerializer(GetSerializerIndex(obj));
		}

		/// <summary>
		///   Gets the serializer at the <paramref name="index" />.
		/// </summary>
		internal Serializer GetSerializer(int index)
		{
			return _serializers[index];
		}

		/// <summary>
		///   Tries to find the index of a serializer that is able to serialize the <paramref name="obj" />.
		/// </summary>
		internal int GetSerializerIndex(object obj)
		{
			var type = obj.GetType();
			return _serializers.FindLastIndex(s => s.CanSerialize(type));
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

			var generator = new SerializationGenerator(methodName: "Serialize");

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

			var generator = new SerializationGenerator(methodName: "Deserialize");

			foreach (var obj in objects)
				GetSerializer(obj).Deserialize(generator, obj, objects.GetObjectIdentifier(obj), mode);

			return generator.Compile(objects);
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