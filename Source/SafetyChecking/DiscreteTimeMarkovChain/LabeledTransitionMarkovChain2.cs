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

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System.Diagnostics;
	using AnalysisModel;
	using Modeling;
	using Utilities;

	//Dictionary-based
	internal unsafe class LabeledTransitionMarkovChain2
	{
		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"
		
		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;
		
		public List<int> SourceStates { get; } = new List<int>();

		private readonly Dictionary<int,Dictionary<EnrichedTargetState, double>> _transitions;
		
		private readonly Dictionary<EnrichedTargetState, double> _initialStates;

		public LabeledTransitionMarkovChain2(int maxNumberOfStates= 1 << 21, int maxNumberOfTransitions=0)
		{
			Requires.InRange(maxNumberOfStates, nameof(maxNumberOfStates), 1024, Int32.MaxValue - 1);
			
			_transitions = new Dictionary<int, Dictionary<EnrichedTargetState, double>>();
			_initialStates = new Dictionary<EnrichedTargetState, double>();
		}

		private struct EnrichedTargetState
		{
			public readonly int TargetState;
			public readonly StateFormulaSet Formulas;
			
			public EnrichedTargetState(int targetState, StateFormulaSet formulas)
			{
				TargetState = targetState;
				Formulas = formulas;
			}

			public bool Equals(EnrichedTargetState other)
			{
				return TargetState == other.TargetState && Formulas.Equals(other.Formulas);
			}
			
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
					return false;
				return obj is EnrichedTargetState && Equals((EnrichedTargetState)obj);
			}
			
			public override int GetHashCode()
			{
				unchecked
				{
					return (TargetState * 397) ^ Formulas.GetHashCode();
				}
			}
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
			
			Dictionary<EnrichedTargetState, double> container;
			if (isInitial)
			{
				container = _initialStates;
			}
			else
			{
				if (_transitions.ContainsKey(sourceState))
				{
					container = _transitions[sourceState];
				}
				else
				{
					container = new Dictionary<EnrichedTargetState, double>();
					_transitions[sourceState] = container;
				}

			}

			// Search for place to append is linear in number of existing transitions of state linearly => O(n^2) 
			foreach (var transition in transitions)
			{
				var probTransition = (LtmcTransition*)transition;
				Assert.That(probTransition->IsValid, "Attempted to add an invalid transition.");

				var enrichedTargetState = new EnrichedTargetState(transition->TargetState, transition->Formulas);
				
				if (container.ContainsKey(enrichedTargetState))
				{
					//Case 1: Merge
					var currentProbability = container[enrichedTargetState];
					container[enrichedTargetState] = currentProbability + probTransition->Probability;
				}
				else
				{
					//Case 2: Append
					container.Add(enrichedTargetState, probTransition->Probability);
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
		
		internal LabeledTransitionEnumerator GetTransitionEnumerator(int stateStorageState)
		{
			return new LabeledTransitionEnumerator(this, stateStorageState);
		}

		internal LabeledTransitionEnumerator GetInitialDistributionEnumerator()
		{
			return new LabeledTransitionEnumerator(this);
		}

		internal struct LabeledTransitionEnumerator
		{
			private readonly LabeledTransitionMarkovChain2 _ltmc;

			private KeyValuePair<EnrichedTargetState, double> _current;

			private Dictionary<EnrichedTargetState, double>.Enumerator _enumerator;
			
			public double CurrentProbability {
				get { return _current.Value; }
			}

			public int CurrentTargetState
			{
				get { return _current.Key.TargetState; }
			}

			public StateFormulaSet CurrentFormulas
			{
				get { return _current.Key.Formulas; }
			}

			public LabeledTransitionEnumerator(LabeledTransitionMarkovChain2 ltmc, int state)
			{
				_ltmc = ltmc;
				_current = default(KeyValuePair<EnrichedTargetState, double>);
				_enumerator = ltmc._transitions[state].GetEnumerator();
			}

			public LabeledTransitionEnumerator(LabeledTransitionMarkovChain2 ltmc)
			{
				_ltmc = ltmc;
				_current = default(KeyValuePair<EnrichedTargetState, double>);
				_enumerator = ltmc._initialStates.GetEnumerator();
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			public bool MoveNext()
			{
				var hasNext = _enumerator.MoveNext();
				if (!hasNext)
					return false;
				_current = _enumerator.Current;
				return true;
			}
		}

		internal TransitionChainEnumerator GetTransitionChainEnumerator()
		{
			return new TransitionChainEnumerator(this);
		}

		internal struct TransitionChainEnumerator
		{
			private readonly LabeledTransitionMarkovChain2 _ltmc;

			private IEnumerator<KeyValuePair<EnrichedTargetState, double>> _enumerator;

			public double CurrentProbability
			{
				get { return _enumerator.Current.Value; }
			}

			public int CurrentTargetState
			{
				get { return _enumerator.Current.Key.TargetState; }
			}

			public StateFormulaSet CurrentFormulas
			{
				get { return _enumerator.Current.Key.Formulas; }
			}

			public TransitionChainEnumerator(LabeledTransitionMarkovChain2 ltmc)
			{
				_ltmc = ltmc;
				_enumerator=CreateEnumerator(ltmc);
			}

			private static IEnumerator<KeyValuePair<EnrichedTargetState, double>> CreateEnumerator(LabeledTransitionMarkovChain2 ltmc)
			{
				foreach (var initialState in ltmc._initialStates)
				{
					yield return initialState;
				}
				foreach (var transition in ltmc._transitions)
				{
					foreach (var d in transition.Value)
					{
						yield return d;
					}
				}
			}

			public bool MoveNext()
			{
				return _enumerator.MoveNext();
			}
		}
	}
}
