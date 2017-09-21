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

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using ExecutableModel;
	using Utilities;



	//The LtmcBuilder is tightly coupled to LabeledTransitionMarkovChain, so we make it a nested class
	public unsafe partial class LabeledTransitionMarkovChain
	{
		/// <summary>
		///   Builds up a <see cref="LabeledTransitionMarkovChain" /> instance during model traversal.
		///   Note: This only works single threaded
		/// </summary>
		internal class LtmcBuilder : IBatchedTransitionAction
		{
			private readonly LabeledTransitionMarkovChain _markovChain;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="markovChain">The Markov chain that should be built up.</param>
			public LtmcBuilder(LabeledTransitionMarkovChain markovChain)
			{
				Requires.NotNull(markovChain, nameof(markovChain));
				_markovChain = markovChain;
			}

			[Conditional("DEBUG")]
			private void CheckIfTransitionsCanBeProcessed(int transitionCount)
			{
				Assert.That(transitionCount > 0, "Cannot add deadlock state.");

				// Need to reserve the memory for the transitions
				var upperBoundaryForTransitions = _markovChain._transitionChainElementCount + transitionCount;
				if (upperBoundaryForTransitions < 0)
					throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			}

			[Conditional("DEBUG")]
			private void CheckIfTransitionIsValid(LtmcTransition* transition)
			{
				Assert.That(TransitionFlags.IsValid(transition->Flags), "Attempted to add an invalid transition.");
			}
			
			private void AddTransitions(int sourceState, bool areInitialTransitions, TransitionCollection transitions)
			{
				var transitionNo = transitions.Count;

				var placeOfTransition = _markovChain.GetPlaceForNewTransitionChainElements(transitionNo);

				var index = 0L;
				foreach (var transition in transitions)
				{
					Assert.That(index<transitionNo,"Bug");

					var probTransition = (LtmcTransition*)transition;

					CheckIfTransitionIsValid(probTransition);

					_markovChain._transitionMemory[placeOfTransition+index] =
						new TransitionElement
						{
							TargetState = transition->TargetStateIndex,
							Formulas = probTransition->Formulas,
							Probability = probTransition->Probability
						};
					index++;
				}

				if (areInitialTransitions)
				{
					_markovChain._indexOfFirstInitialTransition = placeOfTransition;
					_markovChain._numberOfInitialTransitions = transitionNo;
				}
				else
				{
					_markovChain.SourceStates.Add(sourceState);
					_markovChain._stateStorageStateToFirstTransitionElementMemory[sourceState] = placeOfTransition;
					_markovChain._stateStorageStateTransitionNumberElementMemory[sourceState] = transitionNo;
				}

			}

			/// <summary>
			///   Processes the new <paramref name="transitions" /> discovered by the <paramref name="worker " /> within the traversal
			///   <paramref name="context" />. Only transitions with <see cref="CandidateTransition.IsValid" /> set to <c>true</c> are
			///   actually new.
			/// </summary>
			/// <param name="context">The context of the model traversal.</param>
			/// <param name="worker">The worker that found the transition.</param>
			/// <param name="sourceState">The index of the transition's source state.</param>
			/// <param name="transitions">The new transitions that should be processed.</param>
			/// <param name="transitionCount">The actual number of valid transitions.</param>
			/// <param name="areInitialTransitions">
			///   Indicates whether the transitions are an initial transitions not starting in any valid source state.
			/// </param>
			public void ProcessTransitions(TraversalContext context, Worker worker, int sourceState,
										   TransitionCollection transitions, int transitionCount, bool areInitialTransitions)
			{
				// Note, other threads might access _markovChain at the same time
				CheckIfTransitionsCanBeProcessed(transitionCount);
				AddTransitions(sourceState, areInitialTransitions, transitions);
			}
		}
	}
}