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

namespace SafetySharp.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using CompilerServices;
	using Utilities;

	/// <summary>
	///   Manages the allowed ranges and overflow behaviors of <see cref="Component" /> fields.
	/// </summary>
	public static class Range
	{
		private static readonly ConditionalWeakTable<object, Dictionary<FieldInfo, RangeAttribute>> _ranges =
			new ConditionalWeakTable<object, Dictionary<FieldInfo, RangeAttribute>>();

		/// <summary>
		///   Gets the range metadata for the <paramref name="obj" />'s <paramref name="field" />. Returns <c>null</c> when the range is
		///   unrestricted.
		/// </summary>
		/// <param name="obj">The object the range metadata should be returned for.</param>
		/// <param name="field">The field the range metadata should be returned for.</param>
		internal static RangeAttribute GetMetadata(object obj, FieldInfo field)
		{
			var range = field.GetCustomAttribute<RangeAttribute>();
			if (range != null)
			{
				return new RangeAttribute(
					ConvertType(field.FieldType, range.LowerBound), ConvertType(field.FieldType, range.UpperBound), range.OverflowBehavior);
			}

			Dictionary<FieldInfo, RangeAttribute> infos;
			if (!_ranges.TryGetValue(obj, out infos))
				return CreateDefaultRange(field.FieldType);

			if (!infos.TryGetValue(field, out range))
				return CreateDefaultRange(field.FieldType);

			return range;
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
		public static void Restrict<T>(Expression<Func<T>> fieldExpression, object lowerBound, object upperBound, OverflowBehavior overflowBehavior)
			where T : struct, IComparable
		{
			Requires.NotNull(fieldExpression, nameof(fieldExpression));
			Requires.InRange(overflowBehavior, nameof(overflowBehavior));
			Requires.That(typeof(T).IsNumericType(), nameof(fieldExpression), "Expected a field of numeric type.");
			Requires.OfType<MemberExpression>(fieldExpression.Body, nameof(fieldExpression), "Expected a non-nested reference to a field.");

			var range = new RangeAttribute(ConvertType(typeof(T), lowerBound), ConvertType(typeof(T), upperBound), overflowBehavior);
			var memberExpresssion = (MemberExpression)fieldExpression.Body;
			var fieldInfo = memberExpresssion.Member as FieldInfo;
			var objectExpression = memberExpresssion.Expression as ConstantExpression;

			Requires.That(fieldInfo != null, nameof(fieldExpression), "Expected a non-nested reference to a field.");
			Requires.That(objectExpression != null, nameof(fieldExpression), "Expected a non-nested reference to non-static field.");
			Requires.That(((IComparable)range.LowerBound).CompareTo(range.UpperBound) <= 0, nameof(lowerBound),
				$"lower bound '{range.LowerBound}' is not smaller than upper bound '{range.UpperBound}'.");

			Dictionary<FieldInfo, RangeAttribute> infos;
			if (_ranges.TryGetValue(objectExpression.Value, out infos))
				infos[fieldInfo] = range;
			else
				_ranges.Add(objectExpression.Value, new Dictionary<FieldInfo, RangeAttribute> { [fieldInfo] = range });
		}

		/// <summary>
		///   Converts the type of <paramref name="value" /> if it does not match exactly.
		/// </summary>
		private static object ConvertType(Type fieldType, object value)
		{
			var valueType = value.GetType();

			if (fieldType == valueType)
				return value;

			switch (Type.GetTypeCode(fieldType))
			{
				case TypeCode.Char:
					return Convert.ToChar(value);
				case TypeCode.SByte:
					return Convert.ToSByte(value);
				case TypeCode.Byte:
					return Convert.ToByte(value);
				case TypeCode.Int16:
					return Convert.ToInt16(value);
				case TypeCode.UInt16:
					return Convert.ToUInt16(value);
				case TypeCode.Int32:
					return Convert.ToInt32(value);
				case TypeCode.UInt32:
					return Convert.ToUInt32(value);
				case TypeCode.Int64:
					return Convert.ToInt64(value);
				case TypeCode.UInt64:
					return Convert.ToUInt64(value);
				case TypeCode.Single:
					return Convert.ToSingle(value);
				case TypeCode.Double:
					return Convert.ToDouble(value);
				default:
					return Assert.NotReached<object>($"Cannot convert a value of type '{valueType.FullName}' to type '{fieldType.FullName}'.");
			}
		}

		/// <summary>
		///   Creates the default range when nothing is known about a field.
		/// </summary>
		private static RangeAttribute CreateDefaultRange(Type fieldType)
		{
			// We can optimize enums
			if (fieldType.IsEnum)
			{
				var values = ConvertValues(fieldType, Enum.GetValues(fieldType));
				if (values.Length == 0)
					return new RangeAttribute(0, 0, OverflowBehavior.Error);

				if (!fieldType.HasAttribute<FlagsAttribute>())
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