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

namespace ISSE.SafetyChecking.AnalysisModel
{
	using Utilities;

	/// <summary>
	///   Represents a collection of <see cref="CandidateTransition" /> instances.
	/// </summary>
	internal unsafe struct TransitionCollection
	{
		/// <summary>
		///   The transition instances stored in a contiguous array.
		/// </summary>
		private readonly Transition* _transitions;

		/// <summary>
		///   The number of transitions contained in the set; not all of these transitions are valid.
		/// </summary>
		public readonly int Count;

		/// <summary>
		///   The total number of all originally computed transitions.
		/// </summary>
		public readonly int TotalCount;

		/// <summary>
		///   The size of a single transition in bytes.
		/// </summary>
		private readonly int _transitionSize;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="transitions">The transition instances stored in a contiguous array.</param>
		/// <param name="count">The number of transitions contained in the set; not all of these transitions are valid.</param>
		/// <param name="transitionSize">The size of a single transition in bytes.</param>
		public TransitionCollection(Transition* transitions, int count, int transitionSize)
			: this(transitions, count, count, transitionSize)
		{
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="transitions">The transition instances stored in a contiguous array.</param>
		/// <param name="count">The number of transitions contained in the set; not all of these transitions are valid.</param>
		/// <param name="totalCount">The total number of all originally computed transitions.</param>
		/// <param name="transitionSize">The size of a single transition in bytes.</param>
		public TransitionCollection(Transition* transitions, int count, int totalCount, int transitionSize)
		{
			_transitions = transitions;
			_transitionSize = transitionSize;

			Count = count;
			TotalCount = totalCount;
		}

		/// <summary>
		///   Gets an enumerator that can be used to iterate through the collection.
		/// </summary>
		public TransitionEnumerator GetEnumerator()
		{
			return new TransitionEnumerator(_transitions, Count, _transitionSize);
		}

		/// <summary>
		///   Validates the sizes of <see cref="Transition" />-related types.
		/// </summary>
		internal static void ValidateTransitionSizes()
		{
			Requires.That(sizeof(Transition) == 24, "Unexpected transition size.");
			Requires.That(sizeof(CandidateTransition) == 24, "Unexpected candidate transitions size.");
			Requires.That(sizeof(StateFormulaSet) == 4, "Unexpected state formula set size.");
			Requires.That(sizeof(FaultSet) == 8, "Unexpected fault set size.");

			Transition t;
			var c = (CandidateTransition*)&t;

			Requires.That(&t.ActivatedFaults == &c->ActivatedFaults, $"Invalid offset of standard transition field '{nameof(t.ActivatedFaults)}.");
			Requires.That(&t.Formulas == &c->Formulas, $"Invalid offset of standard transition field '{nameof(t.Formulas)}.");
		}
	}
}