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

namespace SafetySharp.Runtime.Serialization.Serializers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Reflection;
	using System.Runtime.Serialization;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Serializes all kinds of objects.
	/// </summary>
	internal class ObjectSerializer : Serializer
	{
		/// <summary>
		///   Checks whether the serialize is able to serialize the <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The obj that should be checked.</param>
		protected internal override bool CanSerialize(object obj)
		{
			return true;
		}

		/// <summary>
		///   Generates the state slot metadata for the <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The object the state slot metadata should be generated for.</param>
		/// <param name="objectIdentifier">The identifier of the <paramref name="obj" />.</param>
		/// <param name="mode">The serialization mode that should be used to generate the metadata.</param>
		protected internal override IEnumerable<StateSlotMetadata> GetStateSlotMetadata(object obj, int objectIdentifier, SerializationMode mode)
		{
			foreach (var field in GetFields(obj, mode))
			{
				if (field.FieldType.IsStructType())
				{
					foreach (var metadataSlot in StateSlotMetadata.FromStruct(field.FieldType, mode))
					{
						metadataSlot.Object = obj;
						metadataSlot.ObjectIdentifier = objectIdentifier;
						metadataSlot.ObjectType = obj.GetType();
						metadataSlot.ElementCount = 1;
						metadataSlot.Field = field;

						yield return metadataSlot;
					}
				}
				else
				{
					yield return new StateSlotMetadata
					{
						Object = obj,
						ObjectType = obj.GetType(),
						ObjectIdentifier = objectIdentifier,
						DataType = field.FieldType,
						Field = field,
						ElementCount = 1
					};
				}
			}
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
			foreach (var field in GetFields(obj, mode, discoveringObjects: true))
			{
				if (field.FieldType.IsReferenceType())
				{
					var value = field.GetValue(obj);
					if (value == null)
						continue;

					foreach (var o in GetObjectReferencesFromField(value, field, mode))
						yield return o;
				}
				else if (field.FieldType.IsStructType())
				{
					var value = field.GetValue(obj);
					if (value == null)
						continue;

					foreach (var referencedObj in SerializationRegistry.Default.GetObjectsReferencedByStruct(value, mode))
						foreach (var o in GetObjectReferencesFromField(referencedObj, field, mode))
							yield return o;
				}
			}
		}

		/// <summary>
		///   Gets all objects referenced in the <paramref name="obj" />'s <paramref name="field" />, taking a potential
		///   <see cref="HiddenAttribute" /> into account.
		/// </summary>
		private static IEnumerable<object> GetObjectReferencesFromField(object obj, FieldInfo field, SerializationMode mode)
		{
			var serializer = SerializationRegistry.Default.GetSerializer(obj);
			var hiddenAttribute = field.GetDeclaringMember().GetCustomAttribute<HiddenAttribute>();

			foreach (var o in serializer.GetReferencedObjects(obj, mode, hiddenAttribute))
				yield return o;
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
		protected virtual IEnumerable<FieldInfo> GetFields(object obj, SerializationMode mode,
														   Type startType = null,
														   Type inheritanceRoot = null,
														   bool discoveringObjects = false)
		{
			return SerializationRegistry.GetSerializationFields(startType ?? obj.GetType(), mode, inheritanceRoot, discoveringObjects);
		}
	}
}