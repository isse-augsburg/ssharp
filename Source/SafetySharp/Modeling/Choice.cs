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
	using System.Runtime.CompilerServices;
	using Runtime;
	using Probability = Analysis.Probability;

	/// <summary>
	///   Represents a nondeterministic choice.
	/// </summary>
	[Hidden, NonDiscoverable]
	public sealed class Choice
	{
		/// <summary>
		///   Gets or sets the resolver that is used to resolve nondeterministic choices.
		/// </summary>
		internal ChoiceResolver Resolver { get; set; }

		/// <summary>
		///   Returns an index in the range of <paramref name="elementCount" />. Returns <c>-1</c> if <paramref name="elementCount" />
		///   is 0.
		/// </summary>
		/// <param name="elementCount">The element count to choose the index from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ChooseIndex(int elementCount)
		{
			switch (elementCount)
			{
				case 0:
					return -1;
				case 1:
					return 0;
				default:
					return Resolver.HandleChoice(elementCount);
			}
		}

		/// <summary>
		///   Returns a value within the range of the given bounds.
		/// </summary>
		/// <param name="lowerBound">The inclusive lower bound of the range to choose from.</param>
		/// <param name="upperBound">The inclusive upper bound of the range to choose from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ChooseFromRange(int lowerBound, int upperBound)
		{
			var range = upperBound - lowerBound + 1;
			if (range <= 0)
				throw new InvalidOperationException($"Invalid range [{lowerBound}, {upperBound}].");

			return lowerBound + ChooseIndex(range);
		}

		/// <summary>
		///   Deterministically returns the default value for <typeparamref name="T" />.
		/// </summary>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>()
		{
			//Resolver.Probability = Resolver.Probability;
			return default(T);
		}

		/// <summary>
		///   Deterministically returns the <paramref name="value" />.
		/// </summary>
		/// <param name="value">The value to return.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(T value)
		{
			//TODO: State machine tests fail: (Resolver==null) Resolver.Probability /= 1;
			return value;
		}

		/// <summary>
		///   Returns either <paramref name="value1" /> or <paramref name="value2" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(T value1, T value2)
		{
			Resolver.Probability /= 2;
			switch (Resolver.HandleChoice(2))
			{
				case 0:
					return value1;
				default:
					return value2;
			}
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, or <paramref name="value3" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(T value1, T value2, T value3)
		{
			Resolver.Probability /= 3;
			switch (Resolver.HandleChoice(3))
			{
				case 0:
					return value1;
				case 1:
					return value2;
				default:
					return value3;
			}
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
		public T Choose<T>(T value1, T value2, T value3, T value4)
		{
			Resolver.Probability /= 4;
			switch (Resolver.HandleChoice(4))
			{
				case 0:
					return value1;
				case 1:
					return value2;
				case 2:
					return value3;
				default:
					return value4;
			}
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
		public T Choose<T>(T value1, T value2, T value3, T value4, T value5)
		{
			Resolver.Probability /= 5;
			switch (Resolver.HandleChoice(5))
			{
				case 0:
					return value1;
				case 1:
					return value2;
				case 2:
					return value3;
				case 3:
					return value4;
				default:
					return value5;
			}
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
		public T Choose<T>(T value1, T value2, T value3, T value4, T value5, T value6)
		{
			Resolver.Probability /= 6;
			switch (Resolver.HandleChoice(6))
			{
				case 0:
					return value1;
				case 1:
					return value2;
				case 2:
					return value3;
				case 3:
					return value4;
				case 4:
					return value5;
				default:
					return value6;
			}
		}

		/// <summary>
		///   Returns one of the <paramref name="values" /> nondeterministically.
		/// </summary>
		/// <param name="values">The values to choose from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(params T[] values)
		{
			Resolver.Probability /= values.Length;
			return values[Resolver.HandleChoice(values.Length)];
		}

		/// <summary>
		///   Deterministically returns the <paramref name="value" />.
		/// </summary>
		/// <param name="value">The value to return.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(Tuple<Probability, T> value)
		{
			Resolver.Probability *= value.Item1;
			return value.Item2;
		}

		/// <summary>
		///   Returns either <paramref name="value1" /> or <paramref name="value2" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The first value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(Tuple<Probability, T> value1, Tuple<Probability, T> value2)
		{
			switch (Resolver.HandleChoice(2))
			{
				case 0:
					Resolver.Probability *= value1.Item1;
					return value1.Item2;
				default:
					Resolver.Probability *= value2.Item1;
					return value2.Item2;
			}
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, or <paramref name="value3" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The firsTuple<Probability, T> value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(Tuple<Probability, T> value1, Tuple<Probability, T> value2, Tuple<Probability, T> value3)
		{
			switch (Resolver.HandleChoice(3))
			{
				case 0:
					Resolver.Probability *= value1.Item1;
					return value1.Item2;
				case 1:
					Resolver.Probability *= value2.Item1;
					return value2.Item2;
				default:
					Resolver.Probability *= value3.Item1;
					return value3.Item2;
			}
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, <paramref name="value3" />, or
		///   <paramref name="value4" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The firsTuple<Probability, T> value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <param name="value4">The fourth value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(Tuple<Probability, T> value1, Tuple<Probability, T> value2, Tuple<Probability, T> value3, Tuple<Probability, T> value4)
		{
			switch (Resolver.HandleChoice(4))
			{
				case 0:
					Resolver.Probability *= value1.Item1;
					return value1.Item2;
				case 1:
					Resolver.Probability *= value2.Item1;
					return value2.Item2;
				case 2:
					Resolver.Probability *= value3.Item1;
					return value3.Item2;
				default:
					Resolver.Probability *= value4.Item1;
					return value4.Item2;
			}
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, <paramref name="value3" />,
		///   <paramref name="value4" />, or <paramref name="value5" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The firsTuple<Probability, T> value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <param name="value4">The fourth value to choose.</param>
		/// <param name="value5">The fifth value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(Tuple<Probability, T> value1, Tuple<Probability, T> value2, Tuple<Probability, T> value3, Tuple<Probability, T> value4, Tuple<Probability, T> value5)
		{
			switch (Resolver.HandleChoice(5))
			{
				case 0:
					Resolver.Probability *= value1.Item1;
					return value1.Item2;
				case 1:
					Resolver.Probability *= value2.Item1;
					return value2.Item2;
				case 2:
					Resolver.Probability *= value3.Item1;
					return value3.Item2;
				case 3:
					Resolver.Probability *= value4.Item1;
					return value4.Item2;
				default:
					Resolver.Probability *= value5.Item1;
					return value5.Item2;
			}
		}

		/// <summary>
		///   Returns either <paramref name="value1" />, <paramref name="value2" />, <paramref name="value3" />,
		///   <paramref name="value4" />, <paramref name="value5" />, or <paramref name="value6" /> nondeterministically.
		/// </summary>
		/// <param name="value1">The firsTuple<Probability, T> value to choose.</param>
		/// <param name="value2">The second value to choose.</param>
		/// <param name="value3">The third value to choose.</param>
		/// <param name="value4">The fourth value to choose.</param>
		/// <param name="value5">The fifth value to choose.</param>
		/// <param name="value6">The sixth value to choose.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(Tuple<Probability, T> value1, Tuple<Probability, T> value2, Tuple<Probability, T> value3, Tuple<Probability, T> value4, Tuple<Probability, T> value5, Tuple<Probability, T> value6)
		{
			switch (Resolver.HandleChoice(6))
			{
				case 0:
					Resolver.Probability *= value1.Item1;
					return value1.Item2;
				case 1:
					Resolver.Probability *= value2.Item1;
					return value2.Item2;
				case 2:
					Resolver.Probability *= value3.Item1;
					return value3.Item2;
				case 3:
					Resolver.Probability *= value4.Item1;
					return value4.Item2;
				case 4:
					Resolver.Probability *= value5.Item1;
					return value5.Item2;
				default:
					Resolver.Probability *= value6.Item1;
					return value6.Item2;
			}
		}

		/// <summary>
		///   Returns one of the <paramref name="values" /> nondeterministically.
		/// </summary>
		/// <param name="values">The values to choose from.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T Choose<T>(params Tuple<Probability, T>[] values)
		{
			var chosen = Resolver.HandleChoice(values.Length);
			Resolver.Probability *= values[chosen].Item1;
			return values[chosen].Item2;
		}


	}
}