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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.MarkovDecisionProcess.Unoptimized
{
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.GenericDataStructures;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public unsafe class LtmdpTestBuilder
	{
		internal const int StateCapacity = 1024;
		internal const int TransitionCapacity = 4096 * 100;

		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmdpTransition* _transitions;
		private int _transitionCount = 0;
		private readonly LtmdpChoiceResolver _choiceResolver;

		internal LtmdpStepGraph StepGraph { get; }
		internal LabeledTransitionMarkovDecisionProcess Ltmdp { get; }
		internal LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal LtmdpBuilder { get; }

		public LtmdpTestBuilder()
		{
			_transitionBuffer.Resize(TransitionCapacity * sizeof(LtmdpTransition), zeroMemory: false);
			_transitions = (LtmdpTransition*)_transitionBuffer.Pointer;

			StepGraph = new LtmdpStepGraph();
			_choiceResolver = new LtmdpChoiceResolver(StepGraph, true);

			Ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			LtmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal(Ltmdp);
		}

		internal int CountTargetStatesOfCid(long cid)
		{
			var targetStatesCount = 0;
			Action<LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement> counter = cge =>
			{
				if (cge.IsChoiceTypeUnsplitOrFinal)
					targetStatesCount++;
			};
			var traverser = Ltmdp.GetTreeTraverser(cid);
			traverser.ApplyActionWithStackBasedAlgorithm(counter);

			return targetStatesCount;
		}

		internal int CountTargetStatesOfInitialState()
		{
			var cidRoot = Ltmdp.GetRootContinuationGraphLocationOfInitialState();
			return CountTargetStatesOfCid(cidRoot);
		}

		internal int CountTargetStatesOfState(int state)
		{
			var cidRoot = Ltmdp.GetRootContinuationGraphLocationOfState(state);
			return CountTargetStatesOfCid(cidRoot);
		}

		internal double SumProbabilitiesOfCid(long cid)
		{
			Func<LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement, double> leafCounter = (cge) =>
			{
				var myProbability = cge.Probability;
				return myProbability;
			};
			Func<LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement, IEnumerable<double>, double> innerCounter = (cge, childProbabilities) =>
			{
				var myProbability = cge.Probability;
				var childProbability = 0.0;
				foreach (var p in childProbabilities)
				{
					childProbability += p;
				}
				return myProbability * childProbability;
			};
			var traverser = Ltmdp.GetTreeTraverser(cid);
			var probabilties = traverser.ApplyFuncWithRecursionBasedAlgorithm(innerCounter, leafCounter);

			return probabilties;
		}

		internal double SumProbabilitiesOfInitialState()
		{
			var cidRoot = Ltmdp.GetRootContinuationGraphLocationOfInitialState();
			return SumProbabilitiesOfCid(cidRoot);
		}

		internal double SumProbabilitiesOfState(int state)
		{
			var cidRoot = Ltmdp.GetRootContinuationGraphLocationOfState(state);
			return SumProbabilitiesOfCid(cidRoot);
		}


		internal void CreateTransition(bool isFormulaSatisfied, int targetStateIndex, int continuationId)
		{
			var transition = _transitionCount;
			_transitionCount++;
			_transitions[transition] = new LtmdpTransition { Index = transition };
			var t = (Transition*)(_transitions + transition);
			t->SourceStateIndex = 0;
			t->TargetStateIndex = targetStateIndex;
			t->Formulas = new StateFormulaSet(new Func<bool>[] { () => isFormulaSatisfied });
			t->Flags = TransitionFlags.IsValidFlag | TransitionFlags.IsStateTransformedToIndexFlag;
			t->ActivatedFaults = new FaultSet();
		}

		internal TransitionCollection CreateTransitionCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _transitionCount, _transitionCount, sizeof(LtmdpTransition), StepGraph);
		}

		internal void Clear()
		{
			_transitionCount = 0;
			_choiceResolver.PrepareNextState();
			StepGraph.Clear();
		}

		internal void ProcessInitialTransitions()
		{
			LtmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);
		}

		internal void ProcessStateTransitions(int sourceState)
		{
			LtmdpBuilder.ProcessTransitions(null, null, sourceState, CreateTransitionCollection(), _transitionCount, false);
		}

	}

	public class LtmdpBuilderDuringTraversalTests
	{
		public TestTraceOutput Output { get; }

		public LtmdpTestBuilder LtmdpTestBuilder { get; } = new LtmdpTestBuilder();

		public LtmdpBuilderDuringTraversalTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void OneReflexiveTransition()
		{
			// add initial state
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 5, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0,1.0);
			LtmdpTestBuilder.ProcessInitialTransitions();
			LtmdpTestBuilder.Clear();

			// add reflexive state 5
			LtmdpTestBuilder.CreateTransition(false, 5, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(5);

			LtmdpTestBuilder.Ltmdp.TransitionTargets.ShouldBe(2);
			LtmdpTestBuilder.Ltmdp.SourceStates.Count.ShouldBe(1);
			LtmdpTestBuilder.Ltmdp.SourceStates.First().ShouldBe(5);

			var initialTransitionTargets = LtmdpTestBuilder.CountTargetStatesOfInitialState();
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = LtmdpTestBuilder.SumProbabilitiesOfInitialState();
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = LtmdpTestBuilder.CountTargetStatesOfState(5);
			transitionTargetsOfState5.ShouldBe(1);
			var probabilitySumOfState5 = LtmdpTestBuilder.SumProbabilitiesOfState(5);
			probabilitySumOfState5.ShouldBe(1.0);
		}

		[Fact]
		public void ThreeReflexiveStatesFromInitialState()
		{
			// add initial state
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.StepGraph.ProbabilisticSplit(0,1,3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(1, 0.3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(2, 0.3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(3, 0.4);
			LtmdpTestBuilder.CreateTransition(false, 5, 1);
			LtmdpTestBuilder.CreateTransition(false, 7, 2);
			LtmdpTestBuilder.CreateTransition(false, 2, 3);
			LtmdpTestBuilder.ProcessInitialTransitions();

			// add reflexive state 5
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 5, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(5);

			// add reflexive state 7
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 5, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(7);

			// add reflexive state 2
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 2, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(2);


			LtmdpTestBuilder.Ltmdp.TransitionTargets.ShouldBe(6);
			LtmdpTestBuilder.Ltmdp.SourceStates.Count.ShouldBe(3);
			LtmdpTestBuilder.Ltmdp.SourceStates.First(state => state==5).ShouldBe(5);

			var initialTransitionTargets = LtmdpTestBuilder.CountTargetStatesOfInitialState();
			initialTransitionTargets.ShouldBe(3);
			var initialProbabilitySum = LtmdpTestBuilder.SumProbabilitiesOfInitialState();
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = LtmdpTestBuilder.CountTargetStatesOfState(5);
			transitionTargetsOfState5.ShouldBe(1);
			var probabilitySumOfState5 = LtmdpTestBuilder.SumProbabilitiesOfState( 5);
			probabilitySumOfState5.ShouldBe(1.0);
		}
		
		[Fact]
		public void ThreeReflexiveStatesFromNonInitialState()
		{
			// add initial state
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 5, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessInitialTransitions();

			// add state 5
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.StepGraph.ProbabilisticSplit(0, 1, 3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(1, 0.3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(2, 0.3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(3, 0.4);
			LtmdpTestBuilder.CreateTransition(false, 7, 1);
			LtmdpTestBuilder.CreateTransition(false, 2, 2);
			LtmdpTestBuilder.CreateTransition(false, 1, 3);
			LtmdpTestBuilder.ProcessStateTransitions(5);

			// add reflexive state 7
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 7, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(7);

			// add reflexive state 2
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 2, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(2);

			// add reflexive state 1
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 1, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(1);


			LtmdpTestBuilder.Ltmdp.TransitionTargets.ShouldBe(7);
			LtmdpTestBuilder.Ltmdp.SourceStates.Count.ShouldBe(4);
			LtmdpTestBuilder.Ltmdp.SourceStates.First(state => state == 5).ShouldBe(5);

			var initialTransitionTargets = LtmdpTestBuilder.CountTargetStatesOfInitialState();
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = LtmdpTestBuilder.SumProbabilitiesOfInitialState();
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = LtmdpTestBuilder.CountTargetStatesOfState( 5);
			transitionTargetsOfState5.ShouldBe(3);
			var probabilitySumOfState5 = LtmdpTestBuilder.SumProbabilitiesOfState( 5);
			probabilitySumOfState5.ShouldBe(1.0);
		}

		[Fact]
		public void StatesFromNonInitialStateWithMoreDistributions()
		{
			// add initial state
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 5, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessInitialTransitions();

			// add state 5
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.StepGraph.NonDeterministicSplit(0, 1, 3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.StepGraph.ProbabilisticSplit(2, 4, 6);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(2, 1.0);
			LtmdpTestBuilder.CreateTransition(false, 1, 1 );
			LtmdpTestBuilder.CreateTransition(false, 7, 4 );
			LtmdpTestBuilder.CreateTransition(false, 2, 5 );
			LtmdpTestBuilder.CreateTransition(false, 1, 6 );
			LtmdpTestBuilder.CreateTransition(false, 7, 3 );
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(4, 0.3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(5, 0.3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(6, 0.4);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(1, 1.0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(3, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(5);

			// add reflexive state 7
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.StepGraph.NonDeterministicSplit(0, 1, 2);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.StepGraph.ProbabilisticSplit(1, 3, 4);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(1, 1.0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(2, 1.0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(3, 0.2);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(4, 0.8);
			LtmdpTestBuilder.CreateTransition(false, 7, 3);
			LtmdpTestBuilder.CreateTransition(false, 2, 4);
			LtmdpTestBuilder.CreateTransition(false, 1, 2);
			LtmdpTestBuilder.ProcessStateTransitions(7);



			// add reflexive state 2
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 2, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(2);

			// add reflexive state 1
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 1, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(1);


			LtmdpTestBuilder.Ltmdp.TransitionTargets.ShouldBe(11);
			LtmdpTestBuilder.Ltmdp.SourceStates.Count.ShouldBe(4);
			LtmdpTestBuilder.Ltmdp.SourceStates.First(state => state == 5).ShouldBe(5);
			
			var initialTransitionTargets = LtmdpTestBuilder.CountTargetStatesOfInitialState();
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = LtmdpTestBuilder.SumProbabilitiesOfInitialState();
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = LtmdpTestBuilder.CountTargetStatesOfState(5);
			transitionTargetsOfState5.ShouldBe(5);
			var probabilitySumOfState5 = LtmdpTestBuilder.SumProbabilitiesOfState(5);
			probabilitySumOfState5.ShouldBe(3.0,0.00000001);
		}


		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplitsWithRemove()
		{
			// add initial state
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 5, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessInitialTransitions();

			// add state 5
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.StepGraph.ProbabilisticSplit(0, 1, 3);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(1, 0.3);
			LtmdpTestBuilder.StepGraph.NonDeterministicSplit(1, 4, 5);
			LtmdpTestBuilder.StepGraph.PruneChoicesOfCidTo2(0);
			LtmdpTestBuilder.StepGraph.Forward(2, 4);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(2, 0.7);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(4, 1.0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(5, 1.0);
			LtmdpTestBuilder.CreateTransition(false, 7, 4);
			LtmdpTestBuilder.CreateTransition(false, 2, 5);
			LtmdpTestBuilder.ProcessStateTransitions(5);

			// add reflexive state 7
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 7, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(7);

			// add reflexive state 2
			LtmdpTestBuilder.Clear();
			LtmdpTestBuilder.CreateTransition(false, 2, 0);
			LtmdpTestBuilder.StepGraph.SetProbabilityOfContinuationId(0, 1.0);
			LtmdpTestBuilder.ProcessStateTransitions(2);

			LtmdpTestBuilder.Ltmdp.TransitionTargets.ShouldBe(5);
			LtmdpTestBuilder.Ltmdp.SourceStates.Count.ShouldBe(3);


			// check that the transformed forward node (was 2) points to the correct target (was 4) and not to anything else.
			var initialRootCidLocation = LtmdpTestBuilder.Ltmdp.GetRootContinuationGraphLocationOfInitialState();
			var initialTarget = LtmdpTestBuilder.Ltmdp.GetContinuationGraphElement(initialRootCidLocation);
			var state5Transformed = LtmdpTestBuilder.Ltmdp.GetTransitionTarget((int)initialTarget.To);
			var state5TransformedRootCidLocation = LtmdpTestBuilder.Ltmdp.GetRootContinuationGraphLocationOfState(state5Transformed.TargetState);
			var state5TransformedRootCid = LtmdpTestBuilder.Ltmdp.GetContinuationGraphElement(state5TransformedRootCidLocation);
			var state5Successors = state5TransformedRootCid.To - state5TransformedRootCid.From + 1;
			state5Successors.ShouldBe(2);
			var state5TransformedFirstSplitCidLocation = state5TransformedRootCid.From;
			var state5TransformedFirstSplitCid = LtmdpTestBuilder.Ltmdp.GetContinuationGraphElement(state5TransformedFirstSplitCidLocation);
			var state5TransformedTargetOfForward = state5TransformedFirstSplitCid.From;
			var state5ForwardNode = LtmdpTestBuilder.Ltmdp.GetContinuationGraphElement(state5TransformedRootCid.To);
			state5ForwardNode.ChoiceType.ShouldBe(LtmdpChoiceType.Forward);
			state5ForwardNode.To.ShouldBe(state5TransformedTargetOfForward);
		}
	}
}
