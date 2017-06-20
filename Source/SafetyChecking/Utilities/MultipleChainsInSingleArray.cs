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
	using System.Diagnostics;

	public class MultipleChainsInSingleArray<T> where T : struct
	{
		// This list is deterministic

		private struct ListChainElement
		{
			public T Element;
			public int NextElementIndex;
			public int PreviousElementIndex;
		}

		private readonly AutoResizeVector<int> _firstChainElementOfChainNumber;
		private readonly AutoResizeVector<int> _lastChainElementOfChainNumber;
		private readonly AutoResizeVector<ListChainElement> _chain;


		private readonly Stack<int> _freedChainIndexes;

		public MultipleChainsInSingleArray()
		{
			_firstChainElementOfChainNumber = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_lastChainElementOfChainNumber = new AutoResizeVector<int>
			{
				DefaultValue = -1
			};
			_chain = new AutoResizeVector<ListChainElement>();

			_freedChainIndexes = new Stack<int>();
		}

		public void Clear()
		{
			_firstChainElementOfChainNumber.Clear();
			_lastChainElementOfChainNumber.Clear();
			_chain.Clear();

			_freedChainIndexes.Clear();
		}

		public int GetUnusedChainIndex()
		{
			if (_freedChainIndexes.Count>0)
			{
				return _freedChainIndexes.Pop();
			}

			var indexOfNewChainEntries = _chain.Count;
			return indexOfNewChainEntries;
		}

		public int GetUnusedChainNumber()
		{
			return _firstChainElementOfChainNumber.Count;
		}

		public int GetNumbersOfChains()
		{
			return _firstChainElementOfChainNumber.Count;
		}

		public T GetElementAtChainIndex(int chainIndex)
		{
			return _chain[chainIndex].Element;
		}

		public void RenameChain(int fromNumber, int toNumber)
		{
			var firstChainIndex = _firstChainElementOfChainNumber[fromNumber];
			_firstChainElementOfChainNumber[fromNumber] = -1;
			_firstChainElementOfChainNumber[toNumber] = firstChainIndex;

			var lastChainIndex = _lastChainElementOfChainNumber[fromNumber];
			_lastChainElementOfChainNumber[fromNumber] = -1;
			_lastChainElementOfChainNumber[toNumber] = lastChainIndex;
		}


		[Conditional("DEBUG")]
		public void AssertThatChainNumberNonExistent(int number)
		{
			Assert.That(_firstChainElementOfChainNumber[number] == -1, "number must not exist");
		}

		public void RequiresThatChainNumberExistent(int number)
		{
			Requires.That(_firstChainElementOfChainNumber[number] != -1, "number must exist");
		}


		public void StartChain(int indexOfNewChainEntry, int number, T newElement)
		{
			Assert.That(_firstChainElementOfChainNumber[number] == -1, "Chain should be empty");

			_firstChainElementOfChainNumber[number] = indexOfNewChainEntry;
			_lastChainElementOfChainNumber[number] = indexOfNewChainEntry;

			_chain[indexOfNewChainEntry] =
				new ListChainElement
				{
					Element = newElement,
					NextElementIndex = -1,
					PreviousElementIndex = -1
				};
		}

		public bool IsChainExisting(int number)
		{
			return _firstChainElementOfChainNumber[number] != -1;
		}

		public T GetFirstChainElement(int number)
		{
			Assert.That(_firstChainElementOfChainNumber[number] != -1, "Chain should be empty");

			var indexOfNewChainEntry = _firstChainElementOfChainNumber[number];

			return _chain[indexOfNewChainEntry].Element;
		}

		public void ReplaceChainElement(int indexOfChainEntry, T newElement)
		{
			_chain[indexOfChainEntry] =
				new ListChainElement
				{
					Element = newElement,
					NextElementIndex = _chain[indexOfChainEntry].NextElementIndex,
					PreviousElementIndex = _chain[indexOfChainEntry].PreviousElementIndex
				};
		}

		public void InsertChainElement(int indexOfNewChainEntry, int number, int indexOfPreviousChainElement, T newElement)
		{
			var previousNextElement = _chain[indexOfPreviousChainElement].NextElementIndex;
			var previousPreviousElement = _chain[indexOfPreviousChainElement].PreviousElementIndex;

			_chain[indexOfNewChainEntry] =
				new ListChainElement
				{
					Element = newElement,
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
				_lastChainElementOfChainNumber[number] = indexOfNewChainEntry;
			}
			else
			{
				_chain[previousNextElement] =
					new ListChainElement
					{
						Element = _chain[previousNextElement].Element,
						NextElementIndex = _chain[previousNextElement].NextElementIndex,
						PreviousElementIndex = indexOfNewChainEntry
					};
			}
		}

		public void AppendChainElement(int indexOfChainElement, int number, T element)
		{
			var indexOfPreviousElement = _lastChainElementOfChainNumber[number];
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
			_freedChainIndexes.Push(indexOfChainEntry);

			var chainElement = _chain[indexOfChainEntry];

			if (chainElement.PreviousElementIndex == -1 && chainElement.NextElementIndex == -1)
			{
				// this is the only element
				_firstChainElementOfChainNumber[number] = -1;
				_lastChainElementOfChainNumber[number] = -1;
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
				_lastChainElementOfChainNumber[number] = indexOfPreviousChainElement;
			}

			if (indexOfPreviousChainElement != -1)
			{
				// the current element has a predecessor
				_chain[indexOfPreviousChainElement] =
					new ListChainElement
					{
						Element = _chain[indexOfPreviousChainElement].Element,
						NextElementIndex = indexOfNextChainElement,
						PreviousElementIndex = _chain[indexOfPreviousChainElement].PreviousElementIndex
					};
			}
			else
			{
				//removed the first element
				_firstChainElementOfChainNumber[number] = indexOfNextChainElement;
			}
			return false;
		}

		public void RemoveSuccessors(int number, int indexOfChainEntryToKeep)
		{
			Assert.That(indexOfChainEntryToKeep!=-1,"ChainElement must exist");
			
			var indexOfChainEntryToDelete = _chain[indexOfChainEntryToKeep].NextElementIndex;

			_lastChainElementOfChainNumber[number] = indexOfChainEntryToKeep;
			_chain[indexOfChainEntryToKeep] =
				new ListChainElement
				{
					Element = _chain[indexOfChainEntryToKeep].Element,
					NextElementIndex = -1,
					PreviousElementIndex = _chain[indexOfChainEntryToKeep].PreviousElementIndex
				};

			while (indexOfChainEntryToDelete != -1)
			{
				_freedChainIndexes.Push(indexOfChainEntryToDelete);
				indexOfChainEntryToDelete = _chain[indexOfChainEntryToDelete].NextElementIndex;
			}
		}

		public void RemoveChainNumber(int number)
		{
			var enumerator = GetEnumerator(number);
			
			while (enumerator.MoveNext())
			{
				_freedChainIndexes.Push(enumerator.CurrentChainIndex);
			}
			_firstChainElementOfChainNumber[number] = -1;
			_lastChainElementOfChainNumber[number] = -1;
		}

		public IndexedMulitListEnumerator GetEnumerator(int number)
		{
			return new IndexedMulitListEnumerator(this, number);
		}

		public struct IndexedMulitListEnumerator
		{
			private bool _isFirst;

			public T CurrentElement { get; private set; }

			public int CurrentChainIndex { get; private set; }

			public int Number { get; }

			private readonly AutoResizeVector<ListChainElement> _chain;

			public IndexedMulitListEnumerator(MultipleChainsInSingleArray<T> list, int continuationId)
			{
				_chain = list._chain;
				Number = continuationId;
				CurrentChainIndex = list._firstChainElementOfChainNumber[continuationId];
				CurrentElement = default(T);
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
					CurrentChainIndex = _chain[CurrentChainIndex].NextElementIndex;
				}
				if (CurrentChainIndex == -1)
				{
					return false;
				}
				CurrentElement = _chain[CurrentChainIndex].Element;
				return true;
			}
		}
	}
}
