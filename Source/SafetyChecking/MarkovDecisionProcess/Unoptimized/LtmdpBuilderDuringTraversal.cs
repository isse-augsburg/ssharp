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

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using DiscreteTimeMarkovChain;
	using ExecutableModel;
	using Utilities;
	using GenericDataStructures;
	using Modeling;

	//The LtmdpBuilder is tightly coupled to LabeledTransitionMarkovDecisionProcess, so we make it a nested class
	internal unsafe partial class LabeledTransitionMarkovDecisionProcess
	{
		/// <summary>
		///   Builds up a <see cref="LabeledTransitionMarkovDecisionProcess" /> instance during model traversal.
		///   Note: This only works single threaded
		/// </summary>
		internal class LtmdpBuilderDuringTraversal<TExecutableModel> : IBatchedTransitionAction<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
		{
			private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;

			private readonly AutoResizeVector<int> _continuationIdToTransitionTarget = new AutoResizeVector<int>();

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="ltmdp">The Markov chain that should be built up.</param>
			public LtmdpBuilderDuringTraversal(LabeledTransitionMarkovDecisionProcess ltmdp, AnalysisConfiguration configuration)
			{
				Requires.NotNull(ltmdp, nameof(ltmdp));
				_ltmdp = ltmdp;
			}
			

			[Conditional("DEBUG")]
			private void CheckIfTransitionsCanBeProcessed(int transitionCount)
			{
				Assert.That(transitionCount > 0, "Cannot add deadlock state.");

				// Need to reserve the memory for the transitions
				var upperBoundaryForTransitions = _ltmdp._transitionTargetCount + transitionCount;
				if (upperBoundaryForTransitions < 0 || upperBoundaryForTransitions >= _ltmdp._maxNumberOfTransitionTargets)
					throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			}

			[Conditional("DEBUG")]
			private void CheckIfStepGraphCanBeProcessed(LtmdpStepGraph stepGraph)
			{
				// Need to reserve the memory for the transitions
				var upperBoundaryForStepGraph = _ltmdp._continuationGraphElementCount + stepGraph.Size;
				if (upperBoundaryForStepGraph < 0 || upperBoundaryForStepGraph >= _ltmdp._maxNumberOfContinuationGraphElements)
					throw new OutOfMemoryException("Unable to store transitions. Try increasing the transition capacity.");
			}

			[Conditional("DEBUG")]
			private void CheckIfTransitionIsValid(LtmdpTransition* transition)
			{
				Assert.That(TransitionFlags.IsValid(transition->Flags), "Attempted to add an invalid transition.");
			}

			private void AddTransitions(TransitionCollection transitions)
			{
				foreach (var transition in transitions)
				{
					var probTransition = (LtmdpTransition*)transition;

					CheckIfTransitionIsValid(probTransition);

					var place = _ltmdp.GetPlaceForNewTransitionTargetElement();
					_ltmdp._transitionTarget[place] =
						new TransitionTargetElement
						{
							TargetState = transition->TargetStateIndex,
							Formulas = probTransition->Formulas,
							Probability = probTransition->Probability
						};
					_continuationIdToTransitionTarget[probTransition->ContinuationId] = place;
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private void AddFinalChoiceOfStepGraph(int continuationId, long locationForContinuationGraphElement)
			{
				var transitionTarget = _continuationIdToTransitionTarget[continuationId];

				_ltmdp._continuationGraph[locationForContinuationGraphElement] =
					new ContinuationGraphElement
					{
						ChoiceType = LtmdpChoiceType.UnsplitOrFinal,
						To = transitionTarget,
					};
			}

			
			private void AddChoiceOfStepGraph(LtmdpStepGraph stepGraph, int continuationId, long locationForContinuationGraphElement)
			{
				var choice = stepGraph.GetChoiceOfCid(continuationId);

				if (choice.IsChoiceTypeUnsplitOrFinal)
				{
					AddFinalChoiceOfStepGraph(continuationId, locationForContinuationGraphElement);
					return;
				}

				var offsetTo = choice.To - choice.From;
				var numberOfChildren = offsetTo + 1;

				var placesForChildren = _ltmdp.GetPlaceForNewContinuationGraphElements(numberOfChildren);

				_ltmdp._continuationGraph[locationForContinuationGraphElement] =
					new ContinuationGraphElement
					{
						ChoiceType = choice.ChoiceType,
						From = placesForChildren,
						To = placesForChildren + offsetTo,
					};
				
				for (var currentChildNo = 0; currentChildNo < numberOfChildren; currentChildNo++)
				{
					var originalContinuationId = choice.From + currentChildNo;
					var newLocation = placesForChildren + currentChildNo;
					AddChoiceOfStepGraph(stepGraph, originalContinuationId, newLocation);
				}
			}

			private void AddStepGraph(LtmdpStepGraph stepGraph, int sourceState, bool areInitialTransitions)
			{
				var place = _ltmdp.GetPlaceForNewContinuationGraphElements(1);
				if (areInitialTransitions)
				{
					_ltmdp._indexOfInitialContinuationGraphRoot = place;
				}
				else
				{
					_ltmdp.SourceStates.Add(sourceState);
					_ltmdp._stateStorageStateToRootOfContinuationGraphMemory[sourceState] = place;
				}
				AddChoiceOfStepGraph(stepGraph, 0, place);
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
				CheckIfTransitionsCanBeProcessed(transitionCount);

				AddTransitions(transitions);
				var stepgraph = (LtmdpStepGraph)transitions.StructuralInformation;
				CheckIfStepGraphCanBeProcessed(stepgraph);
				AddStepGraph(stepgraph, sourceState, areInitialTransitions);
			}
		}
	}
}