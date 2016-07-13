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

namespace SafetySharp.Utilities
{
	using System.Threading;

	/// <summary>
	///   Provides additional atomic operations.
	/// </summary>
	internal static class InterlockedExtensions
	{
		/// <summary>
		///   Atomically exchanges <paramref name="location" /> with <paramref name="newValue" /> if <paramref name="comparison" /> is
		///   greater than <paramref name="location" />.
		/// </summary>
		/// <param name="location">The location that should be updated.</param>
		/// <param name="comparison">The value the location should be compared against.</param>
		/// <param name="newValue">The new value that should be set.</param>
		public static bool ExchangeIfGreaterThan(ref int location, int comparison, int newValue)
		{
			int initialValue;
			do
			{
				initialValue = location;
				if (initialValue >= comparison)
					return false;
			} while (Interlocked.CompareExchange(ref location, newValue, initialValue) != initialValue);

			return true;
		}

		/// <summary>
		///   Atomically adds the <paramref name="value" /> to <paramref name="location" /> and returns the value before the addition.
		/// </summary>
		/// <param name="location">The location that should be updated.</param>
		/// <param name="value">The value that should be added.</param>
		public static long AddFetch(ref long location, long value)
		{
			while (true)
			{
				var initialValue = location;
				var newValue = initialValue + value;

				if (Interlocked.CompareExchange(ref location, newValue, initialValue) == initialValue)
					return initialValue;
			}
		}
	}
}