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

	public unsafe class LtmdpBuilderDuringTraversalTests
	{
		private const int StateCapacity = 1024;
		private const int TransitionCapacity = 4096*100;
		public TestTraceOutput Output { get; }

		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmdpTransition* _transitions;
		private int _transitionCount = 0;
		private readonly LtmdpStepGraph _stepGraph;
		private readonly LtmdpChoiceResolver _choiceResolver;

		public LtmdpBuilderDuringTraversalTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
			
			_transitionBuffer.Resize(TransitionCapacity * sizeof(LtmdpTransition), zeroMemory: false);
			_transitions = (LtmdpTransition*)_transitionBuffer.Pointer;

			_stepGraph = new LtmdpStepGraph();
			_choiceResolver = new LtmdpChoiceResolver(_stepGraph,true);
		}
		
		private int CountTargetStatesOfCid(LabeledTransitionMarkovDecisionProcess ltmdp,long cid)
		{
			var targetStatesCount = 0;
			Action<LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement> counter = cge =>
			{
				if (cge.IsChoiceTypeUnsplitOrFinal)
					targetStatesCount++;
			};
			var traverser = ltmdp.GetTreeTraverser(cid);
			traverser.ApplyActionWithStackBasedAlgorithm(counter);

			return targetStatesCount;
		}

		private int CountTargetStatesOfInitialState(LabeledTransitionMarkovDecisionProcess ltmdp)
		{
			var cidRoot = ltmdp.GetRootContinuationGraphLocationOfInitialState();
			return CountTargetStatesOfCid(ltmdp,cidRoot);
		}

		private int CountTargetStatesOfState(LabeledTransitionMarkovDecisionProcess ltmdp, int state)
		{
			var cidRoot = ltmdp.GetRootContinuationGraphLocationOfState(state);
			return CountTargetStatesOfCid(ltmdp, cidRoot);
		}

		private double SumProbabilitiesOfCid(LabeledTransitionMarkovDecisionProcess ltmdp, long cid)
		{
			Func<LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement, double> leafCounter = (cge) =>
			{
				var myProbability = cge.Probability;
				return myProbability;
			};
			Func<LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement,IEnumerable<double>, double> innerCounter = (cge,childProbabilities) =>
			{
				var myProbability = cge.Probability;
				var childProbability = 0.0;
				foreach (var p in childProbabilities)
				{
					childProbability += p;
				}
				return myProbability*childProbability;
			};
			var traverser = ltmdp.GetTreeTraverser(cid);
			var probabilties=traverser.ApplyFuncWithRecursionBasedAlgorithm(innerCounter,leafCounter);

			return probabilties;
		}

		private double SumProbabilitiesOfInitialState(LabeledTransitionMarkovDecisionProcess ltmdp)
		{
			var cidRoot = ltmdp.GetRootContinuationGraphLocationOfInitialState();
			return SumProbabilitiesOfCid(ltmdp, cidRoot);
		}

		private double SumProbabilitiesOfState(LabeledTransitionMarkovDecisionProcess ltmdp, int state)
		{
			var cidRoot = ltmdp.GetRootContinuationGraphLocationOfState(state);
			return SumProbabilitiesOfCid(ltmdp, cidRoot);
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
			t->ActivatedFaults=new FaultSet();
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
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal(ltmdp,AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0,1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);
			Clear();

			// add reflexive state 5
			CreateTransition(false, 5, 0);
			_stepGraph.SetProbabilityOfContinuationId(0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			ltmdp.TransitionTargets.ShouldBe(2);
			ltmdp.SourceStates.Count.ShouldBe(1);
			ltmdp.SourceStates.First().ShouldBe(5);

			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(ltmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp,5);
			transitionTargetsOfState5.ShouldBe(1);
			var probabilitySumOfState5 = SumProbabilitiesOfState(ltmdp, 5);
			probabilitySumOfState5.ShouldBe(1.0);
		}

		[Fact]
		public void ThreeReflexiveStatesFromInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal(ltmdp, AnalysisConfiguration.Default);

			// add initial state
			Clear();
			_stepGraph.ProbabilisticSplit(0,1,3);
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


			ltmdp.TransitionTargets.ShouldBe(6);
			ltmdp.SourceStates.Count.ShouldBe(3);
			ltmdp.SourceStates.First(state => state==5).ShouldBe(5);

			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(3);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(ltmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp, 5);
			transitionTargetsOfState5.ShouldBe(1);
			var probabilitySumOfState5 = SumProbabilitiesOfState(ltmdp, 5);
			probabilitySumOfState5.ShouldBe(1.0);
		}
		
		[Fact]
		public void ThreeReflexiveStatesFromNonInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal(ltmdp, AnalysisConfiguration.Default);

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


			ltmdp.TransitionTargets.ShouldBe(7);
			ltmdp.SourceStates.Count.ShouldBe(4);
			ltmdp.SourceStates.First(state => state == 5).ShouldBe(5);

			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(ltmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp, 5);
			transitionTargetsOfState5.ShouldBe(3);
			var probabilitySumOfState5 = SumProbabilitiesOfState(ltmdp, 5);
			probabilitySumOfState5.ShouldBe(1.0);
		}

		[Fact]
		public void StatesFromNonInitialStateWithMoreDistributions()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal(ltmdp, AnalysisConfiguration.Default);

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
			CreateTransition(false, 1, 1 );
			CreateTransition(false, 7, 4 );
			CreateTransition(false, 2, 5 );
			CreateTransition(false, 1, 6 );
			CreateTransition(false, 7, 3 );
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


			ltmdp.TransitionTargets.ShouldBe(11);
			ltmdp.SourceStates.Count.ShouldBe(4);
			ltmdp.SourceStates.First(state => state == 5).ShouldBe(5);
			
			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(1);
			var initialProbabilitySum = SumProbabilitiesOfInitialState(ltmdp);
			initialProbabilitySum.ShouldBe(1.0);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp, 5);
			transitionTargetsOfState5.ShouldBe(5);
			var probabilitySumOfState5 = SumProbabilitiesOfState(ltmdp,5);
			probabilitySumOfState5.ShouldBe(3.0,0.00000001);
		}


		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplitsWithRemove()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal(ltmdp, AnalysisConfiguration.Default);

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

			ltmdp.TransitionTargets.ShouldBe(5);
			ltmdp.SourceStates.Count.ShouldBe(3);


			// check that the transformed forward node (was 2) points to the correct target (was 4) and not to anything else.
			var initialRootCidLocation = ltmdp.GetRootContinuationGraphLocationOfInitialState();
			var initialTarget = ltmdp.GetContinuationGraphElement(initialRootCidLocation);
			var state5Transformed = ltmdp.GetTransitionTarget((int)initialTarget.To);
			var state5TransformedRootCidLocation = ltmdp.GetRootContinuationGraphLocationOfState(state5Transformed.TargetState);
			var state5TransformedRootCid = ltmdp.GetContinuationGraphElement(state5TransformedRootCidLocation);
			var state5Successors = state5TransformedRootCid.To - state5TransformedRootCid.From + 1;
			state5Successors.ShouldBe(2);
			var state5TransformedFirstSplitCidLocation = state5TransformedRootCid.From;
			var state5TransformedFirstSplitCid = ltmdp.GetContinuationGraphElement(state5TransformedFirstSplitCidLocation);
			var state5TransformedTargetOfForward = state5TransformedFirstSplitCid.From;
			var state5ForwardNode = ltmdp.GetContinuationGraphElement(state5TransformedRootCid.To);
			state5ForwardNode.ChoiceType.ShouldBe(LtmdpChoiceType.Forward);
			state5ForwardNode.To.ShouldBe(state5TransformedTargetOfForward);
		}
	}
}
