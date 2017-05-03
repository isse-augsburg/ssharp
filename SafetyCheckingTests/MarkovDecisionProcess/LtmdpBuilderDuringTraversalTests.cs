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

namespace Tests.MarkovDecisionProcess
{
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
			_choiceResolver = new LtmdpChoiceResolver(_stepGraph);
		}
		
		private int CountTargetStatesOfCid(LabeledTransitionMarkovDecisionProcess ltmdp,long cid)
		{
			var targetStatesCount = 0;
			Action<LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement> counter = cge =>
			{
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


		private void CreateTransition(bool isFormulaSatisfied, int targetStateIndex, int continuationId, double p)
		{
			var transition = _transitionCount;
			_transitionCount++;
			_transitions[transition] = new LtmdpTransition { Probability = p, ContinuationId = continuationId };
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
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp,AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);
			Clear();

			// add reflexive state 5
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			ltmdp.TransitionTargets.ShouldBe(2);
			ltmdp.SourceStates.Count.ShouldBe(1);
			ltmdp.SourceStates.First().ShouldBe(5);
			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(1);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp,5);
			transitionTargetsOfState5.ShouldBe(1);
		}

		[Fact]
		public void ThreeReflexiveStatesFromInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp, AnalysisConfiguration.Default);

			// add initial state
			Clear();
			_stepGraph.ProbabilisticSplit(0,1,3);
			CreateTransition(false, 5, 1, 0.3);
			CreateTransition(false, 7, 2, 0.3);
			CreateTransition(false, 2, 3, 0.4);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add reflexive state 5
			Clear();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			Clear();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			Clear();
			CreateTransition(false, 2, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);


			ltmdp.TransitionTargets.ShouldBe(6);
			ltmdp.SourceStates.Count.ShouldBe(3);
			ltmdp.SourceStates.First(state => state==5).ShouldBe(5);

			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(3);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp, 5);
			transitionTargetsOfState5.ShouldBe(1);
		}
		
		[Fact]
		public void ThreeReflexiveStatesFromNonInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp, AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add state 5
			Clear();
			_stepGraph.ProbabilisticSplit(0, 1, 3);
			CreateTransition(false, 7, 1, 0.3);
			CreateTransition(false, 2, 2, 0.3);
			CreateTransition(false, 1, 3, 0.4);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			Clear();
			CreateTransition(false, 7, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			Clear();
			CreateTransition(false, 2, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 1
			Clear();
			CreateTransition(false, 1, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 1, CreateTransitionCollection(), _transitionCount, false);


			ltmdp.TransitionTargets.ShouldBe(7);
			ltmdp.SourceStates.Count.ShouldBe(4);
			ltmdp.SourceStates.First(state => state == 5).ShouldBe(5);

			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(1);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp, 5);
			transitionTargetsOfState5.ShouldBe(3);
		}

		[Fact]
		public void StatesFromNonInitialStateWithMoreDistributions()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal<SimpleExecutableModel>(ltmdp, AnalysisConfiguration.Default);

			// add initial state
			Clear();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add state 5
			Clear();
			_stepGraph.NonDeterministicSplit(0, 1, 3);
			_stepGraph.ProbabilisticSplit(2, 4, 6);
			CreateTransition(false, 1, 1, 1.0);
			CreateTransition(false, 7, 4, 0.3);
			CreateTransition(false, 2, 5, 0.3);
			CreateTransition(false, 1, 6, 0.4);
			CreateTransition(false, 7, 3, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			var transitionTargetsOfState51 = CountTargetStatesOfState(ltmdp, 5);
			transitionTargetsOfState51.ShouldBe(5);

			// add reflexive state 7
			Clear();
			_stepGraph.NonDeterministicSplit(0, 1, 2);
			_stepGraph.ProbabilisticSplit(1, 3, 4);
			CreateTransition(false, 7, 3, 0.2);
			CreateTransition(false, 2, 4, 0.8);
			CreateTransition(false, 1, 2, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);



			// add reflexive state 2
			Clear();
			CreateTransition(false, 2, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 1
			Clear();
			CreateTransition(false, 1, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 1, CreateTransitionCollection(), _transitionCount, false);


			ltmdp.TransitionTargets.ShouldBe(11);
			ltmdp.SourceStates.Count.ShouldBe(4);
			ltmdp.SourceStates.First(state => state == 5).ShouldBe(5);
			
			var initialTransitionTargets = CountTargetStatesOfInitialState(ltmdp);
			initialTransitionTargets.ShouldBe(1);

			var transitionTargetsOfState5 = CountTargetStatesOfState(ltmdp, 5);
			transitionTargetsOfState5.ShouldBe(5);
		}
	}
}
