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
			private int _maxNumberOfDistributionsPerState;

			private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="ltmdp">The Markov chain that should be built up.</param>
			public LtmdpBuilder(LabeledTransitionMarkovDecisionProcess ltmdp, AnalysisConfiguration configuration)
			{
				Requires.NotNull(ltmdp, nameof(ltmdp));
				_ltmdp = ltmdp;
				
				InitializeDistributionChainIndexCache(configuration.SuccessorCapacity);
			}

			// Each worker has its own LtmdpBuilder. Thus, we can add a buffer here
			// We create this index here in this class to avoid garbage.
			private readonly MemoryBuffer _distributionChainIndexCacheBuffer = new MemoryBuffer();
			private int* _distributionChainIndexCache;

			private void InitializeDistributionChainIndexCache(long maxNumberOfDistributionsPerState)
			{
				Assert.That(maxNumberOfDistributionsPerState<=int.MaxValue, "maxNumberOfDistributionsPerState must fit into an integer");

				_maxNumberOfDistributionsPerState = (int)maxNumberOfDistributionsPerState;
				_distributionChainIndexCacheBuffer.Resize((long)_maxNumberOfDistributionsPerState * sizeof(int), zeroMemory: false);
				_distributionChainIndexCache = (int*)_distributionChainIndexCacheBuffer.Pointer;
				
				ResetDistributionChainIndexCache();
			}

			private void ResetDistributionChainIndexCache()
			{
				MemoryBuffer.SetAllBitsMemoryWithInitblk.ClearWithMinus1(_distributionChainIndexCache, _maxNumberOfDistributionsPerState);
			}

			private int TryToFindDistributionChainIndexInCache(int distribution)
			{
				return _distributionChainIndexCache[distribution];
			}

			private void UpdateDistributionChainIndexCache(int distribution, int locationOfEntry)
			{
				Assert.That(distribution < _maxNumberOfDistributionsPerState, "distribution exceeds _maxNumberOfDistributionsPerState.");
				_distributionChainIndexCache[distribution] = locationOfEntry;
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
			private void CheckIfTransitionIsValid(LtmdpTransition* transition)
			{
				Assert.That(TransitionFlags.IsValid(transition->Flags), "Attempted to add an invalid transition.");
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int GetStartPositionOfDistributionChain(int sourceState, bool areInitialTransitions)
			{
				if (areInitialTransitions)
					return _ltmdp._indexOfFirstInitialDistribution;
				return _ltmdp._stateStorageStateToFirstDistributionChainElementMemory[sourceState];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private DistributionChainElement GetDistributionChainElement(int index)
			{
				return _ltmdp._distributionChainElementsMemory[index];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int FindOrCreateDistributionChainElementInExistingDistributionChain(int startPositionOfDistributionChain, int distribution)
			{
				// Search for place to append is linear in number of existing distributions of state
				var currentDistributionElementIndex = startPositionOfDistributionChain;
				
				while (true)
				{
					var currentElement = GetDistributionChainElement(currentDistributionElementIndex);
					if (currentElement.Distribution == distribution)
					{
						//Case 1: Found
						return currentDistributionElementIndex;
					}
					if (currentElement.NextElementIndex == -1)
					{
						//Case 2: Append
						var locationOfNewEntry = _ltmdp.GetPlaceForNewDistributionChainElement();
						_ltmdp._distributionChainElementsMemory[currentDistributionElementIndex].NextElementIndex =
							locationOfNewEntry;
						_ltmdp._distributionChainElementsMemory[locationOfNewEntry] =
							new DistributionChainElement
							{
								FirstTransitionIndex = -1,
								Distribution = distribution,
								NextElementIndex = -1,
							};
						UpdateDistributionChainIndexCache(distribution, locationOfNewEntry);
						return locationOfNewEntry;
					}
					//else continue iteration
					currentDistributionElementIndex = currentElement.NextElementIndex;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int CreateNewDistributionChainStartWithDistribution(int sourceState, bool areInitialTransitions, int distribution)
			{
				var locationOfNewEntry = _ltmdp.GetPlaceForNewDistributionChainElement();

				if (areInitialTransitions)
				{
					_ltmdp._indexOfFirstInitialDistribution = locationOfNewEntry;
				}
				else
				{
					_ltmdp.SourceStates.Add(sourceState);
					_ltmdp._stateStorageStateToFirstDistributionChainElementMemory[sourceState] = locationOfNewEntry;
				}

				_ltmdp._distributionChainElementsMemory[locationOfNewEntry] =
					new DistributionChainElement
					{
						FirstTransitionIndex = -1,
						Distribution = distribution,
						NextElementIndex = -1,
					};
				UpdateDistributionChainIndexCache(distribution, locationOfNewEntry);
				return locationOfNewEntry;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int FindOrCreateDistributionChainElementWithDistribution(int sourceState, bool areInitialTransitions, int distribution)
			{
				//try to find in cache
				var valueInCache = TryToFindDistributionChainIndexInCache(distribution);
				if (valueInCache!=-1)
					return valueInCache;

				var startPositionOfDistributionChain = GetStartPositionOfDistributionChain(sourceState, areInitialTransitions);

				// now we select the correct distribution
				// startPositionOfDistributionChain == -1 indicates that no distribution chain for state exists
				if (startPositionOfDistributionChain == -1)
				{
					var positionOfDistributionChain = CreateNewDistributionChainStartWithDistribution(sourceState, areInitialTransitions, distribution);
					return positionOfDistributionChain;
				}
				else
				{
					var positionOfDistributionChain = FindOrCreateDistributionChainElementInExistingDistributionChain(startPositionOfDistributionChain, distribution);
					return positionOfDistributionChain;
				}
			}



			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int GetStartPositionOfTransitionChain(int positionOfDistributionElementChain)
			{
				return _ltmdp._distributionChainElementsMemory[positionOfDistributionElementChain].FirstTransitionIndex;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private TransitionChainElement GetTransitionChainElement(int index)
			{
				return _ltmdp._transitionChainElementsMemory[index];
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void CreateNewTransitionChainStartWithTransition(int positionOfDistributionElementChain, LtmdpTransition* transition)
			{
				var locationOfNewEntry = _ltmdp.GetPlaceForNewTransitionChainElement();
				
				_ltmdp._distributionChainElementsMemory[positionOfDistributionElementChain].FirstTransitionIndex =
					locationOfNewEntry;

				_ltmdp._transitionChainElementsMemory[locationOfNewEntry] =
					new TransitionChainElement
					{
						Formulas = transition->Formulas,
						NextElementIndex = -1,
						Probability = transition->Probability,
						TargetState = transition->GetTargetStateIndex()
					};
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void AddTransitionToExistingTransitionChain(int startPositionOfTransitionChain, LtmdpTransition* transition)
			{
				// Search for place to append is linear in number of existing transitions of state => O(n^2) 
				var currentTransitionElementIndex = startPositionOfTransitionChain;

				// merge or append
				while (true)
				{
					var currentElement = GetTransitionChainElement(currentTransitionElementIndex);
					if (currentElement.TargetState == transition->GetTargetStateIndex() && currentElement.Formulas == transition->Formulas)
					{
						//Case 1: Merge
						_ltmdp._transitionChainElementsMemory[currentTransitionElementIndex].Probability =
							transition->Probability + currentElement.Probability;
						return;
					}
					if (currentElement.NextElementIndex == -1)
					{
						//Case 2: Append
						var locationOfNewEntry = _ltmdp.GetPlaceForNewTransitionChainElement();
						_ltmdp._transitionChainElementsMemory[currentTransitionElementIndex].NextElementIndex =
							locationOfNewEntry;
						_ltmdp._transitionChainElementsMemory[locationOfNewEntry] =
							new TransitionChainElement
							{
								Formulas = transition->Formulas,
								NextElementIndex = -1,
								Probability = transition->Probability,
								TargetState = transition->GetTargetStateIndex()
							};
						return;
					}
					//else continue iteration
					currentTransitionElementIndex = currentElement.NextElementIndex;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void AddTransitionToTransitionChain(int positionOfDistributionElementChain, LtmdpTransition* probTransition)
			{
				var startPositionOfTransitionChain = GetStartPositionOfTransitionChain(positionOfDistributionElementChain);

				// startPositionOfChain == -1 indicates that no transition chain for state exists
				if (startPositionOfTransitionChain == -1)
				{
					CreateNewTransitionChainStartWithTransition(positionOfDistributionElementChain, probTransition);
				}
				else
				{
					AddTransitionToExistingTransitionChain(startPositionOfTransitionChain, probTransition);
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
				ResetDistributionChainIndexCache();

				// Note, other threads might access _ltmdp at the same time
				CheckIfTransitionsCanBeProcessed(transitionCount);

				foreach (var transition in transitions)
				{
					var probTransition = (LtmdpTransition*)transition;

					CheckIfTransitionIsValid(probTransition);

					var positionOfDistributionElementChain = FindOrCreateDistributionChainElementWithDistribution(sourceState, areInitialTransitions, probTransition->Distribution);

					AddTransitionToTransitionChain(positionOfDistributionElementChain, probTransition);
				}
			}
		}
	}
}