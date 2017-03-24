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
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using DiscreteTimeMarkovChain;
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
			private const int MaxNumberOfDistributionsPerState = 1024;

			private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="ltmdp">The Markov chain that should be built up.</param>
			public LtmdpBuilder(LabeledTransitionMarkovDecisionProcess ltmdp)
			{
				Requires.NotNull(ltmdp, nameof(ltmdp));
				_ltmdp = ltmdp;

				InitializeDistributionIndexes();
			}

			// Each worker has its own LtmdpBuilder. Thus, we can add a buffer here
			// We create this index here in this class to avoid garbage.
			private int[] _distributionIndexes;
			private int _distributionMaxNumber;

			private void InitializeDistributionIndexes()
			{
				
				_distributionIndexes = new int[MaxNumberOfDistributionsPerState];
				for (var i = 0; i < MaxNumberOfDistributionsPerState; i++)
				{
					_distributionIndexes[i] = -1;
				}
				_distributionMaxNumber = -1;
			}

			private void ResetDistributionIndexes()
			{
				for (var i = 0; i < _distributionMaxNumber; i++)
				{
					_distributionIndexes[i] = -1;
				}
				_distributionMaxNumber = -1;
			}

			[Conditional("DEBUG")]
			private void CheckIfTransitionsCanBeProcessed(int transitionCount)
			{
				Assert.That(transitionCount > 0, "Cannot add deadlock state.");

				// Need to reserve the memory for the transitions
				var upperBoundaryForTransitions = _ltmdp._transitionChainElementCount + transitionCount;
				if (upperBoundaryForTransitions < 0)
					throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			}

			[Conditional("DEBUG")]
			private void CheckIfTransitionIsValid(Transition* transition)
			{
				Assert.That(TransitionFlags.IsValid(transition->Flags), "Attempted to add an invalid transition.");
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int GetStartPositionOfDistributionChain(int sourceState, bool areInitialTransitions)
			{
				if (areInitialTransitions)
					return _ltmdp._indexOfFirstInitialDistribution;
				return _ltmdp._stateStorageStateToFirstDistributionMemory[sourceState];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void CreateNewTransitionChainStartWithTransition(int sourceState, bool areInitialTransitions, Transition* transition)
			{
				/*
				var probTransition = (LtmdpTransition*)transition;
				var locationOfNewEntry = _ltmdp.GetPlaceForNewTransitionChainElement();

				_ltmdp._transitionChainElementsMemory[locationOfNewEntry] =
					new TransitionChainElement
					{
						Formulas = transition->Formulas,
						NextElementIndex = -1,
						Probability = probTransition->Probability,
						TargetState = transition->TargetStateIndex
					};
				if (areInitialTransitions)
				{
					_ltmdp._indexOfFirstInitialTransition = locationOfNewEntry;
				}
				else
				{
					_ltmdp.SourceStates.Add(sourceState);
					_ltmdp._stateStorageStateToFirstTransitionChainElementMemory[sourceState] = locationOfNewEntry;
				}*/
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void CreateNewDistributionsChainWithDistribution(int sourceState, bool areInitialTransitions, int distribution)
			{
				/*
				var locationOfNewEntry = _ltmdp.GetPlaceForNewTransitionChainElement();

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
					_ltmdp._indexOfFirstInitialTransition = locationOfNewEntry;
				}
				else
				{
					_ltmdp.SourceStates.Add(sourceState);
					_ltmdp._stateStorageStateToFirstTransitionChainElementMemory[sourceState] = locationOfNewEntry;
				}*/
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private TransitionChainElement GetTransitionChainElement(int index)
			{
				return _ltmdp._transitionChainElementsMemory[index];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private DistributionChainElement GetDistributionChainElement(int index)
			{
				return _ltmdp._distributionChainElementsMemory[index];
			}


			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void AddTransitionToTransitionChain(int startPositionOfChain, Transition* transition)
			{
				// Search for place to append is linear in number of existing transitions of state => O(n^2) 
				var currentElementIndex = startPositionOfChain;

				var probTransition = (LtmdpTransition*)transition;
				// merge or append
				bool mergedOrAppended = false;
				while (!mergedOrAppended)
				{
					var currentElement = GetTransitionChainElement(currentElementIndex);
					if (currentElement.TargetState == transition->TargetStateIndex && currentElement.Formulas == transition->Formulas)
					{
						//Case 1: Merge
						_ltmdp._transitionChainElementsMemory[currentElementIndex] =
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
						var locationOfNewEntry = _ltmdp.GetPlaceForNewTransitionChainElement();
						mergedOrAppended = true;
						_ltmdp._transitionChainElementsMemory[currentElementIndex] =
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
								TargetState = transition->TargetStateIndex
							};
					}
					else
					{
						//else continue iteration
						currentElementIndex = currentElement.NextElementIndex;
					}
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
			public void ProcessTransitions(TraversalContext<TExecutableModel> context, Worker<TExecutableModel> worker, int sourceState,
									   TransitionCollection transitions, int transitionCount, bool areInitialTransitions)
			{
				ResetDistributionIndexes();

				// Note, other threads might access _ltmdp at the same time
				CheckIfTransitionsCanBeProcessed(transitionCount);

				foreach (var transition in transitions)
				{
					CheckIfTransitionIsValid(transition);
					var startPositionOfDistributionChain = GetStartPositionOfDistributionChain(sourceState, areInitialTransitions);

					// startPositionOfDistributionChain == -1 indicates that no distribution chain exists
					if (startPositionOfDistributionChain == -1)
					{
						//CreateNewDistributionsChainWithDistribution(sourceState, areInitialTransitions, transition);
					}
					else
					{
						AddTransitionToTransitionChain(startPositionOfDistributionChain, transition);
					}
				}
			}
		}
	}
}