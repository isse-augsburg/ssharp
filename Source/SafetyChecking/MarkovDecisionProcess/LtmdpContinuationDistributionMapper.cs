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

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using GenericDataStructures;
	using Utilities;

	internal class LtmdpContinuationDistributionMapper
	{
		private struct ContinuationOfDistributionChainElement
		{
			public int ContinuationId;
			public int NextElementIndex;
			public int PreviousElementIndex;
		}

		private struct DistributionOfContinuationChainElement
		{
			public int DistributionId;
			public int NextElementIndex;
			public int PreviousElementIndex;
		}

		// Note, _distributionOfContinuationChain[i] and _continuationOfDistributionChain[i] are always associated.
		// We call this property chain-congruence in the remainder of this file.

		private readonly AutoResizeVector<int> _firstContinuationOfDistributionChainElement;
		private readonly AutoResizeVector<int> _lastContinuationOfDistributionChainElement;
		private readonly AutoResizeVector<ContinuationOfDistributionChainElement> _continuationOfDistributionChain;

		private readonly AutoResizeVector<int> _firstDistributionOfContinuationChainElement;
		private readonly AutoResizeVector<int> _lastDistributionOfContinuationChainElement;
		private readonly AutoResizeVector<DistributionOfContinuationChainElement> _distributionOfContinuationChain;

		private readonly List<int> _freedChainIndexes;
		
		public LtmdpContinuationDistributionMapper()
		{
			_firstContinuationOfDistributionChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_lastContinuationOfDistributionChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_continuationOfDistributionChain = new AutoResizeVector<ContinuationOfDistributionChainElement>();

			_firstDistributionOfContinuationChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_lastDistributionOfContinuationChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_distributionOfContinuationChain = new AutoResizeVector<DistributionOfContinuationChainElement>();
			_freedChainIndexes = new List<int>();
		}
		
		public void Clear()
		{
			_firstContinuationOfDistributionChainElement.Clear();
			_lastContinuationOfDistributionChainElement.Clear();
			_continuationOfDistributionChain.Clear();

			_firstDistributionOfContinuationChainElement.Clear();
			_lastDistributionOfContinuationChainElement.Clear();
			_distributionOfContinuationChain.Clear();

			_freedChainIndexes.Clear();
		}

		[Conditional("DEBUG")]
		private void AssertThatCidNonExistent(int cid)
		{
			Assert.That(_firstDistributionOfContinuationChainElement[cid]==-1,"cid must not exist");
		}

		private void RequiresThatCidExistent(int cid)
		{
			Requires.That(_firstDistributionOfContinuationChainElement[cid] != -1, "cid must exist");
		}

		private int GetUnusedChainIndex()
		{
			// Due to chain-congruence, _distributionOfContinuationChain.Count could also be used. 
			var indexOfNewChainEntries = _continuationOfDistributionChain.Count;
			return indexOfNewChainEntries;
		}

		private int GetUnusedDistributionId()
		{
			return _firstContinuationOfDistributionChainElement.Count;
		}


		private void StartCofDChain(int indexOfNewChainEntry, int distribution, int cid)
		{
			Assert.That(_firstContinuationOfDistributionChainElement[distribution] == -1, "Chain should be empty");
			
			_firstContinuationOfDistributionChainElement[distribution] = indexOfNewChainEntry;
			_lastContinuationOfDistributionChainElement[distribution] = indexOfNewChainEntry;

			_continuationOfDistributionChain[indexOfNewChainEntry] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = cid,
					NextElementIndex = -1,
					PreviousElementIndex = -1
				};
		}

		private void ReplaceCofDChainElement(int indexOfChainEntry, int newCid)
		{
			_continuationOfDistributionChain[indexOfChainEntry] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = newCid,
					NextElementIndex = _continuationOfDistributionChain[indexOfChainEntry].NextElementIndex,
					PreviousElementIndex = _continuationOfDistributionChain[indexOfChainEntry].PreviousElementIndex
				};
		}

		private void InsertCofDChainElement(int indexOfNewChainEntry, int did, int indexOfPreviousChainElement, int newCid)
		{
			var previousNextElement = _continuationOfDistributionChain[indexOfPreviousChainElement].NextElementIndex;
			var previousPreviousElement = _continuationOfDistributionChain[indexOfPreviousChainElement].PreviousElementIndex;

			_continuationOfDistributionChain[indexOfNewChainEntry] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = newCid,
					NextElementIndex = previousNextElement,
					PreviousElementIndex = indexOfPreviousChainElement
				};

			_continuationOfDistributionChain[indexOfPreviousChainElement] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = _continuationOfDistributionChain[indexOfPreviousChainElement].ContinuationId,
					NextElementIndex = indexOfNewChainEntry,
					PreviousElementIndex = previousPreviousElement
				};

			if (previousNextElement == -1)
			{
				_lastContinuationOfDistributionChainElement[did] = indexOfNewChainEntry;
			}
			else
			{
				_continuationOfDistributionChain[previousNextElement] =
					new ContinuationOfDistributionChainElement
					{
						ContinuationId = _continuationOfDistributionChain[previousNextElement].ContinuationId,
						NextElementIndex = _distributionOfContinuationChain[previousNextElement].NextElementIndex,
						PreviousElementIndex = indexOfNewChainEntry
					};
			}
		}

		private void StartDofCChain(int indexOfNewChainEntry, int cid, int distribution)
		{
			Assert.That(_firstDistributionOfContinuationChainElement[cid] == -1, "Chain should be empty");
			
			_firstDistributionOfContinuationChainElement[cid] = indexOfNewChainEntry;
			_lastDistributionOfContinuationChainElement[cid] = indexOfNewChainEntry;

			_distributionOfContinuationChain[indexOfNewChainEntry] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = distribution,
					NextElementIndex = -1,
					PreviousElementIndex = -1
				};
		}

		private void ReplaceDofCChainElement(int indexOfChainEntry, int newDid)
		{
			_distributionOfContinuationChain[indexOfChainEntry] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = newDid,
					NextElementIndex = _distributionOfContinuationChain[indexOfChainEntry].NextElementIndex,
					PreviousElementIndex = _distributionOfContinuationChain[indexOfChainEntry].PreviousElementIndex
				};
		}

		private void InsertDofCChainElement(int indexOfNewChainEntry, int cid, int indexOfPreviousChainElement, int distribution)
		{
			var previousNextElement = _distributionOfContinuationChain[indexOfPreviousChainElement].NextElementIndex;
			var previousPreviousElement = _distributionOfContinuationChain[indexOfPreviousChainElement].PreviousElementIndex;

			_distributionOfContinuationChain[indexOfNewChainEntry] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = distribution,
					NextElementIndex = previousNextElement,
					PreviousElementIndex = indexOfPreviousChainElement
				};

			_distributionOfContinuationChain[indexOfPreviousChainElement] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = _distributionOfContinuationChain[indexOfPreviousChainElement].DistributionId,
					NextElementIndex = indexOfNewChainEntry,
					PreviousElementIndex = previousPreviousElement
				};

			if (previousNextElement == -1)
			{
				_lastDistributionOfContinuationChainElement[cid] = indexOfNewChainEntry;
			}
			else
			{
				_distributionOfContinuationChain[previousNextElement] =
					new DistributionOfContinuationChainElement
					{
						DistributionId = _distributionOfContinuationChain[previousNextElement].DistributionId,
						NextElementIndex = _distributionOfContinuationChain[previousNextElement].NextElementIndex,
						PreviousElementIndex = indexOfNewChainEntry
					};
			}
		}

		private void AppendDofCChainElement(int indexOfChainElement, int cid, int distribution)
		{
			var indexOfPreviousElement = _lastDistributionOfContinuationChainElement[cid];
			if (indexOfPreviousElement == -1)
			{
				StartDofCChain(indexOfChainElement, cid, distribution);
			}
			else
			{
				InsertDofCChainElement(indexOfChainElement, cid, indexOfPreviousElement, distribution);
			}

			Assert.That(_distributionOfContinuationChain[indexOfChainElement].NextElementIndex==-1,"New element should not have any successor");
		}

		private bool RemoveDofCChainElement(int cid, int indexOfChainEntry)
		{
			// returns true, if indexOfChainEntry is the only element in the chain.

			var dofCChainElement = _distributionOfContinuationChain[indexOfChainEntry];

			if (dofCChainElement.PreviousElementIndex == -1 && dofCChainElement.NextElementIndex == -1)
			{
				// this is the only element
				_firstDistributionOfContinuationChainElement[cid] = -1;
				_lastDistributionOfContinuationChainElement[cid] = -1;
				return true;
			}

			var indexOfNextChainElement = dofCChainElement.NextElementIndex;
			var indexOfPreviousChainElement = dofCChainElement.PreviousElementIndex;

			if (indexOfNextChainElement != -1)
			{
				// the current element has a successor
				_distributionOfContinuationChain[indexOfNextChainElement] =
					new DistributionOfContinuationChainElement
					{
						DistributionId = _distributionOfContinuationChain[indexOfNextChainElement].DistributionId,
						NextElementIndex = _distributionOfContinuationChain[indexOfNextChainElement].NextElementIndex,
						PreviousElementIndex = indexOfPreviousChainElement
					};
			}
			else
			{
				//removed the last element
				_lastDistributionOfContinuationChainElement[cid] = indexOfPreviousChainElement;
			}

			if (indexOfPreviousChainElement != -1)
			{
				// the current element has a predecessor
				_distributionOfContinuationChain[indexOfPreviousChainElement] =
					new DistributionOfContinuationChainElement
					{
						DistributionId = _distributionOfContinuationChain[indexOfPreviousChainElement].DistributionId,
						NextElementIndex = indexOfNextChainElement,
						PreviousElementIndex = _distributionOfContinuationChain[indexOfPreviousChainElement].PreviousElementIndex
					};
			}
			else
			{
				//removed the first element
				_firstDistributionOfContinuationChainElement[cid] = indexOfNextChainElement;
			}
			return false;
		}

		private void ChangeCid(int sourceCid, int newCid)
		{
			// Change entry in _firstDistributionOfContinuationChainElement.
			var currentChainIndex = _firstDistributionOfContinuationChainElement[sourceCid];
			_firstDistributionOfContinuationChainElement[sourceCid] = -1;
			_firstDistributionOfContinuationChainElement[newCid] = currentChainIndex;

			// Entries in _distributionOfContinuationChain can be preserved. Nothing needs to be changed there.

			// Change entry in _continuationOfDistributionChain from sourceCid to newCid.
			// We do not need to iterate through the whole data structure because the necessary entries
			// are pointed at by the entries in _continuationOfDistributionChain.

			while (currentChainIndex != -1)
			{
				var oldDofCChainElement = _distributionOfContinuationChain[currentChainIndex];

				Assert.That(_continuationOfDistributionChain[currentChainIndex].ContinuationId == sourceCid, "entry in _continuationOfDistributionChain is wrong");

				ReplaceCofDChainElement(currentChainIndex, newCid);

				currentChainIndex = oldDofCChainElement.NextElementIndex;
			}
		}

		private void CloneCidWithinDistributions(int sourceCid, int newCid)
		{
			// Get entry in _firstDistributionOfContinuationChainElement.
			var currentOldChainIndex = _firstDistributionOfContinuationChainElement[sourceCid];

			// Now every entry in the _distributionOfContinuationChain needs to be traversed and cloned.
			// During the traversal, a _continuationOfDistributionChain element needs to be added for every new
			// entry. We can reuse the pointer IndexInContinuationOfDistributionChain to find the insertion point.
						
			var currentNewDofCChainIndex = -1; 
			
			while (currentOldChainIndex != -1)
			{
				var oldDofCChainElement = _distributionOfContinuationChain[currentOldChainIndex];
				
				Assert.That(_continuationOfDistributionChain[currentOldChainIndex].ContinuationId == sourceCid, "entry in _continuationOfDistributionChain is wrong");

				var indexOfNewChainElements = GetUnusedChainIndex();

				// Create entry in _continuationOfDistributionChain. Because we clone, we know there is already a element
				InsertCofDChainElement(indexOfNewChainElements, oldDofCChainElement.DistributionId, currentOldChainIndex, newCid);

				// Create entry in _distributionOfContinuationChain
				if (currentNewDofCChainIndex == -1)
					StartDofCChain(indexOfNewChainElements,newCid, oldDofCChainElement.DistributionId);
				else
					InsertDofCChainElement(indexOfNewChainElements, newCid, currentNewDofCChainIndex, oldDofCChainElement.DistributionId);

				currentNewDofCChainIndex = indexOfNewChainElements;
				currentOldChainIndex = oldDofCChainElement.NextElementIndex;
			}
		}


		public void AddInitialDistributionAndContinuation()
		{
			var indexOfNewChainElements = GetUnusedChainIndex();
			StartCofDChain(indexOfNewChainElements, 0, 0);
			StartDofCChain(indexOfNewChainElements, 0, 0);

			Requires.That(indexOfNewChainElements == 0, "Data structures must be empty");
		}

		/// <summary>
		///   Makes an probabilistic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void ProbabilisticSplit(int sourceCid, int fromCid, int toCid)
		{
			// Replace sourceCid in every distribution by the range [fromCid...toCid].

			RequiresThatCidExistent(sourceCid);
			AssertThatCidNonExistent(fromCid);
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

		private void CloneDistributionWithDid(int sourceDid, int replaceCid, int replacedByCid)
		{
			// In the cloned distributions replaceCid is replaced by replacedByCid.

			// Get entry in _firstContinuationOfDistributionChainElement.
			var currentOldChainIndex = _firstContinuationOfDistributionChainElement[sourceDid];

			// Now every entry in the _continuationOfDistributionChain needs to be traversed and cloned.
			// During the traversal, a _distributionOfContinuationChain element needs to be added for every new
			// entry. We can reuse the pointer IndexInContinuationOfDistributionChain to find the insertion point.

			var currentNewCofDChainIndex = -1;
			var newDistributionId = GetUnusedDistributionId();

			while (currentOldChainIndex != -1)
			{
				var oldCofDChainElement = _continuationOfDistributionChain[currentOldChainIndex];

				Assert.That(_distributionOfContinuationChain[currentOldChainIndex].DistributionId == sourceDid, "entry in _distributionOfContinuationChain is wrong");

				var indexOfNewChainElements = GetUnusedChainIndex();

				var cidOfNewElement = oldCofDChainElement.ContinuationId;
				if (cidOfNewElement == replaceCid)
					cidOfNewElement = replacedByCid;

				// Create entry in _continuationOfDistributionChain
				if (currentNewCofDChainIndex == -1)
					StartCofDChain(indexOfNewChainElements, newDistributionId, cidOfNewElement);
				else
					InsertCofDChainElement(indexOfNewChainElements, newDistributionId, currentNewCofDChainIndex, cidOfNewElement);
				
				// Append entry in _distributionOfContinuationChain. May be a new chain
				AppendDofCChainElement(indexOfNewChainElements, cidOfNewElement, newDistributionId);

				currentNewCofDChainIndex = indexOfNewChainElements;
				currentOldChainIndex = oldCofDChainElement.NextElementIndex;
			}
		}

		private void CloneDistributionsContainingCid(int sourceCid, int newCid)
		{
			// Clone every _distributionOfContinuationChain, which contains an element with sourceCid.
			// In the cloned distributions sourceCid is replaced by newCid.
			
			var distributionEnumerator = GetDistributionsOfContinuationEnumerator(sourceCid);

			while (distributionEnumerator.MoveNext())
			{
				CloneDistributionWithDid(distributionEnumerator.CurrentDistributionId, sourceCid, newCid);
			}
		}
		
		/// <summary>
		///   Makes an non deterministic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void NonDeterministicSplit(int sourceCid, int fromCid, int toCid)
		{
			// For every distribution which contains sourceCid, a range of clones gets created
			// where sourceId is replaced by a different element of [fromCid..toCid].
			// Every distribution which contains sourceCid does not exist anymore after the method call.
			// For every distribution with sourceCid, there should exist #[fromCid..toCid] distributions
			// afterwards.

			RequiresThatCidExistent(sourceCid);
			AssertThatCidNonExistent(fromCid);
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
		
		private void RemoveDistribution(int did)
		{
			var currentChainIndex = _firstContinuationOfDistributionChainElement[did];
			
			while (currentChainIndex != -1)
			{
				_freedChainIndexes.Add(currentChainIndex);

				var currentCofDChainElement = _continuationOfDistributionChain[currentChainIndex];

				Assert.That(_distributionOfContinuationChain[currentChainIndex].DistributionId == did, "entry in _distributionOfContinuationChain is wrong");

				RemoveDofCChainElement(currentCofDChainElement.ContinuationId, currentChainIndex);
				
				currentChainIndex = currentCofDChainElement.NextElementIndex;
			}
			_firstContinuationOfDistributionChainElement[did] = -1;
		}

		public void RemoveDistributionsWithCid(int cid)
		{
			var distributionEnumerator = GetDistributionsOfContinuationEnumerator(cid);

			while (distributionEnumerator.MoveNext())
			{
				RemoveDistribution(distributionEnumerator.CurrentDistributionId);
			}
		}


		public void RemoveCidInDistributions(int cid)
		{
			// Useful to undo a probabilistic split.
			// Assume, that the element with cid is not the last element in any distribution.

			var currentChainIndex = _firstDistributionOfContinuationChainElement[cid];
			while (currentChainIndex != -1)
			{
				_freedChainIndexes.Add(currentChainIndex);

				var currentDofCChainElement = _distributionOfContinuationChain[currentChainIndex];
				
				RemoveDofCChainElement(cid, currentChainIndex);

				currentChainIndex = currentDofCChainElement.NextElementIndex;
			}
			_firstDistributionOfContinuationChainElement[cid] = -1;
		}


		public DistributionsOfContinuationEnumerator GetDistributionsOfContinuationEnumerator(int continuationId)
		{
			return new DistributionsOfContinuationEnumerator(this, continuationId);
		}

		internal struct DistributionsOfContinuationEnumerator
		{
			private int _currentIndex;

			private bool _isFirst;

			public int CurrentDistributionId { get; private set; }

			public int ContinuationId { get; }

			private readonly AutoResizeVector<DistributionOfContinuationChainElement> _distributionOfContinuationChain;

			public DistributionsOfContinuationEnumerator(LtmdpContinuationDistributionMapper cidToDidMapper, int continuationId)
			{
				_distributionOfContinuationChain = cidToDidMapper._distributionOfContinuationChain;
				ContinuationId = continuationId;
				_currentIndex = cidToDidMapper._firstDistributionOfContinuationChainElement[continuationId];
				CurrentDistributionId = -1;
				_isFirst = true;
			}

			public bool MoveNext()
			{
				if (_isFirst)
				{
					_isFirst = false;
				}
				else
				{
					_currentIndex = _distributionOfContinuationChain[_currentIndex].NextElementIndex;
				}
				if (_currentIndex == -1)
				{
					return false;
				}
				CurrentDistributionId = _distributionOfContinuationChain[_currentIndex].DistributionId;
				return true;
			}
		}
	}
}
