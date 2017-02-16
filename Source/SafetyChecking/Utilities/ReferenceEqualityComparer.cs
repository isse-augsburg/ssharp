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
	using System.Collections.Generic;
	using System.Runtime.CompilerServices;

	/// <summary>
	///   An equality comparer that compares objects for reference equality.
	/// </summary>
	/// <typeparam name="T">The type of objects to compare.</typeparam>
	internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
		where T : class
	{
		/// <summary>
		///   Gets the default instance of the <see cref="ReferenceEqualityComparer{T}" /> class.
		/// </summary>
		public static readonly ReferenceEqualityComparer<T> Default = new ReferenceEqualityComparer<T>();

		/// <inheritdoc />
		public bool Equals(T left, T right)
		{
			return ReferenceEquals(left, right);
		}

		/// <inheritdoc />
		public int GetHashCode(T value)
		{
			return RuntimeHelpers.GetHashCode(value);
		}
	}
}