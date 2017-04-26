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

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using GenericDataStructures;
	using Utilities;

	internal class LtmdpStepGraph
	{
		/// <summary>
		///   A continuationId has either been finished or is
		/// </summary>
		private const int ChoiceTypeUnsplitOrFinal = 0;

		/// <summary>
		///   Represents a deterministic choice in _choiceTypeOfContinuationId (only one choice available)
		/// </summary>
		private const int ChoiceTypeDeterministic = 1;

		/// <summary>
		///   Represents a non deterministic choice in _choiceTypeOfContinuationId
		/// </summary>
		private const int ChoiceTypeNondeterministic = 2;

		/// <summary>
		///   Represents a probabilistic choice in _choiceTypeOfContinuationId
		/// </summary>
		private const int ChoiceTypeProbabilitstic = 4;

		private readonly AutoResizeVector<int> _choiceTypeOfContinuationId = new AutoResizeVector<int>();

		private readonly MultipleChainsInSingleArray<int> _internalGraph = new MultipleChainsInSingleArray<int>();

		public void Clear()
		{
			_choiceTypeOfContinuationId.Clear();
			_internalGraph.Clear();
			AddInitialContinuation();
		}

		private void AddInitialContinuation()
		{
			Requires.That(_choiceTypeOfContinuationId.Count == 0, "Data structures must be empty");
			_choiceTypeOfContinuationId[0] = ChoiceTypeUnsplitOrFinal;
		}

		public void MakeChoiceOfCidDeterministic(int cid)
		{
			_choiceTypeOfContinuationId[cid] = ChoiceTypeDeterministic;
			var firstChild = _internalGraph.GetFirstChainElement(cid);
			_internalGraph.RemoveSuccessors(cid,firstChild);
		}

		private void CreateNodeWithChildrenInGraph(int sourceCid, int fromCid, int toCid)
		{
			Assert.That(_choiceTypeOfContinuationId[sourceCid]== ChoiceTypeUnsplitOrFinal, "Children have already been declared.");
			Assert.That(!_internalGraph.IsChainExisting(sourceCid), "Children have already been declared.");

			for (var i = fromCid; i <= toCid; i++)
			{
				var chainNumberInGraph = _internalGraph.GetUnusedChainNumber();
				_internalGraph.AppendChainElement(chainNumberInGraph, sourceCid, i);
			}
		}

		private void CreateFreshContinuations(int fromCid, int toCid)
		{
			for (var i = fromCid; i <= toCid; i++)
				_choiceTypeOfContinuationId[i] = ChoiceTypeUnsplitOrFinal;
		}

		/// <summary>
		///   Makes an non deterministic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void NonDeterministicSplit(int sourceCid, int fromCid, int toCid)
		{
			_internalGraph.RequiresThatChainNumberExistent(sourceCid);
			_internalGraph.AssertThatChainNumberNonExistent(fromCid);
			Assert.That(sourceCid < fromCid && sourceCid < toCid, "sourceCid must be smaller than childrenCids");
			Assert.That(fromCid <= toCid, "range [fromCid..toCid] must be ascending and contain at least one element");

			_choiceTypeOfContinuationId[sourceCid] = ChoiceTypeNondeterministic;

			CreateNodeWithChildrenInGraph(sourceCid, fromCid, toCid);

			CreateFreshContinuations(fromCid, toCid);
		}


		/// <summary>
		///   Makes an probabilistic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void ProbabilisticSplit(int sourceCid, int fromCid, int toCid)
		{
			_internalGraph.RequiresThatChainNumberExistent(sourceCid);
			_internalGraph.AssertThatChainNumberNonExistent(fromCid);
			Assert.That(sourceCid < fromCid && sourceCid < toCid, "sourceCid must be smaller than childrenCids");
			Assert.That(fromCid <= toCid, "range [fromCid..toCid] must be ascending and contain at least one element");

			_choiceTypeOfContinuationId[sourceCid] = ChoiceTypeProbabilitstic;

			CreateNodeWithChildrenInGraph(sourceCid, fromCid, toCid);

			CreateFreshContinuations(fromCid, toCid);
		}


		public DirectChildrenEnumerator GetDirectChildrenEnumerator(int parentContinuationId)
		{
			return new DirectChildrenEnumerator(this, parentContinuationId);
		}

		internal struct DirectChildrenEnumerator
		{
			public int ChildContinuationId => _chainEnumerator.CurrentElement;

			public int ParentContinuationId { get; }

			public int ChoiceType { get; }

			public bool IsChoiceTypeUnsplitOrFinal => ChoiceType == ChoiceTypeUnsplitOrFinal;

			public bool IsChoiceTypeDeterministic => ChoiceType == ChoiceTypeDeterministic;

			public bool IsChoiceTypeNondeterministic => ChoiceType == ChoiceTypeNondeterministic;

			public bool IsChoiceTypeProbabilitstic => ChoiceType == ChoiceTypeProbabilitstic;

			private MultipleChainsInSingleArray<int>.IndexedMulitListEnumerator _chainEnumerator;

			public DirectChildrenEnumerator(LtmdpStepGraph ltmdpStepGraph, int parentContinuationId)
			{
				ChoiceType = ltmdpStepGraph._choiceTypeOfContinuationId[parentContinuationId];
				_chainEnumerator = ltmdpStepGraph._internalGraph.GetEnumerator(parentContinuationId);
				ParentContinuationId = parentContinuationId;
			}

			public bool MoveNext()
			{
				return _chainEnumerator.MoveNext();
			}
		}
	}
}
