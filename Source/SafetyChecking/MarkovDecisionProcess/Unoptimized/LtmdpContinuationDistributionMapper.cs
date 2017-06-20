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

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using GenericDataStructures;
	using Utilities;

	internal class LtmdpContinuationDistributionMapper
	{
		private struct ContinuationElement
		{
			public int ContinuationId;
		}

		private struct DistributionElement
		{
			public int DistributionId;
		}

		// Note, _distributionOfContinuationChain.GetElementAtChainIndex(i) and
		// _continuationOfDistributionChain.GetElementAtChainIndex(i) are always associated.
		// We call this property chain-congruence in the remainder of this file.

		private readonly MultipleChainsInSingleArray<ContinuationElement> _continuationOfDistributionChain;
		
		private readonly MultipleChainsInSingleArray<DistributionElement> _distributionOfContinuationChain;

		private long _offset;
		
		public LtmdpContinuationDistributionMapper()
		{
			_continuationOfDistributionChain = new MultipleChainsInSingleArray<ContinuationElement>();
			
			_distributionOfContinuationChain = new MultipleChainsInSingleArray<DistributionElement>();
		}
		
		public void Clear()
		{
			_continuationOfDistributionChain.Clear();
			_distributionOfContinuationChain.Clear();
		}

		public void AddInitialDistributionAndContinuation(long offset)
		{
			_offset = offset;
			var indexOfNewChainElements = GetUnusedChainIndex();
			Requires.That(indexOfNewChainElements == 0, "Data structures must be empty");

			_continuationOfDistributionChain.StartChain(indexOfNewChainElements, 0, new ContinuationElement { ContinuationId = 0 });
			_distributionOfContinuationChain.StartChain(indexOfNewChainElements, 0, new DistributionElement { DistributionId = 0 });
		}

		private int GetUnusedChainIndex()
		{
			// Due to chain-congruence, _distributionOfContinuationChain.GetUnusedChainIndex could also be used. 
			var indexOfNewChainEntries = _continuationOfDistributionChain.GetUnusedChainIndex();
			return indexOfNewChainEntries;
		}

		private int GetUnusedDistributionId()
		{
			return _continuationOfDistributionChain.GetUnusedChainNumber();
		}

		public int GetNumbersOfDistributions()
		{
			return _continuationOfDistributionChain.GetNumbersOfChains();
		}

		internal bool IsDistributionEmpty(int distribution)
		{
			return !_continuationOfDistributionChain.IsChainExisting(distribution);
		}


		private void ChangeCid(int sourceCid, int newCid)
		{
			// Change entry in _distributionOfContinuationChain.
			// Entries inside _distributionOfContinuationChain can be preserved. Nothing needs to be changed there.
			_distributionOfContinuationChain.RenameChain(sourceCid, newCid);
			
			// Change entry in _continuationOfDistributionChain from sourceCid to newCid.
			// We do not need to iterate through the whole data structure because the necessary entries
			// are pointed at by the entries in _continuationOfDistributionChain.

			var docEnumerator = _distributionOfContinuationChain.GetEnumerator(newCid);

			while (docEnumerator.MoveNext())
			{
				// Note, chain-congruence!
				Assert.That(_continuationOfDistributionChain.GetElementAtChainIndex(docEnumerator.CurrentChainIndex).ContinuationId == sourceCid, "entry in _continuationOfDistributionChain is wrong");

				_continuationOfDistributionChain.ReplaceChainElement(docEnumerator.CurrentChainIndex, new ContinuationElement {ContinuationId = newCid });
			}
		}

		private void CloneCidWithinDistributions(int sourceCid, int newCid)
		{
			// Get entry in _distributionOfContinuationChain.

			var oldChainEnumerator = _distributionOfContinuationChain.GetEnumerator(sourceCid);

			// Now every entry in the _distributionOfContinuationChain needs to be traversed and cloned.
			// During the traversal, a _continuationOfDistributionChain element needs to be added for every new
			// entry. We can reuse the pointer IndexInContinuationOfDistributionChain to find the insertion point.
						
			var currentNewDofCChainIndex = -1; 
			
			while (oldChainEnumerator.MoveNext())
			{
				// Note, chain congruence
				Assert.That(_continuationOfDistributionChain.GetElementAtChainIndex(oldChainEnumerator.CurrentChainIndex).ContinuationId == sourceCid, "entry in _continuationOfDistributionChain is wrong");

				var indexOfNewChainElements = GetUnusedChainIndex();

				// Create entry in _continuationOfDistributionChain. Because we clone, we know there is already a element
				_continuationOfDistributionChain.InsertChainElement(indexOfNewChainElements, oldChainEnumerator.CurrentElement.DistributionId, oldChainEnumerator.CurrentChainIndex, new ContinuationElement {ContinuationId = newCid});
				
				// Create entry in _distributionOfContinuationChain
				var distributionElement = new DistributionElement { DistributionId = oldChainEnumerator.CurrentElement.DistributionId };
				if (currentNewDofCChainIndex == -1)
					_distributionOfContinuationChain.StartChain(indexOfNewChainElements,newCid, distributionElement);
				else
					_distributionOfContinuationChain.InsertChainElement(indexOfNewChainElements, newCid, currentNewDofCChainIndex, distributionElement);

				currentNewDofCChainIndex = indexOfNewChainElements;
			}
		}



		private void InternalProbabilisticSplit(int sourceCid, int fromCid, int toCid)
		{
			// Replace sourceCid in every distribution by the range [fromCid...toCid].
			_distributionOfContinuationChain.AssertThatChainNumberNonExistent(fromCid);
			_distributionOfContinuationChain.RequiresThatChainNumberExistent(sourceCid);
			Assert.That(fromCid <= toCid, "range [fromCid..toCid] must be ascending and contain at least one element");

			// reuse sourceCid for fromCid
			ChangeCid(sourceCid, fromCid);

			// For all others, create a clone. The cloned element is directly appended after the sourceCid in the
			// _distributionOfContinuationChain Thus, we always use the previously cloned element as basis to preserve
			// the order in _distributionOfContinuationChain. 
			var cloneSourceCid = fromCid;
			for (var newCid = fromCid+1 ; newCid <= toCid; newCid++)
			{
				CloneCidWithinDistributions(cloneSourceCid, newCid);
				cloneSourceCid = newCid;
			}
		}

		/// <summary>
		///   Makes an probabilistic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void ProbabilisticSplit(long sourceCid, long fromCid, long toCid)
		{
			var internalSourceCid = sourceCid - _offset;
			var internalFromCid = fromCid - _offset;
			var internalToCid = toCid - _offset;
			Assert.That(internalSourceCid <= int.MaxValue, "internalSourceCid<=int.MaxValue");
			Assert.That(internalFromCid <= int.MaxValue, "internalFromCid<=int.MaxValue");
			Assert.That(internalToCid <= int.MaxValue, "internalToCid<=int.MaxValue");
			InternalProbabilisticSplit((int)internalSourceCid, (int)internalFromCid, (int)internalToCid);
		}

		private void CloneDistributionWithDid(int sourceDid, int replaceCid, int replacedByCid)
		{
			// In the cloned distributions replaceCid is replaced by replacedByCid.

			// Get entry in _firstContinuationOfDistributionChainElement.
			var codEnumerator = _continuationOfDistributionChain.GetEnumerator(sourceDid);
			
			// Now every entry in the _continuationOfDistributionChain needs to be traversed and cloned.
			// During the traversal, a _distributionOfContinuationChain element needs to be added for every new
			// entry.

			var currentNewCofDChainIndex = -1;
			var newDistributionId = GetUnusedDistributionId();
			var newDistributionElement = new DistributionElement { DistributionId = newDistributionId };

			while (codEnumerator.MoveNext())
			{
				Assert.That(_distributionOfContinuationChain.GetElementAtChainIndex(codEnumerator.CurrentChainIndex).DistributionId == sourceDid, "entry in _distributionOfContinuationChain is wrong");

				var indexOfNewChainElements = GetUnusedChainIndex();

				var cidOfNewElement = codEnumerator.CurrentElement.ContinuationId;
				if (cidOfNewElement == replaceCid)
					cidOfNewElement = replacedByCid;

				// Create entry in _continuationOfDistributionChain
				var continuationElement = new ContinuationElement { ContinuationId = cidOfNewElement };
				if (currentNewCofDChainIndex == -1)
					_continuationOfDistributionChain.StartChain(indexOfNewChainElements, newDistributionId, continuationElement);
				else
					_continuationOfDistributionChain.InsertChainElement(indexOfNewChainElements, newDistributionId, currentNewCofDChainIndex, continuationElement);
				
				// Append entry in _distributionOfContinuationChain. May be a new chain
				_distributionOfContinuationChain.AppendChainElement(indexOfNewChainElements, cidOfNewElement, newDistributionElement);

				currentNewCofDChainIndex = indexOfNewChainElements;
			}
		}

		private void CloneDistributionsContainingCid(int sourceCid, int newCid)
		{
			// Clone every _distributionOfContinuationChain, which contains an element with sourceCid.
			// In the cloned distributions sourceCid is replaced by newCid.
			
			var distributionEnumerator = InternalGetDistributionsOfContinuationEnumerator(sourceCid);

			while (distributionEnumerator.MoveNext())
			{
				CloneDistributionWithDid(distributionEnumerator.CurrentDistributionId, sourceCid, newCid);
			}
		}

		public void InternalNonDeterministicSplit(int sourceCid, int fromCid, int toCid)
		{
			// For every distribution which contains sourceCid, a range of clones gets created
			// where sourceId is replaced by a different element of [fromCid..toCid].
			// Every distribution which contains sourceCid does not exist anymore after the method call.
			// For every distribution with sourceCid, there should exist #[fromCid..toCid] distributions
			// afterwards.

			_distributionOfContinuationChain.RequiresThatChainNumberExistent(sourceCid);
			_distributionOfContinuationChain.AssertThatChainNumberNonExistent(fromCid);

			Assert.That(fromCid <= toCid, "range [fromCid..toCid] must be ascending and contain at least one element");

			// Reuse sourceCid for the first distribution to create
			ChangeCid(sourceCid, fromCid);

			// For all others, create a clone. 
			var cloneSourceCid = fromCid;
			for (var newCid = fromCid + 1; newCid <= toCid; newCid++)
			{
				CloneDistributionsContainingCid(cloneSourceCid, newCid);
			}
		}

		/// <summary>
		///   Makes an non deterministic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void NonDeterministicSplit(long sourceCid, long fromCid, long toCid)
		{
			var internalSourceCid = sourceCid - _offset;
			var internalFromCid = fromCid - _offset;
			var internalToCid = toCid - _offset;
			Assert.That(internalSourceCid <= int.MaxValue, "internalSourceCid<=int.MaxValue");
			Assert.That(internalFromCid <= int.MaxValue, "internalFromCid<=int.MaxValue");
			Assert.That(internalToCid <= int.MaxValue, "internalToCid<=int.MaxValue");
			InternalNonDeterministicSplit((int)internalSourceCid, (int)internalFromCid, (int)internalToCid);
		}

		private DistributionsOfContinuationEnumerator InternalGetDistributionsOfContinuationEnumerator(int continuationId)
		{
			return new DistributionsOfContinuationEnumerator(this, continuationId);
		}
		
		public DistributionsOfContinuationEnumerator GetDistributionsOfContinuationEnumerator(long continuationId)
		{
			var internalContinuationId = continuationId - _offset;
			Assert.That(internalContinuationId <= int.MaxValue, "internalContinuationId<=int.MaxValue");
			return InternalGetDistributionsOfContinuationEnumerator((int)internalContinuationId);
		}

		internal struct DistributionsOfContinuationEnumerator
		{
			public int CurrentDistributionId => _distributionOfContinuationChainEnumerator.CurrentElement.DistributionId;

			private MultipleChainsInSingleArray<DistributionElement>.IndexedMulitListEnumerator _distributionOfContinuationChainEnumerator;

			public DistributionsOfContinuationEnumerator(LtmdpContinuationDistributionMapper cidToDidMapper, int continuationId)
			{
				_distributionOfContinuationChainEnumerator = cidToDidMapper._distributionOfContinuationChain.GetEnumerator(continuationId);
			}

			public bool MoveNext()
			{
				return _distributionOfContinuationChainEnumerator.MoveNext();
			}
		}

		
		public ContinuationsOfDistributionEnumerator GetContinuationsOfDistributionEnumerator(int distributionId)
		{
			return new ContinuationsOfDistributionEnumerator(this, distributionId);
		}

		internal struct ContinuationsOfDistributionEnumerator
		{
			public long CurrentContinuationId => _continuationsOfDistributionChainEnumerator.CurrentElement.ContinuationId + _offset;

			private long _offset;

			private MultipleChainsInSingleArray<ContinuationElement>.IndexedMulitListEnumerator _continuationsOfDistributionChainEnumerator;

			public ContinuationsOfDistributionEnumerator(LtmdpContinuationDistributionMapper cidToDidMapper, int distributionId)
			{
				_continuationsOfDistributionChainEnumerator = cidToDidMapper._continuationOfDistributionChain.GetEnumerator(distributionId);
				_offset = cidToDidMapper._offset;
			}

			public bool MoveNext()
			{
				return _continuationsOfDistributionChainEnumerator.MoveNext();
			}
		}
	}
}
