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
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public unsafe class LtmdpToNmdpTests
	{
		private const int StateCapacity = 1024;
		private const int TransitionCapacity = 4096*100;
		public TestTraceOutput Output { get; }

		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmdpTransition* _transitions;
		private int _transitionCount = 0;
		private readonly LtmdpStepGraph _stepGraph;
		private readonly LtmdpChoiceResolver _choiceResolver;

		public LtmdpToNmdpTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
			
			_transitionBuffer.Resize(TransitionCapacity * sizeof(LtmdpTransition), zeroMemory: false);
			_transitions = (LtmdpTransition*)_transitionBuffer.Pointer;

			_stepGraph = new LtmdpStepGraph();
			_choiceResolver = new LtmdpChoiceResolver(_stepGraph,true);
		}
		
		private int CountTargetStatesOfCid(NestedMarkovDecisionProcess nmdp,long cid)
		{
			var targetStatesCount = 0;
			Action<NestedMarkovDecisionProcess.ContinuationGraphElement> counter = cge =>
			{
				if (cge.IsChoiceTypeUnsplitOrFinal)
					targetStatesCount++;
			};
			var traverser = nmdp.GetTreeTraverser(cid);
			traverser.ApplyActionWithStackBasedAlgorithm(counter);

			return targetStatesCount;
		}

		private int CountTargetStatesOfInitialState(NestedMarkovDecisionProcess nmdp)
		{
			var cidRoot = nmdp.GetRootContinuationGraphLocationOfInitialState();
			return CountTargetStatesOfCid(nmdp,cidRoot);
		}

		private int CountTargetStatesOfState(NestedMarkovDecisionProcess nmdp, int state)
		{
			var cidRoot = nmdp.GetRootContinuationGraphLocationOfState(state);
			return CountTargetStatesOfCid(nmdp, cidRoot);
		}

		private double SumProbabilitiesOfCid(NestedMarkovDecisionProcess nmdp, long cid)
		{
			var probabilties = 0.0;
			Action<NestedMarkovDecisionProcess.ContinuationGraphElement> counter = cge =>
			{
				if (cge.IsChoiceTypeUnsplitOrFinal)
				{
					var cgl = cge.AsLeaf;
					var probability = cgl.Probability;
					probabilties += probability;
				}
			};
			var traverser = nmdp.GetTreeTraverser(cid);
			traverser.ApplyActionWithStackBasedAlgorithm(counter);

			return probabilties;
		}

		private double SumProbabilitiesOfInitialState(NestedMarkovDecisionProcess nmdp)
		{
			var cidRoot = nmdp.GetRootContinuationGraphLocationOfInitialState();
			return SumProbabilitiesOfCid(nmdp, cidRoot);
		}

		private double SumProbabilitiesOfState(NestedMarkovDecisionProcess nmdp, int state)
		{
			var cidRoot = nmdp.GetRootContinuationGraphLocationOfState(state);
			return SumProbabilitiesOfCid(nmdp, cidRoot);
		}


		private void CreateTransition(bool isFormulaSatisfied, int targetStateIndex, int continuationId)
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

		private TransitionCollection CreateTransitionCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _transitionCount, _transitionCount, sizeof(LtmdpTransition), _stepGraph);
		}

		private void Clear()
		{
			_transitionCount = 0;
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
		}


		[Fact]
		public void OneReflexiveTransition()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp,AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);
			Clear();

			// add reflexive state 5
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			var ltmdpToNmdp = new LtmdpToNmdp(ltmdp);
			var nmdp = ltmdpToNmdp.NestedMarkovDecisionProcess;

			nmdp.ContinuationGraphSize.ShouldBe(2);
			nmdp.States.ShouldBe(1);

			var initialTransitionTargets = CountTargetStatesOfInitialState(nmdp);
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(nmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState0 = CountTargetStatesOfState(nmdp, 0);
			transitionTargetsOfState0.ShouldBe(1);
			var probabilitySumOfState0 = SumProbabilitiesOfState(nmdp, 0);
			probabilitySumOfState0.ShouldBe(1.0);
		}

		[Fact]
		public void ThreeReflexiveStatesFromInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp, AnalysisConfiguration.Default);
			
			// add initial state
			Clear();
			_stepGraph.ProbabilisticSplit(0, 1, 3);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			_stepGraph.SetProbabilityOfContinuationId(1, 0.3);
			_stepGraph.SetProbabilityOfContinuationId(2, 0.3);
			_stepGraph.SetProbabilityOfContinuationId(3, 0.4);
			CreateTransition(false, 5, 1);
			CreateTransition(false, 7, 2);
			CreateTransition(false, 2, 3);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add reflexive state 5
			Clear();
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			Clear();
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			Clear();
			CreateTransition(false, 2, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			var ltmdpToNmdp = new LtmdpToNmdp(ltmdp);
			var nmdp = ltmdpToNmdp.NestedMarkovDecisionProcess;
			
			nmdp.ContinuationGraphSize.ShouldBe(7);
			nmdp.States.ShouldBe(3);

			var initialTransitionTargets = CountTargetStatesOfInitialState(nmdp);
			initialTransitionTargets.ShouldBe(3);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(nmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState0 = CountTargetStatesOfState(nmdp, 0);
			transitionTargetsOfState0.ShouldBe(1);
			var probabilitySumOfState0 = SumProbabilitiesOfState(nmdp, 0);
			probabilitySumOfState0.ShouldBe(1.0);
		}
		
		[Fact]
		public void ThreeReflexiveStatesFromNonInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp, AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add state 5
			Clear();
			_stepGraph.ProbabilisticSplit(0, 1, 3);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			_stepGraph.SetProbabilityOfContinuationId(1, 0.3);
			_stepGraph.SetProbabilityOfContinuationId(2, 0.3);
			_stepGraph.SetProbabilityOfContinuationId(3, 0.4);
			CreateTransition(false, 7, 1);
			CreateTransition(false, 2, 2);
			CreateTransition(false, 1, 3);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			Clear();
			CreateTransition(false, 7, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			Clear();
			CreateTransition(false, 2, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 1
			Clear();
			CreateTransition(false, 1, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 1, CreateTransitionCollection(), _transitionCount, false);

			var ltmdpToNmdp = new LtmdpToNmdp(ltmdp);
			var nmdp = ltmdpToNmdp.NestedMarkovDecisionProcess;

			nmdp.ContinuationGraphSize.ShouldBe(8);
			nmdp.States.ShouldBe(4);

			var initialTransitionTargets = CountTargetStatesOfInitialState(nmdp);
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(nmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState0 = CountTargetStatesOfState(nmdp, 0);
			transitionTargetsOfState0.ShouldBe(3);
			var probabilitySumOfState0 = SumProbabilitiesOfState(nmdp, 0);
			probabilitySumOfState0.ShouldBe(1.0);
		}

		[Fact]
		public void StatesFromNonInitialStateWithMoreDistributions()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp, AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add state 5
			Clear();
			_stepGraph.NonDeterministicSplit(0, 1, 3);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			_stepGraph.ProbabilisticSplit(2, 4, 6);
			_stepGraph.SetProbabilityOfContinuationId(2, 1.0);
			CreateTransition(false, 1, 1);
			CreateTransition(false, 7, 4);
			CreateTransition(false, 2, 5);
			CreateTransition(false, 1, 6);
			CreateTransition(false, 7, 3);
			_stepGraph.SetProbabilityOfContinuationId(4, 0.3);
			_stepGraph.SetProbabilityOfContinuationId(5, 0.3);
			_stepGraph.SetProbabilityOfContinuationId(6, 0.4);
			_stepGraph.SetProbabilityOfContinuationId(1, 1.0);
			_stepGraph.SetProbabilityOfContinuationId(3, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			Clear();
			_stepGraph.NonDeterministicSplit(0, 1, 2);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			_stepGraph.ProbabilisticSplit(1, 3, 4);
			_stepGraph.SetProbabilityOfContinuationId(1, 1.0);
			_stepGraph.SetProbabilityOfContinuationId(2, 1.0);
			_stepGraph.SetProbabilityOfContinuationId(3, 0.2);
			_stepGraph.SetProbabilityOfContinuationId(4, 0.8);
			CreateTransition(false, 7, 3);
			CreateTransition(false, 2, 4);
			CreateTransition(false, 1, 2);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);



			// add reflexive state 2
			Clear();
			CreateTransition(false, 2, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 1
			Clear();
			CreateTransition(false, 1, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 1, CreateTransitionCollection(), _transitionCount, false);

			var ltmdpToNmdp = new LtmdpToNmdp(ltmdp);
			var nmdp = ltmdpToNmdp.NestedMarkovDecisionProcess;


			nmdp.ContinuationGraphSize.ShouldBe(15);
			nmdp.States.ShouldBe(4);
			
			var initialTransitionTargets = CountTargetStatesOfInitialState(nmdp);
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(nmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState0 = CountTargetStatesOfState(nmdp, 0);
			transitionTargetsOfState0.ShouldBe(5);
			var probabilitySumOfState0 = SumProbabilitiesOfState(nmdp,0);
			probabilitySumOfState0.ShouldBe(3.0);
		}



		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplitsWithRemove()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp, AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add state 5
			Clear();
			_stepGraph.ProbabilisticSplit(0, 1, 3);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			_stepGraph.SetProbabilityOfContinuationId(1, 0.3);
			_stepGraph.NonDeterministicSplit(1, 4, 5);
			_stepGraph.PruneChoicesOfCidTo2(0);
			_stepGraph.Forward(2, 4);
			_stepGraph.SetProbabilityOfContinuationId(2, 0.7);
			_stepGraph.SetProbabilityOfContinuationId(4, 1.0);
			_stepGraph.SetProbabilityOfContinuationId(5, 1.0);
			CreateTransition(false, 7, 4);
			CreateTransition(false, 2, 5);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			Clear();
			CreateTransition(false, 7, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			Clear();
			CreateTransition(false, 2, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			var ltmdpToNmdp = new LtmdpToNmdp(ltmdp);
			var nmdp = ltmdpToNmdp.NestedMarkovDecisionProcess;
			
			nmdp.ContinuationGraphSize.ShouldBe(8);
			nmdp.States.ShouldBe(3);

			var initialTransitionTargets = CountTargetStatesOfInitialState(nmdp);
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(nmdp);
			initialProbabilitySum.ShouldBe(1.0);


			// check that the transformed forward node (was 2) points to the correct target (was 4) and not to anything else.
			var initialRootCidLocation = nmdp.GetRootContinuationGraphLocationOfInitialState();
			var initialTarget = nmdp.GetContinuationGraphLeaf(initialRootCidLocation);
			var state5Transformed = initialTarget.ToState;
			var state5TransformedRootCidLocation = nmdp.GetRootContinuationGraphLocationOfState(state5Transformed);
			var state5TransformedRootCid = nmdp.GetContinuationGraphInnerNode(state5TransformedRootCidLocation);
			var state5Successors = state5TransformedRootCid.ToCid - state5TransformedRootCid.FromCid + 1;
			state5Successors.ShouldBe(2);
			var state5TransformedFirstSplitCidLocation = state5TransformedRootCid.FromCid;
			var state5TransformedFirstSplitCid = nmdp.GetContinuationGraphInnerNode(state5TransformedFirstSplitCidLocation);
			var state5TransformedTargetOfForward = state5TransformedFirstSplitCid.FromCid;
			var state5ForwardNode = nmdp.GetContinuationGraphInnerNode(state5TransformedRootCid.ToCid);
			state5ForwardNode.ChoiceType.ShouldBe(LtmdpChoiceType.Forward);
			state5ForwardNode.ToCid.ShouldBe(state5TransformedTargetOfForward);
		}
	}
}
