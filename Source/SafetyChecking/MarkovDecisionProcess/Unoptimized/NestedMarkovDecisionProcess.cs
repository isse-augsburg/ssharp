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

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using System.Runtime.InteropServices;
	using Modeling;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using ExecutedModel;
	using Formula;
	using GenericDataStructures;
	using Utilities;
	using System.Collections.Generic;

	public unsafe class NestedMarkovDecisionProcess
	{
		public static readonly int TransitionSize = sizeof(ContinuationGraphElement);
		private const int StateSize = sizeof(int);

		public string[] StateFormulaLabels { get; set; }

		public string[] StateRewardRetrieverLabels;

		private long _indexOfInitialContinuationGraphRoot = -1;
		private readonly MemoryBuffer _stateToRootOfContinuationGraphBuffer = new MemoryBuffer();
		private readonly long* _stateToRootOfContinuationGraphMemory;

		private readonly MemoryBuffer _continuationGraphBuffer = new MemoryBuffer();
		private readonly ContinuationGraphElement* _continuationGraph;
		private long _continuationGraphElementCount = 0;

		private readonly long _maxNumberOfContinuationGraphElements;

		public LabelVector StateLabeling { get; }

		public NestedMarkovDecisionProcess(ModelCapacity modelCapacity)
		{
			var modelSize = modelCapacity.DeriveModelByteSize(StateSize, TransitionSize);

			StateLabeling = new LabelVector();
			States = (int) modelSize.NumberOfStates;
			
			_maxNumberOfContinuationGraphElements = modelSize.NumberOfTransitions;
			_maxNumberOfContinuationGraphElements = Math.Max(_maxNumberOfContinuationGraphElements, 1024);
			
			Requires.InRange(_maxNumberOfContinuationGraphElements, nameof(_maxNumberOfContinuationGraphElements), 1024, Int32.MaxValue - 1);

			_stateToRootOfContinuationGraphBuffer.Resize(States * sizeof(long), zeroMemory: false);
			_stateToRootOfContinuationGraphMemory = (long*)_stateToRootOfContinuationGraphBuffer.Pointer;

			_continuationGraphBuffer.Resize((long)_maxNumberOfContinuationGraphElements * sizeof(ContinuationGraphElement), zeroMemory: false);
			_continuationGraph = (ContinuationGraphElement*)_continuationGraphBuffer.Pointer;

			MemoryBuffer.SetAllBitsMemoryWithInitblk.ClearWithMinus1(_stateToRootOfContinuationGraphMemory, States);
		}

		// Retrieving matrix phase

		[StructLayout(LayoutKind.Explicit, Size = 32)]
		public struct ContinuationGraphElement
		{
			[FieldOffset(0)]
			public LtmdpChoiceType ChoiceType;

			public bool IsChoiceTypeUnsplitOrFinal => ChoiceType == LtmdpChoiceType.UnsplitOrFinal;

			public bool IsChoiceTypeForward => ChoiceType == LtmdpChoiceType.Forward;

			public bool IsChoiceTypeNondeterministic => ChoiceType == LtmdpChoiceType.Nondeterministic;

			public bool IsChoiceTypeProbabilitstic => ChoiceType == LtmdpChoiceType.Probabilitstic;

			[FieldOffset(0)]
			public ContinuationGraphLeaf AsLeaf;
			
			[FieldOffset(8)]
			public double Probability;
		}

		[StructLayout(LayoutKind.Explicit, Size = 32)]
		public struct ContinuationGraphInnerNode
		{
			[FieldOffset(0)]
			public LtmdpChoiceType ChoiceType;

			[FieldOffset(8)]
			public double Probability;

			[FieldOffset(16)]
			public long FromCid;

			[FieldOffset(24)]
			public long ToCid;
		}

		[StructLayout(LayoutKind.Explicit, Size = 32)]
		public struct ContinuationGraphLeaf
		{
			[FieldOffset(0)]
			public LtmdpChoiceType ChoiceType;

			[FieldOffset(8)]
			public double Probability;

			[FieldOffset(16)]
			public int ToState;
		}

		public ContinuationGraphElement GetContinuationGraphElement(long position)
		{
			return _continuationGraph[position];
		}

		public ContinuationGraphInnerNode GetContinuationGraphInnerNode(long position)
		{
			Assert.That(!_continuationGraph[position].IsChoiceTypeUnsplitOrFinal, "must be an inner node");
			return ((ContinuationGraphInnerNode*)_continuationGraph)[position];
		}

		public ContinuationGraphLeaf GetContinuationGraphLeaf(long position)
		{
			Assert.That(_continuationGraph[position].IsChoiceTypeUnsplitOrFinal, "must be a leaf node");
			return ((ContinuationGraphLeaf*)_continuationGraph)[position];
		}

		public long GetPlaceForNewContinuationGraphElements(long number)
		{
			var locationOfFirstNewEntry = InterlockedExtensions.AddFetch(ref _continuationGraphElementCount, number);
			if (locationOfFirstNewEntry >= _maxNumberOfContinuationGraphElements)
				throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			return locationOfFirstNewEntry;
		}
		
		public void AddContinuationGraphLeaf(long locationForContinuationGraphElement, int targetState, double probability)
		{
			((ContinuationGraphLeaf*)_continuationGraph)[locationForContinuationGraphElement] =
				new ContinuationGraphLeaf
				{
					ChoiceType = LtmdpChoiceType.UnsplitOrFinal,
					ToState = targetState,
					Probability = probability,
				};
		}

		public void AddContinuationGraphInnerNode(long locationForContinuationGraphElement, LtmdpChoiceType choiceType, long fromCid, long toCid, double probability)
		{
			Assert.That(choiceType!=LtmdpChoiceType.UnsplitOrFinal,"must not be final node");
			((ContinuationGraphInnerNode*)_continuationGraph)[locationForContinuationGraphElement] =
				new ContinuationGraphInnerNode
				{
					ChoiceType = choiceType,
					FromCid = fromCid,
					ToCid = toCid,
					Probability = probability,
				};
		}

		internal long GetRootContinuationGraphLocationOfInitialState()
		{
			return _indexOfInitialContinuationGraphRoot;
		}

		internal long GetRootContinuationGraphLocationOfState(int state)
		{
			return _stateToRootOfContinuationGraphMemory[state];
		}
		
		internal void SetRootContinuationGraphLocationOfState(int state, long locationOfStateRoot)
		{
			_stateToRootOfContinuationGraphMemory[state] = locationOfStateRoot;
		}

		internal void SetRootContinuationGraphLocationOfInitialState(long locationOfStateRoot)
		{
			_indexOfInitialContinuationGraphRoot = locationOfStateRoot;
		}

		public long ContinuationGraphSize => _continuationGraphElementCount;
		
		public int States { get; }
		
		public int StateToRowsEntryOfInitialDistributions = 0;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int StateToColumn(int state) => state; //Do nothing! Just here to make the algorithms more clear.

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ColumnToState(int state) => state; //Do nothing! Just here to make the algorithms more clear.


		internal void SetStateLabeling(int state, StateFormulaSet formula)
		{
			StateLabeling[state] = formula;
		}

		public Func<int, bool> CreateFormulaEvaluator(Formula formula)
		{
			var stateFormulaEvaluator = StateFormulaSetEvaluatorCompilationVisitor.Compile(StateFormulaLabels, formula);
			Func<int, bool> evaluator = transitionTarget =>
			{
				var stateFormulaSet = StateLabeling[transitionTarget];
				return stateFormulaEvaluator(stateFormulaSet);
			};
			return evaluator;
		}

		internal TreeTraversal GetTreeTraverser(long parentContinuationId)
		{
			return new TreeTraversal(this, parentContinuationId);
		}

		internal struct TreeTraversal
		{
			public long ParentContinuationId { get; }

			public NestedMarkovDecisionProcess Nmdp { get; }

			public TreeTraversal(NestedMarkovDecisionProcess nmdp, long parentContinuationId)
			{
				ParentContinuationId = parentContinuationId;
				Nmdp = nmdp;
			}

			public void ApplyActionWithStackBasedAlgorithm(Action<ContinuationGraphElement> action)
			{
				// also shows how to traverse a tree with stacks and without recursion
				var fromDecisionStack = new Stack<long>();
				var toDecisionStack = new Stack<long>();

				fromDecisionStack.Push(ParentContinuationId);
				toDecisionStack.Push(ParentContinuationId);

				while (fromDecisionStack.Count > 0)
				{
					// go to next leaf in tree
					var foundNextLeaf = false;
					var cgl = default(ContinuationGraphLeaf);
					while (!foundNextLeaf)
					{
						// select current fromCid
						var fromCid = fromDecisionStack.Peek();
						var cge = Nmdp.GetContinuationGraphElement(fromCid);
						// found new cge
						action(cge);

						if (cge.IsChoiceTypeUnsplitOrFinal)
						{
							foundNextLeaf = true;
							cgl = Nmdp.GetContinuationGraphLeaf(fromCid);
						}
						else
						{
							var cgi = Nmdp.GetContinuationGraphInnerNode(fromCid);
							fromDecisionStack.Push(cgi.FromCid);
							toDecisionStack.Push(cgi.ToCid);
						}
					}

					// here we can work with the next leaf

					// find next fromCid
					var foundNextFromCid = false;
					while (fromDecisionStack.Count > 0 && !foundNextFromCid)
					{
						var nextFromCid = fromDecisionStack.Pop() + 1;
						var toCid = toDecisionStack.Peek();
						if (nextFromCid > toCid)
						{
							toDecisionStack.Pop();
						}
						else
						{
							fromDecisionStack.Push(nextFromCid);
							foundNextFromCid = true;
						}
					}
					Assert.That(fromDecisionStack.Count == toDecisionStack.Count, "Stacks must have equal size");
				}
			}

			private void ApplyActionWithRecursionBasedAlgorithmInnerRecursion(Action<ContinuationGraphElement> action, long currentCid)
			{
				ContinuationGraphElement cge = Nmdp.GetContinuationGraphElement(currentCid);
				action(cge);
				if (cge.IsChoiceTypeUnsplitOrFinal)
				{
				}
				else
				{
					var cgi = Nmdp.GetContinuationGraphInnerNode(currentCid);
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						ApplyActionWithRecursionBasedAlgorithmInnerRecursion(action, i);
					}
				}
			}

			public void ApplyActionWithRecursionBasedAlgorithm(Action<ContinuationGraphElement> action)
			{
				ApplyActionWithRecursionBasedAlgorithmInnerRecursion(action, ParentContinuationId);
			}
		}

		internal UnderlyingDigraph CreateUnderlyingDigraph()
		{
			return new UnderlyingDigraph(this);
		}
		
		internal class UnderlyingDigraph
		{
			public BidirectionalGraph BaseGraph { get; }

			public UnderlyingDigraph(NestedMarkovDecisionProcess mdp)
			{
				//Assumption "every node is reachable" is fulfilled due to the construction
				BaseGraph = new BidirectionalGraph();

				var currentState = 0;
				Action<ContinuationGraphElement> addTargetState = cge =>
				{
					if (cge.IsChoiceTypeUnsplitOrFinal)
					{
						var cgl = cge.AsLeaf;
						if (cgl.Probability > 0.0)
							BaseGraph.AddVerticesAndEdge(new Edge(currentState, cgl.ToState));
					}
				};

				for (currentState = 0; currentState < mdp.States; currentState++)
				{
					var parentCid = mdp.GetRootContinuationGraphLocationOfState(currentState);
					var treeTraverser = mdp.GetTreeTraverser(parentCid);
					treeTraverser.ApplyActionWithStackBasedAlgorithm(addTargetState);
				}
			}
		}
	}
}
