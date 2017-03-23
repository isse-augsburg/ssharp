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

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using ExecutableModel;
	using Utilities;

	//The LtmdpBuilder is tightly coupled to LabeledTransitionMarkovDecisionProcess, so we make it a nested class
	internal unsafe partial class LabeledTransitionMarkovDecisionProcess
	{
		/// <summary>
		///   Builds up a <see cref="LabeledTransitionMarkovDecisionProcess" /> instance during model traversal.
		///   Note: This only works single threaded
		/// </summary>
		internal class LtmdpBuilder<TExecutableModel> : IBatchedTransitionAction<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
		{
			private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="ltmdp">The Markov chain that should be built up.</param>
			public LtmdpBuilder(LabeledTransitionMarkovDecisionProcess ltmdp)
			{
				Requires.NotNull(ltmdp, nameof(ltmdp));
				_ltmdp = ltmdp;
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
			public void ProcessTransitions(TraversalContext<TExecutableModel> context, Worker<TExecutableModel> worker, int sourceState,
									   TransitionCollection transitions, int transitionCount, bool areInitialTransitions)
			{
				// Note, other threads might access _ltmdp at the same time

				// initialize an index
				var maxDistribution = 0;
				foreach (var transition in transitions)
				{
					var noOfDistribution = ((LtmdpTransition*)transition)->Distribution;
					if (noOfDistribution > maxDistribution)
						maxDistribution = noOfDistribution;
				}
				int* firstTransitionOfDistribution = stackalloc int[maxDistribution];
				for (var i = 0; i < maxDistribution; i++)
				{
					firstTransitionOfDistribution[i] = -1;
				}


				Assert.That(transitionCount > 0, "Cannot add deadlock state.");

				var upperBoundaryForTransitions = _ltmdp._transitionChainElementCount + transitionCount;
				if (upperBoundaryForTransitions < 0)
					throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");

				var firstTransitionIndex = -1;

				// Search for place to append is linear in number of existing transitions of state linearly => O(n^2) 
				foreach (var transition in transitions)
				{
					var probTransition = (LtmdpTransition*)transition;
					Assert.That(TransitionFlags.IsValid(probTransition->Flags), "Attempted to add an invalid transition.");

					int currentTransitionChainElementIndex;

					if (areInitialTransitions)
						currentTransitionChainElementIndex = _ltmdp._indexOfFirstInitialDistribution;
					else
						currentTransitionChainElementIndex = firstTransitionIndex;

					if (currentTransitionChainElementIndex == -1)
					{
						// Add new chain start
						var locationOfNewEntry = InterlockedExtensions.IncrementReturnOld(ref _ltmdp._transitionChainElementCount);
						if (locationOfNewEntry >= _ltmdp._maxNumberOfTransitions)
							throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
						_ltmdp._transitionChainElementsMemory[locationOfNewEntry] =
							new TransitionChainElement
							{
								Formulas = transition->Formulas,
								NextElementIndex = -1,
								Probability = probTransition->Probability,
								TargetState = transition->TargetState
							};
						if (areInitialTransitions)
						{
							_ltmdp._indexOfFirstInitialDistribution = locationOfNewEntry;
						}
						else
						{
							_ltmdp.SourceStates.Add(sourceState);
							firstTransitionIndex = locationOfNewEntry;
						}
					}
					else
					{
						// merge or append
						bool mergedOrAppended = false;
						while (!mergedOrAppended)
						{
							var currentElement = _ltmdp._transitionChainElementsMemory[currentTransitionChainElementIndex];
							if (currentElement.TargetState == transition->TargetState && currentElement.Formulas == transition->Formulas)
							{
								//Case 1: Merge
								_ltmdp._transitionChainElementsMemory[currentTransitionChainElementIndex] =
									new TransitionChainElement
									{
										Formulas = currentElement.Formulas,
										NextElementIndex = currentElement.NextElementIndex,
										Probability = probTransition->Probability + currentElement.Probability,
										TargetState = currentElement.TargetState
									};
								mergedOrAppended = true;
							}
							else if (currentElement.NextElementIndex == -1)
							{
								//Case 2: Append
								var locationOfNewEntry = InterlockedExtensions.IncrementReturnOld(ref _ltmdp._transitionChainElementCount);
								if (locationOfNewEntry >= _ltmdp._maxNumberOfTransitions)
									throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
								mergedOrAppended = true;
								_ltmdp._transitionChainElementsMemory[currentTransitionChainElementIndex] =
									new TransitionChainElement
									{
										Formulas = currentElement.Formulas,
										NextElementIndex = locationOfNewEntry,
										Probability = currentElement.Probability,
										TargetState = currentElement.TargetState
									};
								_ltmdp._transitionChainElementsMemory[locationOfNewEntry] =
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
								currentTransitionChainElementIndex = currentElement.NextElementIndex;
							}
						}
					}
				}

				var locationOfNewDistributionEntry = InterlockedExtensions.IncrementReturnOld(ref _ltmdp._distributionChainElementCount);

				_ltmdp._distributionChainElementsMemory[locationOfNewDistributionEntry] =
						new DistributionChainElement
						{
							NextElementIndex = -1,
							FirstTransitionIndex = firstTransitionIndex
						};
			}
		}

	}
}