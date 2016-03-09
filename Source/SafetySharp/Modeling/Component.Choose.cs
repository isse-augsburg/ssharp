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
	using System.Diagnostics;
	using System.Runtime.CompilerServices;

	public abstract partial class Component
	{
		/// <summary>
		///   The default <see cref="Choice" /> instance used by the component.
		/// </summary>
		[Hidden, DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private readonly Choice _defaultChoice = new Choice();

		/// <summary>
		///   Returns an index in the range of <paramref name="elementCount" />. Returns -1 if <paramref name="elementCount" />
		///   is 0.
		/// </summary>
		/// <param name="elementCount">The element count to choose the index from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int ChooseIndex(int elementCount)
		{
			return _defaultChoice.ChooseIndex(elementCount);
		}

		/// <summary>
		///   Returns a value within the range of the given bounds.
		/// </summary>
		/// <param name="lowerBound">The inclusive lower bound of the range to choose from.</param>
		/// <param name="upperBound">The inclusive upper bound of the range to choose from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected int ChooseFromRange(int lowerBound, int upperBound)
		{
			return _defaultChoice.ChooseFromRange(lowerBound, upperBound);
		}

		/// <summary>
		///   Deterministically returns the default value for <typeparamref name="T" />.
		/// </summary>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>()
		{
			return default(T);
		}

		/// <summary>
		///   Deterministically returns the <paramref name="value" />.
		/// </summary>
		/// <param name="value">The value to return.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>(T value)
		{
			return value;
		}

		/// <summary>
		///   Returns either <paramref name="value1" /> or <paramref name="value2" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>(T value1, T value2)
		{
			return _defaultChoice.Choose(value1, value2);
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, or <paramref name="value3" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>(T value1, T value2, T value3)
		{
			return _defaultChoice.Choose(value1, value2, value3);
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, <paramref name="value3" />, or
		///   <paramref name="value4" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <param name="value4">The fourth value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>(T value1, T value2, T value3, T value4)
		{
			return _defaultChoice.Choose(value1, value2, value3, value4);
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, <paramref name="value3" />,
		///   <paramref name="value4" />, or <paramref name="value5" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <param name="value4">The fourth value to choose.</param>
		/// <param name="value5">The fifth value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>(T value1, T value2, T value3, T value4, T value5)
		{
			return _defaultChoice.Choose(value1, value2, value3, value4, value5);
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, <paramref name="value3" />,
		///   <paramref name="value4" />, <paramref name="value5" />, or <paramref name="value6" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <param name="value4">The fourth value to choose.</param>
		/// <param name="value5">The fifth value to choose.</param>
		/// <param name="value6">The sixth value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>(T value1, T value2, T value3, T value4, T value5, T value6)
		{
			return _defaultChoice.Choose(value1, value2, value3, value4, value5, value6);
		}

		/// <summary>
		///   Returns one of the <paramref name="values" /> nondeterministically.
		/// </summary>
		/// <param name="values">The values to choose from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected T Choose<T>(params T[] values)
		{
			return _defaultChoice.Choose(values);
		}
	}
}