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
	using ISSE.SafetyChecking.GenericDataStructures;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public unsafe class LtmdpBuilderTests
	{
		private const int StateCapacity = 1024;
		private const int TransitionCapacity = 4096;
		public TestTraceOutput Output { get; }

		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmdpTransition* _transitions;
		private int _transitionCount = 0;

		public LtmdpBuilderTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
			
			_transitionBuffer.Resize(TransitionCapacity * sizeof(LtmdpTransition), zeroMemory: false);
			_transitions = (LtmdpTransition*)_transitionBuffer.Pointer;
		}

		private void CreateTransition(bool isFormulaSatisfied, int targetStateIndex, int distribution, double p)
		{
			var transition = _transitionCount;
			_transitionCount++;
			_transitions[transition] = new LtmdpTransition { Probability = p, Distribution = distribution };
			var t = (Transition*)(_transitions + transition);
			t->SourceStateIndex = 0;
			t->TargetStateIndex = targetStateIndex;
			t->Formulas = new StateFormulaSet(new Func<bool>[] { () => isFormulaSatisfied });
			t->Flags = TransitionFlags.IsValidFlag | TransitionFlags.IsStateTransformedToIndexFlag;
			t->ActivatedFaults=new FaultSet();
		}

		private TransitionCollection CreateTransitionCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _transitionCount, _transitionCount, sizeof(LtmdpTransition));
		}

		private void ClearTransitions()
		{
			_transitionCount = 0;
		}

		[Fact]
		public void OneReflexiveTransition()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilder<SimpleExecutableModel>(ltmdp);

			// add initial state
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);
			ClearTransitions();

			// add reflexive state 5
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			ltmdp.Transitions.ShouldBe(2);
			ltmdp.SourceStates.Count.ShouldBe(1);
			ltmdp.SourceStates.First().ShouldBe(5);
			var initialDistEnumerator = ltmdp.GetInitialDistributionsEnumerator();
			var distCount = 0;
			var transCount = 0;
			while (initialDistEnumerator.MoveNext())
			{
				distCount++;
				var transEnumerator = initialDistEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					transCount++;
				}
			}
			distCount.ShouldBe(1);
			transCount.ShouldBe(1);
			distCount = 0;
			transCount = 0;
			var state5DistEnumerator = ltmdp.GetDistributionsEnumerator(5);
			while (state5DistEnumerator.MoveNext())
			{
				distCount++;
				var transEnumerator = state5DistEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					transCount++;
				}
			}
			distCount.ShouldBe(1);
			transCount.ShouldBe(1);
			distCount = 0;
			transCount = 0;
		}

		[Fact]
		public void ThreeReflexiveStatesFromInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilder<SimpleExecutableModel>(ltmdp);

			// add initial state
			ClearTransitions();
			CreateTransition(false, 5, 0, 0.3);
			CreateTransition(false, 7, 0, 0.3);
			CreateTransition(false, 2, 0, 0.4);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add reflexive state 5
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);
			
			// add reflexive state 7
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			ClearTransitions();
			CreateTransition(false, 2, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);


			ltmdp.Transitions.ShouldBe(6);
			ltmdp.SourceStates.Count.ShouldBe(3);
			ltmdp.SourceStates.First(state => state==5).ShouldBe(5);
			var initialDistEnumerator = ltmdp.GetInitialDistributionsEnumerator();
			var distCount = 0;
			var transCount = 0;
			while (initialDistEnumerator.MoveNext())
			{
				distCount++;
				var transEnumerator = initialDistEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					transCount++;
				}
			}
			distCount.ShouldBe(1);
			transCount.ShouldBe(3);
			distCount = 0;
			transCount = 0;
			var state5DistEnumerator = ltmdp.GetDistributionsEnumerator(5);
			while (state5DistEnumerator.MoveNext())
			{
				distCount++;
				var transEnumerator = state5DistEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					transCount++;
				}
			}
			distCount.ShouldBe(1);
			transCount.ShouldBe(1);
			distCount = 0;
			transCount = 0;
		}



		[Fact]
		public void ThreeReflexiveStatesFromNonInitialState()
		{
			var ltmdp = new LabeledTransitionMarkovDecisionProcess(StateCapacity, TransitionCapacity);
			var ltmdpBuilder = new LabeledTransitionMarkovDecisionProcess.LtmdpBuilder<SimpleExecutableModel>(ltmdp);

			// add initial state
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add state 5
			ClearTransitions();
			CreateTransition(false, 7, 0, 0.3);
			CreateTransition(false, 2, 0, 0.3);
			CreateTransition(false, 1, 0, 0.4);
			ltmdpBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			ClearTransitions();
			CreateTransition(false, 7, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			ClearTransitions();
			CreateTransition(false, 2, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 1
			ClearTransitions();
			CreateTransition(false, 1, 0, 1.0);
			ltmdpBuilder.ProcessTransitions(null, null, 1, CreateTransitionCollection(), _transitionCount, false);


			ltmdp.Transitions.ShouldBe(7);
			ltmdp.SourceStates.Count.ShouldBe(4);
			ltmdp.SourceStates.First(state => state == 5).ShouldBe(5);

			var initialDistEnumerator = ltmdp.GetInitialDistributionsEnumerator();
			var distCount = 0;
			var transCount = 0;
			while (initialDistEnumerator.MoveNext())
			{
				distCount++;
				var transEnumerator = initialDistEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					transCount++;
				}
			}
			distCount.ShouldBe(1);
			transCount.ShouldBe(1);

			distCount = 0;
			transCount = 0;
			var state5DistEnumerator = ltmdp.GetDistributionsEnumerator(5);
			while (state5DistEnumerator.MoveNext())
			{
				distCount++;
				var transEnumerator = state5DistEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					transCount++;
				}
			}
			distCount.ShouldBe(1);
			transCount.ShouldBe(3);
		}
	}
}
