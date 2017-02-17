// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace SafetySharp.Runtime
{
	using System;
	using System.Reflection;
	using ISSE.SafetyChecking.Utilities;
	using Modeling;
	using Utilities;

	/// <summary>
	///   Provides metadata about the range of a field.
	/// </summary>
	internal sealed class RangeMetadata<T> : RangeMetadata
	{
		private readonly T _lowerBound;
		private readonly T _upperBound;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="obj">The object the metadata is provided for or <c>null</c> for struct fields.</param>
		/// <param name="type">The type of the range the metadata is provided for.</param>
		/// <param name="field">The field the range metadata is provided for or <c>null</c> for arrays.</param>
		/// <param name="range">The range metadata for the field.</param>
		public RangeMetadata(object obj, Type type, FieldInfo field, RangeAttribute range)
			: base(obj, field, range.OverflowBehavior)
		{
			_lowerBound = (T)ConvertType(type, range.LowerBound);
			_upperBound = (T)ConvertType(type, range.UpperBound);
		}

		/// <summary>
		///   Gets the inclusive lower bound.
		/// </summary>
		public override object LowerBound => _lowerBound;

		/// <summary>
		///   Gets the inclusive upper bound.
		/// </summary>
		public override object UpperBound => _upperBound;

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
	}
}