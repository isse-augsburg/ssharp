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

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System.Collections.Generic;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using Utilities;

	/// <summary>
	///	  After ITransitionModifiers have been applied onto a TransitionCollection, there might be several transitions
	///   which only differ in their probability. This is especially the case when EarlyTermination has been applied,
	///   because former different states now go into the same Stuttering state.
	/// </summary>
	internal sealed unsafe class ConsolidateTransitionsModifier : ITransitionModifier
	{
		public int ExtraBytesInStateVector { get; } = 0;

		public int ExtraBytesOffset { get; set; }

		public int RelevantStateVectorSize { get; set; }

		private readonly List<IndexWithHash> _sortedTransitions = new List<IndexWithHash>();

		private readonly SortTransitionByHashComparer _transitionComparer = new SortTransitionByHashComparer();

		private TransitionCollection _transitions;

		private bool _useHash;

		public int UseHashLimit { get; set; } = 16;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public ConsolidateTransitionsModifier()
		{
		}

		/// <summary>
		///   Optionally modifies the <paramref name="transitions" />, changing any of their values. However, no new transitions can be
		///   added; transitions can be removed by setting their <see cref="CandidateTransition.IsValid" /> flag to <c>false</c>.
		///   During subsequent traversal steps, only valid transitions and target states reached by at least one valid transition
		///   are considered.
		/// </summary>
		/// <param name="context">The context of the model traversal.</param>
		/// <param name="worker">The worker that found the transition.</param>
		/// <param name="transitions">The transitions that should be checked.</param>
		/// <param name="sourceState">The source state of the transitions.</param>
		/// <param name="sourceStateIndex">The unique index of the transition's source state.</param>
		/// <param name="isInitial">Indicates whether the transitions are initial transitions not starting in any valid source state.</param>
		public void ModifyTransitions(TraversalContext context, Worker worker, TransitionCollection transitions, byte* sourceState, int sourceStateIndex, bool isInitial)
		{
			_transitions = transitions;
			_useHash = transitions.Count >= UseHashLimit;

			CreateSortedTransitionIndexes();
			IterateThroughAllTransitionsInSortedOrder();
		}

		private void IterateThroughAllTransitionsInSortedOrder()
		{
			var mergeInCandidateIndex = 0;

			while (mergeInCandidateIndex < _transitions.Count)
			{
				var mergeInCandidate = GetCandidateTransition(mergeInCandidateIndex);
				if (TransitionFlags.IsValid(mergeInCandidate->Flags))
				{
					MergeCandidateWithAllApplicableTargets(mergeInCandidate, mergeInCandidateIndex);
				}
				mergeInCandidateIndex++;
			}
		}

		private void MergeCandidateWithAllApplicableTargets(LtmcTransition* mergeInCandidate, int mergeInCandidateIndex)
		{
			var mergeInCandidateHash = GetCandidateHash(mergeInCandidateIndex);
			var toMergeCandidateIndex = mergeInCandidateIndex + 1;
			while (!CandidateIsOutOfIndexOrHasDifferentHash(mergeInCandidateHash,toMergeCandidateIndex))
			{
				var toMergeCandidate = GetCandidateTransition(toMergeCandidateIndex);
				if (TransitionFlags.IsValid(toMergeCandidate->Flags))
				{
					if (CanTransitionsBeMerged(mergeInCandidate, toMergeCandidate))
					{
						MergeTransitions(mergeInCandidate, toMergeCandidate);
					}
				}
				toMergeCandidateIndex++;
			}
		}

		private bool CandidateIsOutOfIndexOrHasDifferentHash(uint mergeInCandidateHash, int toMergeCandidateIndex)
		{
			if (toMergeCandidateIndex >= _transitions.Count)
			{
				return true;
			}
			var toMergeCandidateHash = GetCandidateHash(toMergeCandidateIndex);
			return mergeInCandidateHash != toMergeCandidateHash;
		}

		private LtmcTransition* GetCandidateTransition(int sortedIndex)
		{
			var indexInTransitions = _sortedTransitions[sortedIndex].Index;
			return (LtmcTransition*)_transitions[indexInTransitions];
		}

		private uint GetCandidateHash(int sortedIndex)
		{
			if (!_useHash)
				return 0;
			return _sortedTransitions[sortedIndex].Hash;
		}

		private uint HashTransition(LtmcTransition* transition)
		{
			// hashing see FNV hash at http://eternallyconfuzzled.com/tuts/algorithms/jsw_tut_hashing.aspx
			if (!TransitionFlags.IsValid(transition->Flags))
			{
				return 0;
			}
			unchecked
			{
				uint hash = 0;
				if (!TransitionFlags.IsToStutteringState(transition->Flags))
					hash = MemoryBuffer.Hash(transition->TargetStatePointer, RelevantStateVectorSize, 0);
				hash = hash * 397 ^ (uint)transition->Formulas.GetHashCode();
				return hash;
			}
		}

		public void CreateSortedTransitionIndexes()
		{
			_sortedTransitions.Clear();
			for (var i = 0; i < _transitions.Count; i++)
			{
				var newSortedTransition = new IndexWithHash();
				newSortedTransition.Index = i;
				var transition = (LtmcTransition*)_transitions[i];
				newSortedTransition.Hash = HashTransition(transition);
				_sortedTransitions.Add(newSortedTransition);
			}
			if (_useHash)
			{
				_sortedTransitions.Sort(_transitionComparer);
			}
		}

		private bool CanTransitionsBeMerged(LtmcTransition* a, LtmcTransition* b)
		{
			var aToStuttering = TransitionFlags.IsToStutteringState(a->Flags);
			var bToStuttering = TransitionFlags.IsToStutteringState(b->Flags);
			if (aToStuttering != bToStuttering)
			{
				// Target states do not match
				return false;
			}
			if (!aToStuttering)
			{
				// both target states are not the stuttering state. So check if the states match
				if (!MemoryBuffer.AreEqual(a->TargetStatePointer, b->TargetStatePointer, RelevantStateVectorSize))
					return false;
			}
			if (a->Flags != b->Flags)
				return false;
			if (a->Formulas != b->Formulas)
				return false;
			return true;
		}

		private void MergeTransitions(LtmcTransition* mergeInCandidate, LtmcTransition* toMergeCandidate)
		{
			mergeInCandidate->Probability += toMergeCandidate->Probability;
			toMergeCandidate->Flags = TransitionFlags.RemoveValid(toMergeCandidate->Flags);
		}

		private struct IndexWithHash
		{
			public long Index;
			public uint Hash;
		}

		private class SortTransitionByHashComparer : IComparer<IndexWithHash>
		{
			public int Compare(IndexWithHash x, IndexWithHash y)
			{
				return x.Hash.CompareTo(y.Hash);
			}
		}
	}
}