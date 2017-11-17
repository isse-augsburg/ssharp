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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	///   This implementation compares arrays by their elements instead of by reference.
	/// </summary>
	/// <typeparam name="T">The element type of the arrays to compare.</typeparam>
	/// <inheritdoc cref="IEqualityComparer{T}"/>
	public class ArrayEqualityComparer<T> : IEqualityComparer<T[]>
	{
		private readonly IEqualityComparer<T> _elementComparer;

		public ArrayEqualityComparer(IEqualityComparer<T> elementComparer)
		{
			_elementComparer = elementComparer ?? EqualityComparer<T>.Default;
		}

		public static ArrayEqualityComparer<T> Default { get; } = new ArrayEqualityComparer<T>(null);

		bool IEqualityComparer<T[]>.Equals(T[] x, T[] y)
		{
			if (ReferenceEquals(x, y))
				return true;
			if (x == null || y == null)
				return false;
			return x.SequenceEqual(y, _elementComparer);
		}

		int IEqualityComparer<T[]>.GetHashCode(T[] obj)
		{
			return obj.Aggregate(0, (hash, elem) => hash << 1 | elem.GetHashCode());
		}
	}
}
