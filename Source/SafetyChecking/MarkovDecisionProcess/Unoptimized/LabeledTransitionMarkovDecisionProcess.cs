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
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using Modeling;
	using Utilities;
	using AnalysisModel;
	using ExecutableModel;
	using Formula;
	using GenericDataStructures;

	public unsafe partial class LabeledTransitionMarkovDecisionProcess : DisposableObject
	{
		public static readonly int TransitionSize = sizeof(TransitionTargetElement);
		private const int AvgGraphNodesPerSucceedingState = 7;

		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;
		
		private long _indexOfInitialContinuationGraphRoot = -1;

		public ConcurrentBag<int> SourceStates { get; } = new ConcurrentBag<int>();

		private readonly MemoryBuffer _stateStorageStateToRootOfContinuationGraphBuffer = new MemoryBuffer();
		private readonly long* _stateStorageStateToRootOfContinuationGraphMemory;
		
		private readonly MemoryBuffer _continuationGraphBuffer = new MemoryBuffer();
		private readonly ContinuationGraphElement* _continuationGraph;
		private long _continuationGraphElementCount = 0;

		private readonly long _maxNumberOfContinuationGraphElements;

		private readonly MemoryBuffer _transitionTargetBuffer = new MemoryBuffer();
		private readonly TransitionTargetElement* _transitionTarget;
		private int _transitionTargetCount = 0;

		private readonly long _maxNumberOfTransitionTargets;


		public LabeledTransitionMarkovDecisionProcess(long maxNumberOfStates, long maxNumberOfTransitions)
		{
			Requires.InRange(maxNumberOfStates, nameof(maxNumberOfStates), 1024, Int32.MaxValue - 1);

			_maxNumberOfTransitionTargets = maxNumberOfTransitions/(AvgGraphNodesPerSucceedingState+1);
			_maxNumberOfContinuationGraphElements = _maxNumberOfTransitionTargets * AvgGraphNodesPerSucceedingState;

			Requires.InRange(_maxNumberOfTransitionTargets, nameof(_maxNumberOfTransitionTargets), 1024, Int32.MaxValue - 1);
			Requires.InRange(_maxNumberOfContinuationGraphElements, nameof(_maxNumberOfContinuationGraphElements), 1024, Int32.MaxValue - 1);

			_stateStorageStateToRootOfContinuationGraphBuffer.Resize((long)maxNumberOfStates * sizeof(long), zeroMemory: false);
			_stateStorageStateToRootOfContinuationGraphMemory = (long*)_stateStorageStateToRootOfContinuationGraphBuffer.Pointer;

			_continuationGraphBuffer.Resize((long)_maxNumberOfContinuationGraphElements * sizeof(ContinuationGraphElement), zeroMemory: false);
			_continuationGraph = (ContinuationGraphElement*)_continuationGraphBuffer.Pointer;

			_transitionTargetBuffer.Resize((long)_maxNumberOfTransitionTargets * sizeof(TransitionTargetElement), zeroMemory: false);
			_transitionTarget = (TransitionTargetElement*)_transitionTargetBuffer.Pointer;
			
			MemoryBuffer.SetAllBitsMemoryWithInitblk.ClearWithMinus1(_stateStorageStateToRootOfContinuationGraphMemory, maxNumberOfStates);
		}
		
		public struct ContinuationGraphElement
		{
			public LtmdpChoiceType ChoiceType;
			public long From;
			public long To;
			public double Probability;

			public bool IsChoiceTypeUnsplitOrFinal => ChoiceType == LtmdpChoiceType.UnsplitOrFinal;

			public bool IsChoiceTypeForward => ChoiceType == LtmdpChoiceType.Forward;

			public bool IsChoiceTypeNondeterministic => ChoiceType == LtmdpChoiceType.Nondeterministic;

			public bool IsChoiceTypeProbabilitstic => ChoiceType == LtmdpChoiceType.Probabilitstic;
		}

		public long ContinuationGraphSize => _continuationGraphElementCount;

		public struct TransitionTargetElement
		{
			public int TargetState;
			public StateFormulaSet Formulas;
		}

		public long TransitionTargets => _transitionTargetCount;

		private long GetPlaceForNewContinuationGraphElements(int number)
		{
			var locationOfFirstNewEntry = InterlockedExtensions.AddFetch(ref _continuationGraphElementCount,number);
			if (locationOfFirstNewEntry >= _maxNumberOfContinuationGraphElements)
				throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			return locationOfFirstNewEntry;
		}

		private long GetPlaceForNewTransitionTargetElement()
		{
			var locationOfNewEntry = InterlockedExtensions.IncrementReturnOld(ref _transitionTargetCount);
			if (locationOfNewEntry >= _maxNumberOfTransitionTargets)
				throw new OutOfMemoryException("Unable to store distribution.");
			return locationOfNewEntry;
		}

		public void CreateStutteringState(int stutteringStateIndex)
		{
			// The stuttering state might not be reached at all.
			// Make sure, that all used algorithms to not require a connected state graph.
			var currentElementIndex = _stateStorageStateToRootOfContinuationGraphMemory[stutteringStateIndex];
			Assert.That(currentElementIndex == -1, "Stuttering state has already been created");

			var locationOfNewContinuationGraphElement = GetPlaceForNewContinuationGraphElements(1);
			var locationOfNewTransitionTargetElement = GetPlaceForNewTransitionTargetElement();

			_continuationGraph[locationOfNewContinuationGraphElement] =
					new ContinuationGraphElement
					{
						ChoiceType=LtmdpChoiceType.UnsplitOrFinal,
						To = locationOfNewTransitionTargetElement,
						Probability = 1.0,
					};

			_transitionTarget[locationOfNewTransitionTargetElement] =
					new TransitionTargetElement
					{
						Formulas = new StateFormulaSet(),
						TargetState = stutteringStateIndex
					};

			SourceStates.Add(stutteringStateIndex);
			_stateStorageStateToRootOfContinuationGraphMemory[stutteringStateIndex] = locationOfNewContinuationGraphElement;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_stateStorageStateToRootOfContinuationGraphBuffer.SafeDispose();
			_continuationGraphBuffer.SafeDispose();
			_transitionTargetBuffer.SafeDispose();
		}

		internal long GetRootContinuationGraphLocationOfState(int state)
		{
			return _stateStorageStateToRootOfContinuationGraphMemory[state];
		}

		internal ContinuationGraphElement GetRootContinuationGraphElementOfState(int state)
		{
			var location = GetRootContinuationGraphLocationOfState(state);
			return _continuationGraph[location];
		}

		internal long GetRootContinuationGraphLocationOfInitialState()
		{
			return _indexOfInitialContinuationGraphRoot;
		}

		public ContinuationGraphElement GetRootContinuationGraphElementOfInitialState()
		{
			var location = _indexOfInitialContinuationGraphRoot;
			return _continuationGraph[location];
		}

		public ContinuationGraphElement GetContinuationGraphElement(long position)
		{
			return _continuationGraph[position];
		}


		public TransitionTargetElement GetTransitionTarget(long position)
		{
			return _transitionTarget[position];
		}
		
		public Func<int, bool> CreateFormulaEvaluator(Formula formula)
		{
			var stateFormulaEvaluator = StateFormulaSetEvaluatorCompilationVisitor.Compile(StateFormulaLabels, formula);
			Func<int, bool> evaluator = transitionTarget =>
			{
				var stateFormulaSet = _transitionTarget[transitionTarget].Formulas;
				return stateFormulaEvaluator(stateFormulaSet);
			};
			return evaluator;
		}

		[Conditional("DEBUG")]
		internal void AssertIsDense()
		{
			var size = SourceStates.Count;
			using (var enumerator = SourceStates.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Assert.That(enumerator.Current < size, "Markov chain must be dense");
				}
			}
		}

		internal TreeTraversal GetTreeTraverser(long parentContinuationId)
		{
			return new TreeTraversal(this, parentContinuationId);
		}

		internal struct TreeTraversal
		{
			public long ParentContinuationId { get; }

			public LabeledTransitionMarkovDecisionProcess Ltmdp { get; }

			public TreeTraversal(LabeledTransitionMarkovDecisionProcess ltmdp, long parentContinuationId)
			{
				ParentContinuationId = parentContinuationId;
				Ltmdp = ltmdp;
			}

			public void ApplyActionWithStackBasedAlgorithm(Action<ContinuationGraphElement> action )
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
					var cge = default(ContinuationGraphElement);
					while (!foundNextLeaf)
					{
						// select current fromCid
						var fromCid = fromDecisionStack.Peek();
						cge = Ltmdp.GetContinuationGraphElement(fromCid);

						// found new cge
						action(cge);

						if (cge.IsChoiceTypeUnsplitOrFinal)
						{
							foundNextLeaf = true;
						}
						else
						{
							fromDecisionStack.Push(cge.From);
							toDecisionStack.Push(cge.To);
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
				ContinuationGraphElement cge = Ltmdp.GetContinuationGraphElement(currentCid);
				action(cge);
				if (cge.IsChoiceTypeUnsplitOrFinal)
				{
				}
				else
				{
					for (var i = cge.From; i <= cge.To; i++)
					{
						ApplyActionWithRecursionBasedAlgorithmInnerRecursion(action, i);
					}
				}
			}

			public T ApplyFuncWithRecursionBasedAlgorithm<T>(Func<ContinuationGraphElement,IEnumerable<T>, T> innerFunc, Func<ContinuationGraphElement, T> leafFunc)
			{
				var cache = new Dictionary<long,T>();
				return ApplyFuncWithRecursionBasedAlgorithmInnerRecursion(innerFunc, leafFunc, cache, ParentContinuationId);
			}

			private T ApplyFuncWithRecursionBasedAlgorithmInnerRecursion<T>(Func<ContinuationGraphElement, IEnumerable<T>, T> innerFunc, Func<ContinuationGraphElement, T> leafFunc, Dictionary<long,T> cachedElements, long currentCid)
			{
				if (cachedElements.ContainsKey(currentCid))
					return cachedElements[currentCid];

				ContinuationGraphElement cge = Ltmdp.GetContinuationGraphElement(currentCid);
				if (cge.IsChoiceTypeUnsplitOrFinal)
				{
					var result = leafFunc(cge);
					cachedElements[currentCid] = result;
					return result;
				}
				else
				{
					var results = new T[cge.To - cge.From + 1];
					for (var i = cge.From; i <= cge.To; i++)
					{
						results[i- cge.From] = ApplyFuncWithRecursionBasedAlgorithmInnerRecursion(innerFunc, leafFunc, cachedElements, i);
					}
					var result = innerFunc(cge, results);
					cachedElements[currentCid] = result;
					return result;
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

		internal DirectChildrenEnumerator GetDirectChildrenEnumerator(long parentContinuationId)
		{
			return new DirectChildrenEnumerator(this, parentContinuationId);
		}
		
		internal class UnderlyingDigraph
		{
			public BidirectionalGraph<EdgeType> _baseGraph { get; }

			private readonly long _transitionTargetNo;

			private readonly int _stateNo;

			private readonly long _cidNo;

			private LabeledTransitionMarkovDecisionProcess _ltmdp;

			public UnderlyingDigraph(LabeledTransitionMarkovDecisionProcess ltmdp)
			{
				//Assumption "every node is reachable" is fulfilled due to the construction
				_ltmdp = ltmdp;
				_transitionTargetNo = ltmdp.TransitionTargets;
				_cidNo = ltmdp.ContinuationGraphSize;
				_stateNo = ltmdp.SourceStates.Count;
				_baseGraph = new BidirectionalGraph<EdgeType>();

				var initialCid = _ltmdp.GetRootContinuationGraphLocationOfInitialState();
				AddNodesOfContinuationId(initialCid);

				foreach (var sourceState in _ltmdp.SourceStates)
				{
					var stateRootCid = _ltmdp.GetRootContinuationGraphLocationOfState(sourceState);
					AddNodesOfContinuationId(stateRootCid);
					AddStateToRootCidTransition(sourceState, stateRootCid);
				}
				AddTransitionTargets();
			}


			private void AddNodesOfContinuationId(long continuationId)
			{
				var choice = _ltmdp.GetContinuationGraphElement(continuationId);

				var cidNode = CidToNodeIndex(continuationId);

				if (choice.IsChoiceTypeUnsplitOrFinal)
				{
					var transitionTargetIndex = choice.To;
					var transitionTargetNode = TransitionTargetToNodeIndex(transitionTargetIndex);
					_baseGraph.AddVerticesAndEdge(new Edge<EdgeType>(cidNode, transitionTargetNode, EdgeType.Deterministic));
					return;
				}
				if (choice.IsChoiceTypeForward)
				{
					// no recursive descent here
					var forwardToCid = choice.To;
					var forwardToCidNode = CidToNodeIndex(forwardToCid);
					_baseGraph.AddVerticesAndEdge(new Edge<EdgeType>(cidNode, forwardToCidNode, EdgeType.Deterministic));
					return;
				}
				
				// Assume that no child has probability of 0
				var type = choice.IsChoiceTypeProbabilitstic ? EdgeType.Probabilistic:EdgeType.NonDeterministic;

				for (var childCid = choice.From; childCid <= choice.To; childCid++)
				{
					var childCidNode = CidToNodeIndex(childCid);
					_baseGraph.AddVerticesAndEdge(new Edge<EdgeType>(cidNode, childCidNode, type));
				}
			}

			private void AddStateToRootCidTransition(int sourceState, long stateRootCid)
			{
				var sourceStateNode = StateToNodeIndex(sourceState);
				var cidNode = CidToNodeIndex(stateRootCid);
				_baseGraph.AddVerticesAndEdge(new Edge<EdgeType>(sourceStateNode, cidNode, EdgeType.Deterministic));
			}

			private void AddTransitionTargets()
			{
				for (var i = 0L; i < _transitionTargetNo; i++)
				{
					var transitionTarget = _ltmdp.GetTransitionTarget(i);
					var transitionTargetNode = TransitionTargetToNodeIndex(i);
					var transitionTargetStateNode = StateToNodeIndex(transitionTarget.TargetState);
					_baseGraph.AddVerticesAndEdge(new Edge<EdgeType>(transitionTargetNode, transitionTargetStateNode, EdgeType.Deterministic));
				}
			}

			public long? TryGetTransitionTargetIndex(long node)
			{
				Assert.That(node >= 0 && node < _transitionTargetNo + _stateNo, "Out of index");
				if (node < _transitionTargetNo)
				{
					return node;
				}
				return null;
			}

			private long TransitionTargetToNodeIndex(long transition)
			{
				Assert.That(transition >= 0 && transition < _transitionTargetNo, "Out of range");
				return transition;
			}

			private long StateToNodeIndex(int state)
			{
				Assert.That(state >= 0 && state < _stateNo, "Out of range");
				return _transitionTargetNo + state;
			}

			private long CidToNodeIndex(long cid)
			{
				Assert.That(cid >= 0 && cid<_cidNo, "Out of range");
				return _transitionTargetNo + _stateNo +cid;
			}

			internal void BackwardTraversal(IEnumerable<long> targetTransitionTargets, Action<long> actionOnResultingTransitionTarget, Func<long, bool> transitionTargetsToIgnore)
			{
				var transitionTargetNodes = targetTransitionTargets.Select(TransitionTargetToNodeIndex);

				Func<long, bool> ignoreNodeFunc =
					node =>
					{
						var transitionTarget = TryGetTransitionTargetIndex(node);
						if (transitionTarget == null)
							return false;
						return transitionTargetsToIgnore(transitionTarget.Value);
					};

				var ancestors = _baseGraph.GetAncestors(transitionTargetNodes, ignoreNodeFunc);
				foreach (var ancestor in ancestors)
				{
					var transitionTargetOfAncestor = TryGetTransitionTargetIndex(ancestor.Key);
					if (transitionTargetOfAncestor != null)
					{
						actionOnResultingTransitionTarget(transitionTargetOfAncestor.Value);
					}
				}
			}

			public enum EdgeType
			{
				Probabilistic,
				NonDeterministic,
				Deterministic
			}
		}

		internal struct DirectChildrenEnumerator
		{
			public long ParentContinuationId { get; }

			public long CurrentChildContinuationId { private set; get; }

			public ContinuationGraphElement ContinuationGraphElement { get; }

			public DirectChildrenEnumerator(LabeledTransitionMarkovDecisionProcess ltmdp, long parentContinuationId)
			{
				ContinuationGraphElement = ltmdp._continuationGraph[parentContinuationId];
				CurrentChildContinuationId = ContinuationGraphElement.From - 1;
				ParentContinuationId = parentContinuationId;
			}

			public bool MoveNext()
			{
				if (ContinuationGraphElement.IsChoiceTypeUnsplitOrFinal)
					return false;
				CurrentChildContinuationId++;
				if (CurrentChildContinuationId <= ContinuationGraphElement.To)
					return true;
				return false;
			}
		}


		internal TransitionTargetEnumerator GetTransitionTargetEnumerator()
		{
			return new TransitionTargetEnumerator(this);
		}

		internal struct TransitionTargetEnumerator
		{
			private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;

			public int CurrentIndex { get; private set; }
			
			public int CurrentTargetState => _ltmdp._transitionTarget[CurrentIndex].TargetState;

			public StateFormulaSet CurrentFormulas => _ltmdp._transitionTarget[CurrentIndex].Formulas;

			public TransitionTargetEnumerator(LabeledTransitionMarkovDecisionProcess ltmdp)
			{
				_ltmdp = ltmdp;
				CurrentIndex = -1;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			public bool MoveNext()
			{
				CurrentIndex++;
				if (CurrentIndex >= _ltmdp._transitionTargetCount)
					return false;
				return true;
			}
		}
	}
}
