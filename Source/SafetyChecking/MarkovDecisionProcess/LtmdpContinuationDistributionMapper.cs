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
	using System.Diagnostics;
	using GenericDataStructures;
	using Utilities;

	internal class LtmdpContinuationDistributionMapper
	{
		private struct ContinuationOfDistributionChainElement
		{
			public int ContinuationId;
			public int NextElementIndex;
		}

		private struct DistributionOfContinuationChainElement
		{
			public int DistributionId;
			public int NextElementIndex;
		}

		// Note, _distributionOfContinuationChain[i] and _continuationOfDistributionChain[i] are always associated.
		// We call this property chain-congruence in the remainder of this file.

		private readonly AutoResizeVector<int> _firstContinuationOfDistributionChainElement;
		private readonly AutoResizeVector<ContinuationOfDistributionChainElement> _continuationOfDistributionChain;

		private readonly AutoResizeVector<int> _firstDistributionOfContinuationChainElement;
		private readonly AutoResizeVector<DistributionOfContinuationChainElement> _distributionOfContinuationChain;

		private int ContinuationOfDistributionChainElementCount => _continuationOfDistributionChain.Count;
		private int DistributionOfContinuationChainElementCount => _distributionOfContinuationChain.Count;

		public LtmdpContinuationDistributionMapper()
		{
			_firstContinuationOfDistributionChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_continuationOfDistributionChain = new AutoResizeVector<ContinuationOfDistributionChainElement>();

			_firstDistributionOfContinuationChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_distributionOfContinuationChain = new AutoResizeVector<DistributionOfContinuationChainElement>();
		}
		
		public void Clear()
		{
			_firstContinuationOfDistributionChainElement.Clear();
			_continuationOfDistributionChain.Clear();

			_firstDistributionOfContinuationChainElement.Clear();
			_distributionOfContinuationChain.Clear();
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


		private void StartCofDChain(int indexOfNewChainEntry, int distribution, int cid)
		{
			Assert.That(_firstContinuationOfDistributionChainElement[distribution] == -1, "Chain should be empty");
			
			_firstContinuationOfDistributionChainElement[distribution] = indexOfNewChainEntry;
			
			_continuationOfDistributionChain[indexOfNewChainEntry] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = cid,
					NextElementIndex = -1
				};
		}

		private void ReplaceCofDChainElement(int indexOfChainEntry, int newCid)
		{
			_continuationOfDistributionChain[indexOfChainEntry] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = newCid,
					NextElementIndex = _continuationOfDistributionChain[indexOfChainEntry].NextElementIndex
				};
		}

		private void AppendCofDChainElement(int indexOfNewChainEntry, int indexOfPreviousChainElement, int newCid)
		{
			_continuationOfDistributionChain[indexOfNewChainEntry] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = newCid,
					NextElementIndex = _continuationOfDistributionChain[indexOfPreviousChainElement].NextElementIndex
				};

			_continuationOfDistributionChain[indexOfPreviousChainElement] =
				new ContinuationOfDistributionChainElement
				{
					ContinuationId = _continuationOfDistributionChain[indexOfPreviousChainElement].ContinuationId,
					NextElementIndex = indexOfNewChainEntry
				};
		}

		private void StartDofCChain(int indexOfNewChainEntry, int cid, int distribution)
		{
			Assert.That(_firstDistributionOfContinuationChainElement[cid] == -1, "Chain should be empty");
			
			_firstDistributionOfContinuationChainElement[cid] = indexOfNewChainEntry;

			_distributionOfContinuationChain[indexOfNewChainEntry] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = distribution,
					NextElementIndex = -1
				};
		}

		private void ReplaceDofCChainElement(int indexOfChainEntry, int newDid)
		{
			_distributionOfContinuationChain[indexOfChainEntry] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = newDid,
					NextElementIndex = _distributionOfContinuationChain[indexOfChainEntry].NextElementIndex
				};
		}

		private void AppendDofCChainElement(int indexOfNewChainEntry, int indexOfPreviousChainElement, int distribution)
		{
			_distributionOfContinuationChain[indexOfNewChainEntry] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = distribution,
					NextElementIndex = _distributionOfContinuationChain[indexOfPreviousChainElement].NextElementIndex
				};

			_distributionOfContinuationChain[indexOfPreviousChainElement] =
				new DistributionOfContinuationChainElement
				{
					DistributionId = _distributionOfContinuationChain[indexOfPreviousChainElement].DistributionId,
					NextElementIndex = indexOfNewChainEntry
				};
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

		private void CloneCid(int sourceCid, int newCid)
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
				AppendCofDChainElement(indexOfNewChainElements, currentOldChainIndex, newCid);

				// Create entry in _distributionOfContinuationChain
				if (currentNewDofCChainIndex == -1)
					StartDofCChain(indexOfNewChainElements,newCid, oldDofCChainElement.DistributionId);
				else
					AppendDofCChainElement(indexOfNewChainElements,currentNewDofCChainIndex, oldDofCChainElement.DistributionId);
				
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
		}



		/// <summary>
		///   Makes an probabilistic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void ProbabilisticSplit(int sourceCid, int fromCid, int toCid)
		{
			// replace sourceCid in every distribution by the range [fromCid...toCid].
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
				CloneCid(cloneSourceCid, newCid);
				cloneSourceCid = newCid;
			}
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
