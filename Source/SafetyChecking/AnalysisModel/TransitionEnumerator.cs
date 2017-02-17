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

namespace ISSE.SafetyChecking.AnalysisModel
{
	/// <summary>
	///   Enumerates a <see cref="TransitionCollection" />.
	/// </summary>
	internal unsafe struct TransitionEnumerator
	{
		private readonly byte* _transitions;
		private readonly int _count;
		private readonly int _transitionSize;
		private int _current;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="transitions">The transition instances stored in a contiguous array.</param>
		/// <param name="count">The number of transitions contained in the set; not all of these transitions are valid.</param>
		/// <param name="transitionSize">The size of a single transition in bytes.</param>
		public TransitionEnumerator(Transition* transitions, int count, int transitionSize)
			: this()
		{
			_transitions = (byte*)transitions;
			_count = count;
			_transitionSize = transitionSize;
			_current = -1;
		}

		/// <summary>
		///   Advances the enumerator to the next element of the collection.
		/// </summary>
		public bool MoveNext()
		{
			++_current;

			while (_current < _count)
			{
				if (((CandidateTransition*)Current)->IsValid)
					return true;

				++_current;
			}

			return false;
		}

		/// <summary>
		///   Gets the element in the collection at the current position of the enumerator.
		/// </summary>
		public Transition* Current => (Transition*)(_transitions + _current * _transitionSize);
	}
}