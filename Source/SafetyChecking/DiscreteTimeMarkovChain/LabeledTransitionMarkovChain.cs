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

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Modeling;
	using Utilities;
	using AnalysisModel;
	using Formula;
	using GenericDataStructures;

	public unsafe partial class LabeledTransitionMarkovChain : DisposableObject
	{
		public static readonly int TransitionSize = sizeof(TransitionElement);

		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"
		

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;

		private long _indexOfFirstInitialTransition = -1;
		private long _numberOfInitialTransitions = 0;

		public ConcurrentBag<int> SourceStates { get; } = new ConcurrentBag<int>();

		private readonly MemoryBuffer _stateStorageStateToFirstTransitionElementBuffer = new MemoryBuffer();
		private readonly long* _stateStorageStateToFirstTransitionElementMemory;
		
		private readonly MemoryBuffer _stateStorageStateTransitionNumberElementBuffer = new MemoryBuffer();
		private readonly long* _stateStorageStateTransitionNumberElementMemory;

		private readonly MemoryBuffer _transitionChainElementsBuffer = new MemoryBuffer();
		private readonly TransitionElement* _transitionMemory;
		private long _transitionChainElementCount = 0;

		public long Transitions => _transitionChainElementCount;

		private readonly long _maxNumberOfTransitions;
		
		public LabeledTransitionMarkovChain(long maxNumberOfStates, long maxNumberOfTransitions)
		{
			Requires.InRange(maxNumberOfStates, nameof(maxNumberOfStates), 1024, Int32.MaxValue - 1);

			_maxNumberOfTransitions = maxNumberOfTransitions;

			_stateStorageStateToFirstTransitionElementBuffer.Resize(maxNumberOfStates * sizeof(long), zeroMemory: false);
			_stateStorageStateToFirstTransitionElementMemory = (long*)_stateStorageStateToFirstTransitionElementBuffer.Pointer;

			_stateStorageStateTransitionNumberElementBuffer.Resize(maxNumberOfStates * sizeof(long), zeroMemory: true);
			_stateStorageStateTransitionNumberElementMemory = (long*)_stateStorageStateTransitionNumberElementBuffer.Pointer;

			_transitionChainElementsBuffer.Resize(maxNumberOfTransitions * sizeof(TransitionElement), zeroMemory: false);
			_transitionMemory = (TransitionElement*)_transitionChainElementsBuffer.Pointer;

			MemoryBuffer.SetAllBitsMemoryWithInitblk.ClearWithMinus1(_stateStorageStateToFirstTransitionElementMemory,maxNumberOfStates);
		}
		
		private struct TransitionElement
		{
			public int TargetState;
			public StateFormulaSet Formulas;
			public double Probability;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private long GetPlaceForNewTransitionChainElements(long number)
		{
			var locationOfFirstNewEntry = InterlockedExtensions.AddFetch(ref _transitionChainElementCount, number);
			if (locationOfFirstNewEntry + number >= _maxNumberOfTransitions)
				throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			return locationOfFirstNewEntry ;
		}

		public void CreateStutteringState(int stutteringStateIndex)
		{
			// The stuttering state might not be reached at all.
			// Make sure, that all used algorithms to not require a connected state graph.
			var currentElementIndex = _stateStorageStateToFirstTransitionElementMemory[stutteringStateIndex];
			var currentElementNumber = _stateStorageStateTransitionNumberElementMemory[stutteringStateIndex];
			Assert.That(currentElementIndex == -1 && currentElementNumber == 0, "Stuttering state has already been created");

			var locationOfNewEntry = GetPlaceForNewTransitionChainElements(1);
			_transitionMemory[locationOfNewEntry] =
					new TransitionElement
					{
						Formulas = new StateFormulaSet(),
						Probability = 1.0,
						TargetState = stutteringStateIndex
					};

			SourceStates.Add(stutteringStateIndex);
			_stateStorageStateToFirstTransitionElementMemory[stutteringStateIndex] = locationOfNewEntry;
			_stateStorageStateTransitionNumberElementMemory[stutteringStateIndex] = 1;
		}
		
		// Validation

		[Conditional("DEBUG")]
		public void ValidateStates()
		{
			foreach (var sourceState in SourceStates)
			{
				var enumerator = GetTransitionEnumerator(sourceState);
				var probability = 0.0;
				while (enumerator.MoveNext())
				{
					probability += enumerator.CurrentProbability;
				}
				if (!Probability.IsOne(probability, 0.000000001))
					throw new Exception("Probabilities should sum up to 1");
			}
		}

		[Conditional("DEBUG")]
		public void ValidateInitialDistribution()
		{
			var enumerator = GetInitialDistributionEnumerator();
			var probability = 0.0;
			while (enumerator.MoveNext())
			{
				probability += enumerator.CurrentProbability;
			}
			if (!Probability.IsOne(probability, 0.000000001))
				throw new Exception("Probabilities should sum up to 1");
		}


		[Conditional("DEBUG")]
		internal void PrintPathWithStepwiseHighestProbability(int steps)
		{
			Func<LabeledTransitionEnumerator, long> selectEntryWithHighestProbability =
				enumerator =>
				{
					var candidate = -1L;
					var probabilityOfCandidate = -1.0;
					while (enumerator.MoveNext())
						if (probabilityOfCandidate < enumerator.CurrentProbability)
							candidate = enumerator.CurrentIndex;
					return candidate;
				};
			Action<long> printTransition =
				index =>
				{
					var transition = _transitionMemory[index];
					var stateLabeling = "";
					for (var i = 0; i < StateFormulaLabels.Length; i++)
					{
						var label = StateFormulaLabels[i];
						//Console.Write(" " + label + "=");
						if (transition.Formulas[i])
							Console.Write("true");
						else
							Console.Write("false");
					}
					Console.Write($"--- {transition.Probability.ToString(CultureInfo.InvariantCulture)} {stateLabeling}--> {transition.TargetState}");
					
					Console.WriteLine();
				};

			foreach (var label in StateFormulaLabels)
			{
				Console.Write(" " + label );
			}

			var initialTransitionWithHighestProbability = selectEntryWithHighestProbability(GetInitialDistributionEnumerator());
			printTransition(initialTransitionWithHighestProbability);

			var lastState = _transitionMemory[initialTransitionWithHighestProbability].TargetState;
			for (var i = 0; i < steps; i++)
			{
				var currentTransition = selectEntryWithHighestProbability(GetTransitionEnumerator(lastState));
				printTransition(currentTransition);
				lastState = _transitionMemory[currentTransition].TargetState;
			}
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_stateStorageStateToFirstTransitionElementBuffer.SafeDispose();
			_transitionChainElementsBuffer.SafeDispose();
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

		internal LabeledTransitionEnumerator GetTransitionEnumerator(int stateStorageState)
		{
			var firstElement = _stateStorageStateToFirstTransitionElementMemory[stateStorageState];
			var elements = _stateStorageStateTransitionNumberElementMemory[stateStorageState];
			return new LabeledTransitionEnumerator(this, firstElement, elements);
		}

		internal LabeledTransitionEnumerator GetInitialDistributionEnumerator()
		{
			var firstElement = _indexOfFirstInitialTransition;
			var elements = _numberOfInitialTransitions;
			return new LabeledTransitionEnumerator(this, firstElement, elements);
		}

		internal struct LabeledTransitionEnumerator
		{
			private readonly LabeledTransitionMarkovChain _ltmc;

			public long CurrentIndex { get; private set; }

			private readonly long _lastElementIndex;

			public double CurrentProbability => _ltmc._transitionMemory[CurrentIndex].Probability;

			public int CurrentTargetState => _ltmc._transitionMemory[CurrentIndex].TargetState;

			public StateFormulaSet CurrentFormulas => _ltmc._transitionMemory[CurrentIndex].Formulas;

			public LabeledTransitionEnumerator(LabeledTransitionMarkovChain ltmc, long firstElement, long elements)
			{
				_ltmc = ltmc;
				CurrentIndex = firstElement - 1;
				_lastElementIndex = firstElement + elements -1;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			public bool MoveNext()
			{
				if (CurrentIndex >= _lastElementIndex)
					return false;
				CurrentIndex ++;
				return true;
			}
		}

		internal TransitionChainEnumerator GetTransitionChainEnumerator()
		{
			return new TransitionChainEnumerator(this);
		}
		
		public Func<long, bool> CreateFormulaEvaluator(Formula formula)
		{
			var stateFormulaEvaluator = StateFormulaSetEvaluatorCompilationVisitor.Compile(StateFormulaLabels, formula);
			Func<long, bool> evaluator = transitionTarget =>
			{
				var stateFormulaSet = _transitionMemory[transitionTarget].Formulas;
				return stateFormulaEvaluator(stateFormulaSet);
			};
			return evaluator;
		}

		internal struct TransitionChainEnumerator
		{
			private readonly LabeledTransitionMarkovChain _ltmc;

			public long CurrentIndex { get; private set; }
			
			public double CurrentProbability => _ltmc._transitionMemory[CurrentIndex].Probability;

			public int CurrentTargetState
			{
				get { return _ltmc._transitionMemory[CurrentIndex].TargetState; }
			}

			public StateFormulaSet CurrentFormulas
			{
				get { return _ltmc._transitionMemory[CurrentIndex].Formulas; }
			}

			public TransitionChainEnumerator(LabeledTransitionMarkovChain ltmc)
			{
				_ltmc = ltmc;
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
				if (CurrentIndex >= _ltmc._transitionChainElementCount)
					return false;
				return true;
			}
		}

		internal UnderlyingDigraph CreateUnderlyingDigraph()
		{
			return new UnderlyingDigraph(this);
		}

		internal class UnderlyingDigraph
		{
			// Idea: Every transitionTarget and every state gets a node. From [0,markovChain.Transitions] are
			// transitionTargets and from [markovChain.Transitions+1,markovChain.Transitions+markovChain.SourceStates.Count]
			// transitions.

			private readonly BidirectionalGraph _baseGraph;

			private readonly long _transitionTargetNo;
			private readonly int _stateNo;

			public BidirectionalGraphDirectNodeAccess BaseGraph => _baseGraph;

			public UnderlyingDigraph(LabeledTransitionMarkovChain markovChain)
			{
				// Assumption "every node is reachable" is fulfilled due to the construction
				// Except maybe the stuttering state
				_transitionTargetNo = markovChain.Transitions;
				_stateNo = markovChain.SourceStates.Count;

				_baseGraph = new BidirectionalGraph();

				var enumerator = markovChain.GetInitialDistributionEnumerator();
				AddStatesFromEnumerator(null, enumerator);
				foreach (var sourceState in markovChain.SourceStates)
				{
					enumerator = markovChain.GetTransitionEnumerator(sourceState);
					markovChain.GetTransitionEnumerator(sourceState);
					AddStatesFromEnumerator(sourceState, enumerator);
				}
			}

			public long? TryGetTransitionTargetIndex(long node)
			{
				Assert.That(node>=0 && node < _transitionTargetNo + _stateNo,"Out of index");
				if (node < _transitionTargetNo)
				{
					return node;
				}
				return null;
			}

			private long StateToNodeIndex(int state)
			{
				Assert.That(state >= 0 && state < _stateNo, "Out of range");
				return _transitionTargetNo + state;
			}

			private long TransitionTargetToNodeIndex(long transition)
			{
				Assert.That(transition >= 0 && transition < _transitionTargetNo, "Out of range");
				return transition;
			}

			public void AddStatesFromEnumerator(int? sourceState, LabeledTransitionEnumerator enumerator)
			{
				while (enumerator.MoveNext())
				{
					// Cannot make the next validation check because such transitions might exist when many small probabilities are
					// multiplied because of imprecise doubles arithmetic
					//if (!(enumerator.CurrentProbability > 0.0))
					//	continue;
					
					var transitionTargetNodeIndex = TransitionTargetToNodeIndex(enumerator.CurrentIndex);
					var targetStateNodeIndex = StateToNodeIndex(enumerator.CurrentTargetState);
					_baseGraph.AddVerticesAndEdge(new Edge(transitionTargetNodeIndex, targetStateNodeIndex));

					if (sourceState == null)
						continue;
					var sourceStateNodeIndex = StateToNodeIndex(sourceState.Value);
					_baseGraph.AddVerticesAndEdge(new Edge(sourceStateNodeIndex, transitionTargetNodeIndex));
				}
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

				var ancestors=_baseGraph.GetAncestors(transitionTargetNodes, ignoreNodeFunc);
				foreach (var ancestor in ancestors)
				{
					var transitionTargetOfAncestor = TryGetTransitionTargetIndex(ancestor.Key);
					if (transitionTargetOfAncestor != null)
					{
						actionOnResultingTransitionTarget(transitionTargetOfAncestor.Value);
					}
				}
			}
		}
	}
}
