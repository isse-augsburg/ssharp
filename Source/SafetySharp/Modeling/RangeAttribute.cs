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

	/// <summary>
	///   When applied to a S# field, indicates the field's range of valid values and its <see cref="OverflowBehavior" />.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public sealed class RangeAttribute : Attribute
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="from">The inclusive lower bound.</param>
		/// <param name="to">The inclusive upper bound.</param>
		/// <param name="overflowBehavior">The overflow behavior.</param>
		public RangeAttribute(object from, object to, OverflowBehavior overflowBehavior)
		{
			LowerBound = from;
			UpperBound = to;
			OverflowBehavior = overflowBehavior;
		}

		/// <summary>
		///   Gets the inclusive lower bound.
		/// </summary>
		public object LowerBound { get; }

		/// <summary>
		///   Gets the inclusive upper bound.
		/// </summary>
		public object UpperBound { get; }

		/// <summary>
		///   Gets the overflow behavior.
		/// </summary>
		public OverflowBehavior OverflowBehavior { get; }

		/// <summary>
		///   Gets the numeric <see cref="Type" /> that can express all values in the range.
		/// </summary>
		internal Type GetRangeType()
		{
			if (CheckType(SByte.MinValue, SByte.MaxValue, Convert.ToSByte))
				return typeof(SByte);

			if (CheckType(Byte.MinValue, Byte.MaxValue, Convert.ToByte))
				return typeof(Byte);

			if (CheckType(UInt16.MinValue, UInt16.MaxValue, Convert.ToUInt16))
				return typeof(UInt16);

			if (CheckType(Int16.MinValue, Int16.MaxValue, Convert.ToInt16))
				return typeof(Int16);

			if (CheckType(UInt32.MinValue, UInt32.MaxValue, Convert.ToUInt32))
				return typeof(UInt32);

			if (CheckType(Int32.MinValue, Int32.MaxValue, Convert.ToInt32))
				return typeof(Int32);

			if (CheckType(UInt64.MinValue, UInt64.MaxValue, Convert.ToUInt64))
				return typeof(UInt64);

			return typeof(Int64);
		}

		/// <summary>
		///   Checks whether the <paramref name="conversion" /> can be applied to both the lower and the upper bound and whether the
		///   bounds lie withing the range of the <paramref name="min" /> and <paramref name="max" /> values.
		/// </summary>
		private bool CheckType<T>(T min, T max, Func<object, T> conversion)
			where T : IComparable
		{
			try
			{
				var lower = conversion(LowerBound);
				var upper = conversion(UpperBound);

				return min.CompareTo(lower) <= 0 && max.CompareTo(upper) >= 0;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}