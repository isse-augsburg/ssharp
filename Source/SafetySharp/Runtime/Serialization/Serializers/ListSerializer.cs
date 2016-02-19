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
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Serializes <see cref="List{T}" /> instances.
	/// </summary>
	internal sealed class ListSerializer : ObjectSerializer
	{
		/// <summary>
		///   Checks whether the serialize is able to serialize the <paramref name="obj" />.
		/// </summary>
		/// <param name="obj">The obj that should be checked.</param>
		protected internal override bool CanSerialize(object obj)
		{
			var type = obj.GetType();
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
		}

		/// <summary>
		///   Gets all objects referenced by <paramref name="obj" />, excluding <paramref name="obj" /> itself, when
		///   <paramref name="obj" /> was marked with the <paramref name="hidden" /> attribute.
		/// </summary>
		/// <param name="obj">The object the referenced objects should be returned for.</param>
		/// <param name="mode">The serialization mode that should be used to serialize the objects.</param>
		/// <param name="hidden">The <see cref="HiddenAttribute" /> instance the field storing <paramref name="obj" /> was marked with.</param>
		protected internal override IEnumerable<object> GetReferencedObjects(object obj, SerializationMode mode, HiddenAttribute hidden)
		{
			// Optimization: Skip enumerables with hidden elements
			if (mode == SerializationMode.Optimized && hidden?.HideElements == true)
			{
				// We have to make sure the objects referenced by the array are discovered nevertheless
				var enumerable = obj as IEnumerable;
				if (enumerable == null)
					yield break;

				foreach (var element in enumerable.Cast<object>().Where(item => item.GetType().IsReferenceType()))
					yield return element;
			}
			else
				yield return obj;
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
		protected override IEnumerable<FieldInfo> GetFields(object obj, SerializationMode mode, Type startType = null, Type inheritanceRoot = null,
															bool discoveringObjects = false)
		{
			var fields = base.GetFields(obj, mode, startType, inheritanceRoot, discoveringObjects);

			if (mode == SerializationMode.Full || discoveringObjects)
				return fields;

			return fields.Where(field => field.Name == "_size");
		}

		/// <summary>
		///   Gets the range information for the <paramref name="obj" />'s <paramref name="field" /> if it cannot be determined
		///   automatically by S#.
		///   Returns <c>false</c> to indicate that no range information is available.
		/// </summary>
		/// <param name="obj">The object the range should be determined for.</param>
		/// <param name="field">The field the range should be determined for.</param>
		/// <param name="mode">The serialization mode the range is obtained for.</param>
		/// <param name="range">Returns the range, if available.</param>
		protected internal override bool TryGetRange(object obj, FieldInfo field, SerializationMode mode, out RangeAttribute range)
		{
			if (field.Name != "_size" || mode != SerializationMode.Optimized)
			{
				range = null;
				return false;
			}

			var capacityProperty = obj.GetType().GetProperty("Capacity");
			var capacity = (int)capacityProperty.GetValue(obj);

			range = new RangeAttribute(0, capacity, OverflowBehavior.Error);
			return true;
		}
	}
}