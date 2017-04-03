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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking.GenericDataStructures;

namespace ISSE.SafetyChecking.Utilities
{
	public class IndexedMultiList<T> where T : struct
	{
		// This list is deterministic

		private struct ListChainElement
		{
			public T Element;
			public int NextElementIndex;
			public int PreviousElementIndex;
		}

		private readonly AutoResizeVector<int> _firstChainElement;
		private readonly AutoResizeVector<int> _lastChainElement;
		private readonly AutoResizeVector<ListChainElement> _chain;


		private readonly List<int> _freedChainIndexes;

		public IndexedMultiList()
		{
			_firstChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_lastChainElement = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_chain = new AutoResizeVector<ListChainElement>();

			_freedChainIndexes = new List<int>();
		}

		public void Clear()
		{
			_firstChainElement.Clear();
			_lastChainElement.Clear();
			_chain.Clear();

			_freedChainIndexes.Clear();
		}

		private int GetUnusedChainIndex()
		{
			// Due to chain-congruence, _distributionOfContinuationChain.Count could also be used. 
			var indexOfNewChainEntries = _chain.Count;
			return indexOfNewChainEntries;
		}
		

		public void StartChain(int indexOfNewChainEntry, int number, T cid)
		{
			Assert.That(_firstChainElement[number] == -1, "Chain should be empty");

			_firstChainElement[number] = indexOfNewChainEntry;
			_lastChainElement[number] = indexOfNewChainEntry;

			_chain[indexOfNewChainEntry] =
				new ListChainElement
				{
					Element = cid,
					NextElementIndex = -1,
					PreviousElementIndex = -1
				};
		}

		public void ReplaceChainElement(int indexOfChainEntry, T newCid)
		{
			_chain[indexOfChainEntry] =
				new ListChainElement
				{
					Element = newCid,
					NextElementIndex = _chain[indexOfChainEntry].NextElementIndex,
					PreviousElementIndex = _chain[indexOfChainEntry].PreviousElementIndex
				};
		}

		public void InsertChainElement(int indexOfNewChainEntry, int number, int indexOfPreviousChainElement, T newCid)
		{
			var previousNextElement = _chain[indexOfPreviousChainElement].NextElementIndex;
			var previousPreviousElement = _chain[indexOfPreviousChainElement].PreviousElementIndex;

			_chain[indexOfNewChainEntry] =
				new ListChainElement
				{
					Element = newCid,
					NextElementIndex = previousNextElement,
					PreviousElementIndex = indexOfPreviousChainElement
				};

			_chain[indexOfPreviousChainElement] =
				new ListChainElement
				{
					Element = _chain[indexOfPreviousChainElement].Element,
					NextElementIndex = indexOfNewChainEntry,
					PreviousElementIndex = previousPreviousElement
				};

			if (previousNextElement == -1)
			{
				_lastChainElement[number] = indexOfNewChainEntry;
			}
			else
			{
				_chain[previousNextElement] =
					new ListChainElement
					{
						Element = _chain[previousNextElement].Element,
						NextElementIndex = _distributionOfContinuationChain[previousNextElement].NextElementIndex,
						PreviousElementIndex = indexOfNewChainEntry
					};
			}
		}

		public void AppendChainElement(int indexOfChainElement, int number, T element)
		{
			var indexOfPreviousElement = _lastChainElement[number];
			if (indexOfPreviousElement == -1)
			{
				StartChain(indexOfChainElement, number, element);
			}
			else
			{
				InsertChainElement(indexOfChainElement, number, indexOfPreviousElement, element);
			}

			Assert.That(_chain[indexOfChainElement].NextElementIndex == -1, "New element should not have any successor");
		}

		public bool RemoveChainElement(int number, int indexOfChainEntry)
		{
			// returns true, if indexOfChainEntry is the only element in the chain.

			var chainElement = _chain[indexOfChainEntry];

			if (chainElement.PreviousElementIndex == -1 && chainElement.NextElementIndex == -1)
			{
				// this is the only element
				_firstChainElement[number] = -1;
				_lastChainElement[number] = -1;
				return true;
			}

			var indexOfNextChainElement = chainElement.NextElementIndex;
			var indexOfPreviousChainElement = chainElement.PreviousElementIndex;

			if (indexOfNextChainElement != -1)
			{
				// the current element has a successor
				_chain[indexOfNextChainElement] =
					new ListChainElement
					{
						Element = _chain[indexOfNextChainElement].Element,
						NextElementIndex = _chain[indexOfNextChainElement].NextElementIndex,
						PreviousElementIndex = indexOfPreviousChainElement
					};
			}
			else
			{
				//removed the last element
				_lastChainElement[number] = indexOfPreviousChainElement;
			}

			if (indexOfPreviousChainElement != -1)
			{
				// the current element has a predecessor
				_chain[indexOfPreviousChainElement] =
					new ListChainElement
					{
						Element = _chain[indexOfNextChainElement].Element,
						NextElementIndex = indexOfNextChainElement,
						PreviousElementIndex = _chain[indexOfPreviousChainElement].PreviousElementIndex
					};
			}
			else
			{
				//removed the first element
				_firstChainElement[number] = indexOfNextChainElement;
			}
			return false;
		}
	}
}
