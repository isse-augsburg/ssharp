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
		public struct Choice
		{
			public readonly int From;
			public readonly int To;
			public readonly LtmdpChoiceType ChoiceType;
			
			public Choice(int from, int to, LtmdpChoiceType choiceType)
			{
				From=from;
				To=to;
				ChoiceType=choiceType;
			}

			public Choice DeriveDeterministic()
			{
				return new Choice(From,From,LtmdpChoiceType.Deterministic);
			}

			public bool IsChoiceTypeUnsplitOrFinal => ChoiceType == LtmdpChoiceType.UnsplitOrFinal;

			public bool IsChoiceTypeDeterministic => ChoiceType == LtmdpChoiceType.Deterministic;

			public bool IsChoiceTypeNondeterministic => ChoiceType == LtmdpChoiceType.Nondeterministic;

			public bool IsChoiceTypeProbabilitstic => ChoiceType == LtmdpChoiceType.Probabilitstic;
		}
		
		private readonly AutoResizeVector<Choice> _internalGraph = new AutoResizeVector<Choice>();

		public void Clear()
		{
			_internalGraph.Clear();
			AddInitialContinuation();
		}

		public Choice GetChoiceOfCid(int cid)
		{
			return _internalGraph[cid];
		}

		private void AddInitialContinuation()
		{
			Requires.That(_internalGraph.Count == 0, "Data structure must be empty");
			_internalGraph[0] = new Choice(0,0,LtmdpChoiceType.UnsplitOrFinal);
		}

		public void MakeChoiceOfCidDeterministic(int cid)
		{
			_internalGraph[cid] = _internalGraph[cid].DeriveDeterministic();
		}

		public int Size => _internalGraph.Count;
		
		/// <summary>
		///   Makes an non deterministic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void NonDeterministicSplit(int sourceCid, int fromCid, int toCid)
		{
			Assert.That(_internalGraph.Count - 1 < fromCid, "fromCid has already been declared.");
			Assert.That(sourceCid < fromCid && sourceCid < toCid, "sourceCid must be smaller than childrenCids");
			Assert.That(fromCid <= toCid, "range [fromCid..toCid] must be ascending and contain at least one element");

			_internalGraph[sourceCid] = new Choice(fromCid,toCid,LtmdpChoiceType.Nondeterministic);
		}


		/// <summary>
		///   Makes an probabilistic split.
		/// </summary>
		/// <param name="sourceCid">The source continuation id that is split..</param>
		/// <param name="fromCid">The new beginning of the range. Includes fromCid.</param>
		/// <param name="toCid">The new end of the range. Includes toCid.</param>
		public void ProbabilisticSplit(int sourceCid, int fromCid, int toCid)
		{
			Assert.That(_internalGraph.Count -1 < fromCid, "fromCid has already been declared.");
			Assert.That(sourceCid < fromCid && sourceCid < toCid, "sourceCid must be smaller than childrenCids");
			Assert.That(fromCid <= toCid, "range [fromCid..toCid] must be ascending and contain at least one element");

			_internalGraph[sourceCid] = new Choice(fromCid, toCid, LtmdpChoiceType.Probabilitstic);
		}


		public DirectChildrenEnumerator GetDirectChildrenEnumerator(int parentContinuationId)
		{
			return new DirectChildrenEnumerator(this, parentContinuationId);
		}

		internal struct DirectChildrenEnumerator
		{
			public int ParentContinuationId { get; }

			public int CurrentChildContinuationId { private set; get; }

			public Choice Choice { get; }

			public DirectChildrenEnumerator(LtmdpStepGraph ltmdpStepGraph, int parentContinuationId)
			{
				Choice = ltmdpStepGraph.GetChoiceOfCid(parentContinuationId);
				CurrentChildContinuationId = Choice.From - 1;
				ParentContinuationId = parentContinuationId;
			}

			public bool MoveNext()
			{
				if (Choice.IsChoiceTypeUnsplitOrFinal)
					return false;
				CurrentChildContinuationId++;
				if (CurrentChildContinuationId <= Choice.To)
					return true;
				return false;
			}
		}
	}
}
