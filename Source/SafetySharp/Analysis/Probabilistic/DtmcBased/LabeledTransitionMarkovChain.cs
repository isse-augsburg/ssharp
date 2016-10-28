// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.Runtime
{
	using System.Collections;
	using System.Collections.Concurrent;
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using Analysis;
	using Analysis.ModelChecking.Transitions;
	using Modeling;
	using Serialization;
	using Utilities;
	

	internal unsafe class LabeledTransitionMarkovChain
	{
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


		public LabeledTransitionMarkovChain(long maxNumberOfStates= 1 << 21, long maxNumberOfTransitions=0)
		{
			Requires.InRange(maxNumberOfStates, nameof(maxNumberOfStates), 1024, Int32.MaxValue - 1);
			if (maxNumberOfTransitions <= 0)
			{
				maxNumberOfTransitions = maxNumberOfStates << 10;
				var limit = 8 * 1024 / 32 * 1024 * 1024; // 6 gb / 32 bytes (for entries)

				if (maxNumberOfTransitions < maxNumberOfStates || maxNumberOfTransitions > limit)
					maxNumberOfTransitions = limit;

			}
			_maxNumberOfTransitions = maxNumberOfTransitions;

			_stateStorageStateToFirstTransitionChainElementBuffer.Resize((long)maxNumberOfStates * sizeof(int), zeroMemory: false);
			_stateStorageStateToFirstTransitionChainElementMemory = (int*)_stateStorageStateToFirstTransitionChainElementBuffer.Pointer;

			_transitionChainElementsBuffer.Resize((long)maxNumberOfTransitions * sizeof(TransitionChainElement), zeroMemory: false);
			_transitionChainElementsMemory = (TransitionChainElement*)_transitionChainElementsBuffer.Pointer;


			for (var i = 0; i < maxNumberOfStates; i++)
			{
				_stateStorageStateToFirstTransitionChainElementMemory[i] = -1;
			}
		}

		private struct TransitionChainElement
		{
			public int NextElementIndex;
			public int TargetState;
			public StateFormulaSet Formulas;
			public double Probability;
		}
		
		/// <summary>
		///   Adds the <paramref name="sourceState" /> and all of its <see cref="transitions" /> to the state graph.
		/// </summary>
		/// <param name="sourceState">The state that should be added.</param>
		/// <param name="isInitial">Indicates whether the state is an initial state.</param>
		/// <param name="transitions">The transitions leaving the state.</param>
		/// <param name="transitionCount">The number of valid transitions leaving the state.</param>
		internal void AddStateInfo(int sourceState, bool isInitial, TransitionCollection transitions, int transitionCount)
		{
			Assert.That(transitionCount > 0, "Cannot add deadlock state.");

			var upperBoundaryForTransitions = _transitionChainElementCount + transitionCount;
			if (upperBoundaryForTransitions > _maxNumberOfTransitions || upperBoundaryForTransitions<0)
				throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");


			// Search for place to append is linear in number of existing transitions of state linearly => O(n^2) 
			foreach (var transition in transitions)
			{
				var probTransition = (LtmcTransition*)transition;
				Assert.That(probTransition->IsValid, "Attempted to add an invalid transition.");

				int currentElementIndex;
				if (isInitial)
					currentElementIndex = _indexOfFirstInitialTransition;
				else
					currentElementIndex = _stateStorageStateToFirstTransitionChainElementMemory[sourceState];

				if (currentElementIndex == -1)
				{
					// Add new chain start
					var locationOfNewEntry = InterlockedExtensions.IncrementReturnOld(ref _transitionChainElementCount);
					_transitionChainElementsMemory[locationOfNewEntry] =
						new TransitionChainElement
						{
							Formulas = transition->Formulas,
							NextElementIndex = -1,
							Probability = probTransition->Probability,
							TargetState = transition->TargetState
						};
					if (isInitial)
					{
						_indexOfFirstInitialTransition = locationOfNewEntry;
					}
					else
					{
						SourceStates.Add(sourceState);
						_stateStorageStateToFirstTransitionChainElementMemory[sourceState] = locationOfNewEntry;
					}
				}
				else
				{
					// merge or append
					bool mergedOrAppended = false;
					while (!mergedOrAppended)
					{
						var currentElement = _transitionChainElementsMemory[currentElementIndex];
						if (currentElement.TargetState == transition->TargetState && currentElement.Formulas == transition->Formulas)
						{
							//Case 1: Merge
							_transitionChainElementsMemory[currentElementIndex] =
								new TransitionChainElement
								{
									Formulas = currentElement.Formulas,
									NextElementIndex = currentElement.NextElementIndex,
									Probability = probTransition->Probability+ currentElement.Probability,
									TargetState = currentElement.TargetState
								};
							mergedOrAppended = true;
						}
						else if (currentElement.NextElementIndex == -1)
						{
							//Case 2: Append
							var locationOfNewEntry = InterlockedExtensions.IncrementReturnOld(ref _transitionChainElementCount);
							mergedOrAppended = true;
							_transitionChainElementsMemory[currentElementIndex] =
								new TransitionChainElement
								{
									Formulas = currentElement.Formulas,
									NextElementIndex = locationOfNewEntry,
									Probability = currentElement.Probability,
									TargetState = currentElement.TargetState
								};
							_transitionChainElementsMemory[locationOfNewEntry] =
								new TransitionChainElement
								{
									Formulas = transition->Formulas,
									NextElementIndex = -1,
									Probability = probTransition->Probability,
									TargetState = transition->TargetState
								};
						}
						else
						{
							//else continue iteration
							currentElementIndex = currentElement.NextElementIndex;
						}
					}
				}
			}
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

			public double CurrentProbability {
				get { return _ltmc._transitionChainElementsMemory[CurrentIndex].Probability; }
				//set { _ltmc._transitionChainElementsMemory[CurrentIndex].Probability = value; }
			}

			public int CurrentTargetState
			{
				get { return _ltmc._transitionChainElementsMemory[CurrentIndex].TargetState; }
				//set { _ltmc._transitionChainElementsMemory[CurrentIndex].TargetState = value; }
			}

			public StateFormulaSet CurrentFormulas
			{
				get { return _ltmc._transitionChainElementsMemory[CurrentIndex].Formulas; }
				//set { _ltmc._transitionChainElementsMemory[CurrentIndex].Formulas = value; }
			}

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
	}
}
