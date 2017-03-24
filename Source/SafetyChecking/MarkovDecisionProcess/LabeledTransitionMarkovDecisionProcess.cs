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

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System.Collections.Concurrent;
	using System.Diagnostics;
	using System.Globalization;
	using Modeling;
	using Utilities;
	using AnalysisModel;
	using ExecutableModel;


	internal unsafe partial class LabeledTransitionMarkovDecisionProcess
	{
		public static readonly int TransitionSize = sizeof(TransitionChainElement);

		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;
		
		private int _indexOfFirstInitialDistribution = -1;

		public ConcurrentBag<int> SourceStates { get; } = new ConcurrentBag<int>();

		private readonly MemoryBuffer _stateStorageStateToFirstDistributionChainElementBuffer = new MemoryBuffer();
		private readonly int* _stateStorageStateToFirstDistributionChainElementMemory;
		
		private readonly MemoryBuffer _distributionChainElementsBuffer = new MemoryBuffer();
		private readonly DistributionChainElement* _distributionChainElementsMemory;
		private int _distributionChainElementCount = 0;

		private readonly MemoryBuffer _transitionChainElementsBuffer = new MemoryBuffer();
		private readonly TransitionChainElement* _transitionChainElementsMemory;
		private int _transitionChainElementCount = 0;

		public int Distributions => _distributionChainElementCount;
		public int Transitions => _transitionChainElementCount;

		private readonly long _maxNumberOfTransitions;


		public LabeledTransitionMarkovDecisionProcess(long maxNumberOfStates, long maxNumberOfTransitions)
		{
			Requires.InRange(maxNumberOfStates, nameof(maxNumberOfStates), 1024, Int32.MaxValue - 1);

			_maxNumberOfTransitions = maxNumberOfTransitions;

			_stateStorageStateToFirstDistributionChainElementBuffer.Resize((long)maxNumberOfStates * sizeof(int), zeroMemory: false);
			_stateStorageStateToFirstDistributionChainElementMemory = (int*)_stateStorageStateToFirstDistributionChainElementBuffer.Pointer;

			_distributionChainElementsBuffer.Resize((long)maxNumberOfStates * sizeof(DistributionChainElement), zeroMemory: false);
			_distributionChainElementsMemory = (DistributionChainElement*)_distributionChainElementsBuffer.Pointer;

			_transitionChainElementsBuffer.Resize((long)maxNumberOfTransitions * sizeof(TransitionChainElement), zeroMemory: false);
			_transitionChainElementsMemory = (TransitionChainElement*)_transitionChainElementsBuffer.Pointer;


			for (var i = 0; i < maxNumberOfStates; i++)
			{
				_stateStorageStateToFirstDistributionChainElementMemory[i] = -1;
			}
		}


		private struct DistributionChainElement
		{
			public int NextElementIndex;
			public int Distribution;
			public int FirstTransitionIndex;
		}

		private struct TransitionChainElement
		{
			public int NextElementIndex;
			public int TargetState;
			public StateFormulaSet Formulas;
			public double Probability;
		}

		private int GetPlaceForNewTransitionChainElement()
		{
			return InterlockedExtensions.IncrementReturnOld(ref _transitionChainElementCount);
		}


		private int GetPlaceForNewDistributionChainElement()
		{
			return InterlockedExtensions.IncrementReturnOld(ref _distributionChainElementCount);
		}

		public void CreateStutteringState(int stutteringStateIndex)
		{
			// The stuttering state might not be reached at all.
			// Make sure, that all used algorithms to not require a connected state graph.
			var currentElementIndex = _stateStorageStateToFirstDistributionChainElementMemory[stutteringStateIndex];
			Assert.That(currentElementIndex == -1, "Stuttering state has already been created");

			var locationOfNewDistributionEntry = GetPlaceForNewDistributionChainElement();
			var locationOfNewTransitionEntry = GetPlaceForNewTransitionChainElement();

			_distributionChainElementsMemory[locationOfNewDistributionEntry] =
					new DistributionChainElement
					{
						NextElementIndex = -1,
						FirstTransitionIndex = locationOfNewTransitionEntry
					};

			_transitionChainElementsMemory[locationOfNewTransitionEntry] =
					new TransitionChainElement
					{
						Formulas = new StateFormulaSet(),
						NextElementIndex = -1,
						Probability = 1.0,
						TargetState = stutteringStateIndex
					};

			SourceStates.Add(stutteringStateIndex);
			_stateStorageStateToFirstDistributionChainElementMemory[stutteringStateIndex] = locationOfNewDistributionEntry;
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
					enumerator.MoveNext(); //at least one element must exist
					var candidate = enumerator.CurrentIndex;
					var probabilityOfCandidate = enumerator.CurrentProbability;
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

		internal LabeledTransitionEnumerator GetTransitionEnumerator(int stateStorageState)
		{
			var firstElement = _stateStorageStateToFirstDistributionChainElementMemory[stateStorageState];
			return new LabeledTransitionEnumerator(this, firstElement);
		}

		internal LabeledTransitionEnumerator GetInitialDistributionEnumerator()
		{
			return new LabeledTransitionEnumerator(this, _indexOfFirstInitialDistribution);
		}

		internal struct LabeledTransitionEnumerator
		{
			private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;

			public int CurrentIndex { get; private set; }

			private int _nextElementIndex;

			public double CurrentProbability {
				get { return _ltmdp._transitionChainElementsMemory[CurrentIndex].Probability; }
				//set { _ltmdp._transitionChainElementsMemory[CurrentIndex].Probability = value; }
			}

			public int CurrentTargetState
			{
				get { return _ltmdp._transitionChainElementsMemory[CurrentIndex].TargetState; }
				//set { _ltmdp._transitionChainElementsMemory[CurrentIndex].TargetState = value; }
			}

			public StateFormulaSet CurrentFormulas
			{
				get { return _ltmdp._transitionChainElementsMemory[CurrentIndex].Formulas; }
				//set { _ltmdp._transitionChainElementsMemory[CurrentIndex].Formulas = value; }
			}

			public LabeledTransitionEnumerator(LabeledTransitionMarkovDecisionProcess ltmdp, int firstElement)
			{
				_ltmdp = ltmdp;
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
				_nextElementIndex = _ltmdp._transitionChainElementsMemory[CurrentIndex].NextElementIndex;
				return true;
			}
		}

		internal TransitionChainEnumerator GetTransitionChainEnumerator()
		{
			return new TransitionChainEnumerator(this);
		}

		internal struct TransitionChainEnumerator
		{
			private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;

			public int CurrentIndex { get; private set; }
			
			public double CurrentProbability => _ltmdp._transitionChainElementsMemory[CurrentIndex].Probability;

			public int CurrentTargetState
			{
				get { return _ltmdp._transitionChainElementsMemory[CurrentIndex].TargetState; }
			}

			public StateFormulaSet CurrentFormulas
			{
				get { return _ltmdp._transitionChainElementsMemory[CurrentIndex].Formulas; }
			}

			public TransitionChainEnumerator(LabeledTransitionMarkovDecisionProcess ltmdp)
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
				if (CurrentIndex >= _ltmdp._transitionChainElementCount)
					return false;
				return true;
			}
		}
	}
}
