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
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Modeling;
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
			RegisterSerializer(new BoxedValueSerializer());
			RegisterSerializer(new FaultEffectSerializer());
			RegisterSerializer(new ArraySerializer());
			RegisterSerializer(new ListSerializer());
			RegisterSerializer(new DictionarySerializer());
			RegisterSerializer(new StringSerializer());
			RegisterSerializer(new TypeSerializer());
			RegisterSerializer(new MethodInfoSerializer());
			RegisterSerializer(new DelegateSerializer());
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
		///   Generates the <see cref="StateVectorLayout" /> for the <paramref name="objects" />.
		/// </summary>
		/// <param name="model">The model the state vector should be layouted for.</param>
		/// <param name="objects">The objects consisting of state values the state vector layout should be generated for.</param>
		/// <param name="mode">The serialization mode that should be used to generate the state vector layout.</param>
		internal StateVectorLayout GetStateVectorLayout(ModelBase model, ObjectTable objects, SerializationMode mode)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(objects, nameof(objects));
			Requires.InRange(mode, nameof(mode));

			var layout = new StateVectorLayout();
			foreach (var slot in objects.SelectMany(obj => GetSerializer(obj).GetStateSlotMetadata(obj, objects.GetObjectIdentifier(obj), mode)))
				layout.Add(slot);

			layout.Compact(model, mode);
			return layout;
		}

		/// <summary>
		///   Gets all objects referenced by the <paramref name="objects" />, not including <paramref name="objects" /> itself.
		/// </summary>
		/// <param name="objects">The objects the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal IEnumerable<object> GetReferencedObjects(object[] objects, SerializationMode mode)
		{
			Requires.NotNull(objects, nameof(objects));

			var referencedObjects = new HashSet<object>(ReferenceEqualityComparer<object>.Default);

			foreach (var obj in objects)
			{
				referencedObjects.Add(obj);
				GetReferencedObjects(referencedObjects, obj, mode);
			}

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

		/// <summary>
		///   Gets all objects referenced by the value-typed <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The value-typed object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		internal IEnumerable<object> GetObjectsReferencedByStruct(object obj, SerializationMode mode)
		{
			Requires.NotNull(obj, nameof(obj));
			Requires.That(obj.GetType().IsStructType(), "Expected a value-typed object.");

			var referencedObjects = new HashSet<object>(ReferenceEqualityComparer<object>.Default);
			GetObjectsReferencedByStruct(referencedObjects, obj, mode);

			return referencedObjects;
		}

		/// <summary>
		///   Adds all objects referenced by the value-typed <paramref name="obj" /> to the set of <paramref name="referencedObjects" />
		///   .
		/// </summary>
		/// <param name="referencedObjects">The set of referenced objects.</param>
		/// <param name="obj">The value-typed object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		private static void GetObjectsReferencedByStruct(HashSet<object> referencedObjects, object obj, SerializationMode mode)
		{
			foreach (var field in GetSerializationFields(obj, mode))
			{
				if (field.FieldType.IsStructType())
					GetObjectsReferencedByStruct(referencedObjects, field.GetValue(obj), mode);
				else if (field.FieldType.IsReferenceType())
				{
					var referencedObj = field.GetValue(obj);
					if (referencedObj != null)
						referencedObjects.Add(referencedObj);
				}
			}
		}

		/// <summary>
		///   Gets the fields declared by the <paramref name="obj" /> that should be serialized.
		/// </summary>
		/// <param name="obj">The object that should be serialized.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		/// <param name="startType">
		///   The first type in <paramref name="obj" />'s inheritance hierarchy whose fields should be returned.
		///   If <c>null</c>, corresponds to <paramref name="obj" />'s actual type.
		/// </param>
		/// <param name="inheritanceRoot">
		///   The first base type of the <paramref name="obj" /> whose fields should be ignored. If
		///   <c>null</c>, <see cref="object" /> is the inheritance root.
		/// </param>
		/// <param name="discoveringObjects">Indicates whether objects are being discovered.</param>
		internal static IEnumerable<FieldInfo> GetSerializationFields(object obj, SerializationMode mode,
																	  Type startType = null,
																	  Type inheritanceRoot = null,
																	  bool discoveringObjects = false)
		{
			var type = startType ?? obj.GetType();
			if (type.IsHidden(mode, discoveringObjects))
				return Enumerable.Empty<FieldInfo>();

			var fields = type.GetFields(inheritanceRoot ?? typeof(object)).Where(field =>
			{
				// Ignore static or constant fields
				if (field.IsStatic || field.IsLiteral)
					return false;

				// Don't try to serialize hidden fields
				if (field.IsHidden(mode, discoveringObjects) || field.FieldType.IsHidden(mode, discoveringObjects))
					return false;

				// Otherwise, serialize the field
				return true;
			});

			// It is important to sort the fields in a deterministic order; by default, .NET's reflection APIs don't
			// return fields in any particular order at all, which obviously causes problems when we then go on to try
			// to deserialize fields in a different order than the one that was used to serialize them
			return fields.OrderBy(field => field.DeclaringType.FullName).ThenBy(field => field.Name);
		}
	}
}