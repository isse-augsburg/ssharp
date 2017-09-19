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
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using Modeling;
	using Utilities;
	using AnalysisModel;
	using Formula;
	using GenericDataStructures;

	public unsafe partial class LabeledTransitionMarkovChain : DisposableObject
	{
		public static readonly int TransitionSize = sizeof(TransitionChainElement);

		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"
		

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;
		
		private int _indexOfFirstInitialTransition = -1;

		public ConcurrentBag<int> SourceStates { get; } = new ConcurrentBag<int>();

		private readonly MemoryBuffer _stateStorageStateToFirstTransitionChainElementBuffer = new MemoryBuffer();
		private readonly int* _stateStorageStateToFirstTransitionChainElementMemory;
		
		private readonly MemoryBuffer _transitionChainElementsBuffer = new MemoryBuffer();
		private readonly TransitionChainElement* _transitionChainElementsMemory;
		private int _transitionChainElementCount = 0;

		public int Transitions => _transitionChainElementCount;

		private readonly long _maxNumberOfTransitions;
		
		public LabeledTransitionMarkovChain(long maxNumberOfStates, long maxNumberOfTransitions)
		{
			Requires.InRange(maxNumberOfStates, nameof(maxNumberOfStates), 1024, Int32.MaxValue - 1);

			_maxNumberOfTransitions = maxNumberOfTransitions;

			_stateStorageStateToFirstTransitionChainElementBuffer.Resize((long)maxNumberOfStates * sizeof(int), zeroMemory: false);
			_stateStorageStateToFirstTransitionChainElementMemory = (int*)_stateStorageStateToFirstTransitionChainElementBuffer.Pointer;

			_transitionChainElementsBuffer.Resize((long)maxNumberOfTransitions * sizeof(TransitionChainElement), zeroMemory: false);
			_transitionChainElementsMemory = (TransitionChainElement*)_transitionChainElementsBuffer.Pointer;

			MemoryBuffer.SetAllBitsMemoryWithInitblk.ClearWithMinus1(_stateStorageStateToFirstTransitionChainElementMemory,maxNumberOfStates);
		}
		
		private struct TransitionChainElement
		{
			public int NextElementIndex;
			public int TargetState;
			public StateFormulaSet Formulas;
			public double Probability;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int GetPlaceForNewTransitionChainElement()
		{
			var locationOfNewEntry= InterlockedExtensions.IncrementReturnOld(ref _transitionChainElementCount);
			if (locationOfNewEntry >= _maxNumberOfTransitions)
				throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			return locationOfNewEntry;
		}

		public void CreateStutteringState(int stutteringStateIndex)
		{
			// The stuttering state might not be reached at all.
			// Make sure, that all used algorithms to not require a connected state graph.
			var currentElementIndex = _stateStorageStateToFirstTransitionChainElementMemory[stutteringStateIndex];
			Assert.That(currentElementIndex == -1, "Stuttering state has already been created");
			var locationOfNewEntry = GetPlaceForNewTransitionChainElement();
			_transitionChainElementsMemory[locationOfNewEntry] =
					new TransitionChainElement
					{
						Formulas = new StateFormulaSet(),
						NextElementIndex = -1,
						Probability = 1.0,
						TargetState = stutteringStateIndex
					};

			SourceStates.Add(stutteringStateIndex);
			_stateStorageStateToFirstTransitionChainElementMemory[stutteringStateIndex] = locationOfNewEntry;
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
			Func<LabeledTransitionEnumerator, int> selectEntryWithHighestProbability =
				enumerator =>
				{
					var candidate = -1;
					var probabilityOfCandidate = -1.0;
					while (enumerator.MoveNext())
						if (probabilityOfCandidate < enumerator.CurrentProbability)
							candidate = enumerator.CurrentIndex;
					return candidate;
				};
			Action<int> printTransition =
				index =>
				{
					var transition = _transitionChainElementsMemory[index];
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

			var lastState = _transitionChainElementsMemory[initialTransitionWithHighestProbability].TargetState;
			for (var i = 0; i < steps; i++)
			{
				var currentTransition = selectEntryWithHighestProbability(GetTransitionEnumerator(lastState));
				printTransition(currentTransition);
				lastState = _transitionChainElementsMemory[currentTransition].TargetState;
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

			_stateStorageStateToFirstTransitionChainElementBuffer.SafeDispose();
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
			var firstElement = _stateStorageStateToFirstTransitionChainElementMemory[stateStorageState];
			return new LabeledTransitionEnumerator(this, firstElement);
		}

		internal LabeledTransitionEnumerator GetInitialDistributionEnumerator()
		{
			return new LabeledTransitionEnumerator(this, _indexOfFirstInitialTransition);
		}

		internal struct LabeledTransitionEnumerator
		{
			private readonly LabeledTransitionMarkovChain _ltmc;

			public int CurrentIndex { get; private set; }

			private int _nextElementIndex;

			public double CurrentProbability => _ltmc._transitionChainElementsMemory[CurrentIndex].Probability;

			public int CurrentTargetState => _ltmc._transitionChainElementsMemory[CurrentIndex].TargetState;

			public StateFormulaSet CurrentFormulas => _ltmc._transitionChainElementsMemory[CurrentIndex].Formulas;

			public LabeledTransitionEnumerator(LabeledTransitionMarkovChain ltmc, int firstElement)
			{
				_ltmc = ltmc;
				CurrentIndex = -1;
				_nextElementIndex = firstElement;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			public bool MoveNext()
			{
				if (_nextElementIndex == -1)
					return false;
				CurrentIndex = _nextElementIndex;
				_nextElementIndex = _ltmc._transitionChainElementsMemory[CurrentIndex].NextElementIndex;
				return true;
			}
		}

		internal TransitionChainEnumerator GetTransitionChainEnumerator()
		{
			return new TransitionChainEnumerator(this);
		}
		
		public Func<int, bool> CreateFormulaEvaluator(Formula formula)
		{
			var stateFormulaEvaluator = StateFormulaSetEvaluatorCompilationVisitor.Compile(StateFormulaLabels, formula);
			Func<int, bool> evaluator = transitionTarget =>
			{
				var stateFormulaSet = _transitionChainElementsMemory[transitionTarget].Formulas;
				return stateFormulaEvaluator(stateFormulaSet);
			};
			return evaluator;
		}

		internal struct TransitionChainEnumerator
		{
			private readonly LabeledTransitionMarkovChain _ltmc;

			public int CurrentIndex { get; private set; }
			
			public double CurrentProbability => _ltmc._transitionChainElementsMemory[CurrentIndex].Probability;

			public int CurrentTargetState
			{
				get { return _ltmc._transitionChainElementsMemory[CurrentIndex].TargetState; }
			}

			public StateFormulaSet CurrentFormulas
			{
				get { return _ltmc._transitionChainElementsMemory[CurrentIndex].Formulas; }
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
			private readonly BidirectionalGraph _baseGraph;

			public BidirectionalGraphDirectNodeAccess BaseGraph => _baseGraph;

			public void AddStatesFromEnumerator(int sourceState, LabeledTransitionEnumerator enumerator)
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.CurrentProbability > 0.0)
						_baseGraph.AddVerticesAndEdge(new Edge(sourceState, enumerator.CurrentTargetState));
				}
			}

			public UnderlyingDigraph(LabeledTransitionMarkovChain markovChain)
			{
				// Assumption "every node is reachable" is fulfilled due to the construction
				// Except maybe the stuttering state

				_baseGraph = new BidirectionalGraph();

				// transitions from initial state get artificial source state with index -1

				var enumerator = markovChain.GetInitialDistributionEnumerator();
				AddStatesFromEnumerator(-1, enumerator);
				foreach (var sourceState in markovChain.SourceStates)
				{
					markovChain.GetTransitionEnumerator(sourceState);
				}
			}
		}
	}
}
