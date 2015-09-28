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

namespace SafetySharp.Runtime.Serialization.Serializers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Serializes all kinds of objects.
	/// </summary>
	internal class ObjectSerializer : Serializer
	{
		/// <summary>
		///   Checks whether the serialize is able to serialize the <paramref name="type" />.
		/// </summary>
		/// <param name="type">The type that should be checked.</param>
		protected internal override bool CanSerialize(Type type)
		{
			return true;
		}

		/// <summary>
		///   Generates the code to deserialize the <paramref name="obj" />.
		/// </summary>
		/// <param name="generator">The  generator that should be used to generate the code.</param>
		/// <param name="obj">The object that should be deserialized.</param>
		/// <param name="objectIdentifier">The identifier of the <paramref name="obj" />.</param>
		/// <param name="mode">The serialization mode that should be used to deserialize the object.</param>
		protected internal override void Deserialize(SerializationGenerator generator, object obj, int objectIdentifier, SerializationMode mode)
		{
			foreach (var field in GetFields(obj, mode))
				generator.DeserializeField(objectIdentifier, field);
		}

		/// <summary>
		///   Generates the code to serialize the <paramref name="obj" />.
		/// </summary>
		/// <param name="generator">The  generator that should be used to generate the code.</param>
		/// <param name="obj">The object that should be serialized.</param>
		/// <param name="objectIdentifier">The identifier of the <paramref name="obj" />.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the object.</param>
		protected internal override void Serialize(SerializationGenerator generator, object obj, int objectIdentifier, SerializationMode mode)
		{
			foreach (var field in GetFields(obj, mode))
				generator.SerializeField(objectIdentifier, field);
		}

		/// <summary>
		///   Gets the number of state slots required by the serialized data of <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The object consisting of state values that should be serialized.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		protected internal override int GetStateSlotCount(object obj, SerializationMode mode)
		{
			return GetFields(obj, mode).Sum(field => SerializationGenerator.GetStateSlotCount(field.FieldType));
		}

		/// <summary>
		///   Serializes the information about <paramref name="obj" />'s type using the <paramref name="writer" />.
		/// </summary>
		/// <param name="obj">The object whose type information should be serialized.</param>
		/// <param name="writer">The writer the serialized information should be written to.</param>
		protected internal override void SerializeType(object obj, BinaryWriter writer)
		{
			if (obj.GetType().IsHidden(SerializationMode.Full, discoveringObjects: false))
				return;

			// ReSharper disable once AssignNullToNotNullAttribute
			writer.Write(obj.GetType().AssemblyQualifiedName);
		}

		/// <summary>
		///   Creates an instance of the serialized type stored in the <paramref name="reader" /> without running
		///   any of the type's constructors.
		/// </summary>
		/// <param name="reader">The reader the serialized type information should be read from.</param>
		protected internal override object InstantiateType(BinaryReader reader)
		{
			return FormatterServices.GetUninitializedObject(Type.GetType(reader.ReadString(), throwOnError: true));
		}

		/// <summary>
		///   Gets all objects referenced by <paramref name="obj" />, excluding <paramref name="obj" /> itself.
		/// </summary>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		protected internal override IEnumerable<object> GetReferencedObjects(object obj, SerializationMode mode)
		{
			return from field in GetFields(obj, mode, discoveringObjects: true)
				   where field.FieldType.IsReferenceType()
				   select field.GetValue(obj);
		}

		/// <summary>
		///   Gets the fields declared by the <paramref name="obj" /> that should be serialized.
		/// </summary>
		/// <param name="obj">The object that should be serialized.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		/// <param name="inheritanceRoot">
		///   The first base type of the <paramref name="obj" /> whose fields should be ignored. If
		///   <c>null</c>, <see cref="object" /> is the inheritance root.
		/// </param>
		/// <param name="discoveringObjects">Indicates whether objects are being discovered.</param>
		protected static IEnumerable<FieldInfo> GetFields(object obj, SerializationMode mode,
														  Type inheritanceRoot = null, bool discoveringObjects = false)
		{
			var type = obj.GetType();
			if (type.IsHidden(mode, discoveringObjects))
				return Enumerable.Empty<FieldInfo>();

			var fields = obj.GetType().GetFields(inheritanceRoot ?? typeof(object)).Where(field =>
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