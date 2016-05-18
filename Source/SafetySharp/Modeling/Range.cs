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

namespace SafetySharp.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using CompilerServices;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Manages the allowed ranges and overflow behaviors of <see cref="Component" /> fields.
	/// </summary>
	public static class Range
	{
		private static readonly ConditionalWeakTable<object, List<RangeMetadata>> _ranges =
			new ConditionalWeakTable<object, List<RangeMetadata>>();

		/// <summary>
		///   Gets the range metadata for the <paramref name="field" />. Returns <c>null</c> when the range is unrestricted.
		/// </summary>
		/// <param name="field">The field the range metadata should be returned for.</param>
		internal static RangeMetadata GetMetadata(FieldInfo field)
		{
			var range = field.GetCustomAttribute<RangeAttribute>();
			return range == null ? null : RangeMetadata.Create(null, field, range);
		}

		/// <summary>
		///   Gets the range metadata for the <paramref name="obj" />'s <paramref name="field" />. Returns <c>null</c> when the range is
		///   unrestricted.
		/// </summary>
		/// <param name="model">The model that stores the <paramref name="obj" />'s range metadata.</param>
		/// <param name="obj">The object the range metadata should be returned for.</param>
		/// <param name="field">The field the range metadata should be returned for.</param>
		internal static RangeMetadata GetMetadata(ModelBase model, object obj, FieldInfo field)
		{
			// For backing fields of auto-implemented properties, check the property instead
			// TODO: Remove this workaround once C# supports [field:Attribute] on properties
			var range = field.GetCustomAttribute<RangeAttribute>() ?? field?.GetAutoProperty()?.GetCustomAttribute<RangeAttribute>();

			if (range != null)
				return RangeMetadata.Create(obj, field, range);

			var metadata = model.RangeMetadata.FirstOrDefault(m => m.DescribesField(obj, field));
			return metadata ?? CreateDefaultRange(obj, field);
		}

		/// <summary>
		///   Restricts values that can be stored in the field referenced by the <paramref name="fieldExpression" /> to the range of
		///   <paramref name="lowerBound" /> and <paramref name="upperBound" />, both inclusive, using the
		///   <paramref name="overflowBehavior" /> to handle range overflows.
		/// </summary>
		/// <typeparam name="T">The type of the field that is restricted.</typeparam>
		/// <param name="fieldExpression">The expression referencing the field whose range should be restricted.</param>
		/// <param name="lowerBound">The inclusive lower bound.</param>
		/// <param name="upperBound">The inclusive upper bound.</param>
		/// <param name="overflowBehavior">The overflow behavior.</param>
		public static void Restrict<T>([LiftExpression] T fieldExpression, object lowerBound, object upperBound, OverflowBehavior overflowBehavior)
			where T : struct, IComparable
		{
			Requires.CompilationTransformation();
		}

		/// <summary>
		///   Restricts values that can be stored in the field referenced by the <paramref name="fieldExpression" /> to the range of
		///   <paramref name="lowerBound" /> and <paramref name="upperBound" />, both inclusive, using the
		///   <paramref name="overflowBehavior" /> to handle range overflows.
		/// </summary>
		/// <typeparam name="T">The type of the field that is restricted.</typeparam>
		/// <param name="fieldExpression">The expression referencing the field whose range should be restricted.</param>
		/// <param name="lowerBound">The inclusive lower bound.</param>
		/// <param name="upperBound">The inclusive upper bound.</param>
		/// <param name="overflowBehavior">The overflow behavior.</param>
		public static void Restrict<T>(Expression<Func<T>> fieldExpression, object lowerBound, object upperBound,
									   OverflowBehavior overflowBehavior)
			where T : struct, IComparable
		{
			Requires.NotNull(fieldExpression, nameof(fieldExpression));
			Requires.InRange(overflowBehavior, nameof(overflowBehavior));
			Requires.That(typeof(T).IsNumericType(), nameof(fieldExpression), "Expected a field of numeric type.");
			Requires.OfType<MemberExpression>(fieldExpression.Body, nameof(fieldExpression), "Expected a non-nested reference to a field.");

			var range = new RangeAttribute(lowerBound, upperBound, overflowBehavior);
			var memberExpression = (MemberExpression)fieldExpression.Body;
			var propertyInfo = memberExpression.Member as PropertyInfo;
			var fieldInfo = propertyInfo?.GetBackingField() ?? memberExpression.Member as FieldInfo;
			var objectExpression = memberExpression.Expression as ConstantExpression;

			Requires.That(fieldInfo != null, nameof(fieldExpression), "Expected a non-nested reference to a field or an auto-property.");
			Requires.That(objectExpression != null, nameof(fieldExpression), "Expected a non-nested reference to non-static field of primitive type.");
			Requires.That(((IComparable)range.LowerBound).CompareTo(range.UpperBound) <= 0, nameof(lowerBound),
				$"lower bound '{range.LowerBound}' is not smaller than upper bound '{range.UpperBound}'.");

			List<RangeMetadata> fields;
			if (_ranges.TryGetValue(objectExpression.Value, out fields))
			{
				var metadata = fields.FirstOrDefault(m => m.DescribesField(objectExpression.Value, fieldInfo));
				if (metadata != null)
					fields.Remove(metadata);

				fields.Add(RangeMetadata.Create(objectExpression.Value, fieldInfo, range));
			}
			else
				_ranges.Add(objectExpression.Value, new List<RangeMetadata> { RangeMetadata.Create(objectExpression.Value, fieldInfo, range) });
		}

		/// <summary>
		///   Copies the range metadata of the <paramref name="model" />'s object to the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model whose range metadata should be copied.</param>
		public static void CopyMetadata(ModelBase model)
		{
			foreach (var obj in model.ReferencedObjects)
			{
				List<RangeMetadata> metadata;
				if (_ranges.TryGetValue(obj, out metadata))
					model.RangeMetadata.AddRange(metadata);
			}
		}

		/// <summary>
		///   Creates the default range when no additional range data is available for the <paramref name="obj" />'s
		///   <paramref name="field" />.
		/// </summary>
		internal static RangeMetadata CreateDefaultRange(object obj, FieldInfo field)
		{
			// Check if the serializer knows a range
			RangeAttribute range;
			if (SerializationRegistry.Default.GetSerializer(obj).TryGetRange(obj, field, out range))
				return RangeMetadata.Create(obj, field, range);

			// Otherwise, maybe we can deduce a range for the type
			return RangeMetadata.Create(obj, field, CreateDefaultRange(field.FieldType));
		}

		/// <summary>
		///   Creates the default range when no additional range data is available for the <paramref name="type" />.
		/// </summary>
		internal static RangeAttribute CreateDefaultRange(Type type)
		{
			// We can optimize enums
			if (type.IsEnum)
			{
				var values = ConvertValues(type, Enum.GetValues(type));
				if (values.Length == 0)
					return new RangeAttribute(0, 0, OverflowBehavior.Error);

				if (!type.HasAttribute<FlagsAttribute>())
					return new RangeAttribute(values.Min(), values.Max(), OverflowBehavior.Error);
			}

			// In all other cases, we have to assume that the full range is used
			return null;
		}

		/// <summary>
		///   Converts the <paramref name="values" /> of the <paramref name="enumType" /> to an array of <see cref="object" /> values.
		/// </summary>
		private static object[] ConvertValues(Type enumType, Array values)
		{
			switch (Type.GetTypeCode(enumType.GetEnumUnderlyingType()))
			{
				case TypeCode.SByte:
					return ((sbyte[])values).Cast<object>().ToArray();
				case TypeCode.Byte:
					return ((byte[])values).Cast<object>().ToArray();
				case TypeCode.Int16:
					return ((short[])values).Cast<object>().ToArray();
				case TypeCode.UInt16:
					return ((ushort[])values).Cast<object>().ToArray();
				case TypeCode.Int32:
					return ((int[])values).Cast<object>().ToArray();
				case TypeCode.UInt32:
					return ((uint[])values).Cast<object>().ToArray();
				case TypeCode.Int64:
					return ((long[])values).Cast<object>().ToArray();
				case TypeCode.UInt64:
					return ((ulong[])values).Cast<object>().ToArray();
				default:
					return Assert.NotReached<object[]>("Cannot convert a values.");
			}
		}
	}
}