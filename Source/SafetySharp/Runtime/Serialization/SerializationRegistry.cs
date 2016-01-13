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
			RegisterSerializer(new FaultEffectSerializer());
			RegisterSerializer(new ArraySerializer());
			RegisterSerializer(new StringSerializer());
			RegisterSerializer(new TypeSerializer());
			RegisterSerializer(new StateFormulaSerializer());
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
			return _serializers.FindLastIndex(s => s.CanSerialize(obj));
		}

		/// <summary>
		///   Generates the <see cref="StateVectorLayout"/> for the <paramref name="objects" />.
		/// </summary>
		/// <param name="objects">The objects consisting of state values the state vector layout should be generated for.</param>
		/// <param name="mode">The serialization mode that should be used to generate the state vector layout.</param>
		internal StateVectorLayout GetStateVectorLayout(ObjectTable objects, SerializationMode mode)
		{
			Requires.NotNull(objects, nameof(objects));
			Requires.InRange(mode, nameof(mode));

			var layout = new StateVectorLayout(objects);
			foreach (var slot in objects.SelectMany(obj => GetSerializer(obj).GetStateSlotMetadata(obj, objects.GetObjectIdentifier(obj), mode)))
				layout.Add(slot);

			layout.Compact();
			return layout;
		}

		/// <summary>
		///   Gets all objects referenced by <paramref name="obj" />, including <paramref name="obj" /> itself.
		/// </summary>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal IEnumerable<object> GetReferencedObjects(object obj, SerializationMode mode)
		{
			Requires.NotNull(obj, nameof(obj));

			var referencedObjects = new HashSet<object>(ReferenceEqualityComparer<object>.Default) { obj };
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