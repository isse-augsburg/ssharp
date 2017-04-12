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

namespace Tests.DiscreteTimeMarkovChain
{
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.GenericDataStructures;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public unsafe class LtmcBuilderTests
	{
		private const int StateCapacity = 1024;
		private const int TransitionCapacity = 4096;
		public TestTraceOutput Output { get; }

		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmcTransition* _transitions;
		private int _transitionCount = 0;

		public LtmcBuilderTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
			
			_transitionBuffer.Resize(TransitionCapacity * sizeof(LtmcTransition), zeroMemory: false);
			_transitions = (LtmcTransition*)_transitionBuffer.Pointer;
		}

		private void CreateTransition(bool isFormulaSatisfied, int targetStateIndex, int distribution, double p)
		{
			var transition = _transitionCount;
			_transitionCount++;
			_transitions[transition] = new LtmcTransition { Probability = p };
			var t = (Transition*)(_transitions + transition);
			t->SourceStateIndex = 0;
			t->TargetStateIndex = targetStateIndex;
			t->Formulas = new StateFormulaSet(new Func<bool>[] { () => isFormulaSatisfied });
			t->Flags = TransitionFlags.IsValidFlag | TransitionFlags.IsStateTransformedToIndexFlag;
			t->ActivatedFaults=new FaultSet();
		}

		private TransitionCollection CreateTransitionCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _transitionCount, _transitionCount, sizeof(LtmcTransition));
		}

		private void ClearTransitions()
		{
			_transitionCount = 0;
		}

		[Fact]
		public void OneReflexiveTransition()
		{
			var ltmc = new LabeledTransitionMarkovChain(StateCapacity, TransitionCapacity);
			var ltmcBuilder = new LabeledTransitionMarkovChain.LtmcBuilder<SimpleExecutableModel>(ltmc);

			// add initial state
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);
			ClearTransitions();

			// add reflexive state 5
			CreateTransition(false, 5, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			ltmc.Transitions.ShouldBe(2);
			ltmc.SourceStates.Count.ShouldBe(1);
			ltmc.SourceStates.First().ShouldBe(5);
			var transCount = 0;
			var transEnumerator = ltmc.GetInitialDistributionEnumerator();
			while (transEnumerator.MoveNext())
			{
				transCount++;
			}
			transCount.ShouldBe(1);

			transCount = 0;
			transEnumerator = ltmc.GetTransitionEnumerator(5);
			while (transEnumerator.MoveNext())
			{
				transCount++;
			}
			transCount.ShouldBe(1);
		}

		[Fact]
		public void ThreeReflexiveStatesFromInitialState()
		{
			var ltmc = new LabeledTransitionMarkovChain(StateCapacity, TransitionCapacity);
			var ltmcBuilder = new LabeledTransitionMarkovChain.LtmcBuilder<SimpleExecutableModel>(ltmc);

			// add initial state
			ClearTransitions();
			CreateTransition(false, 5, 0, 0.3);
			CreateTransition(false, 7, 0, 0.3);
			CreateTransition(false, 2, 0, 0.4);
			ltmcBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add reflexive state 5
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);
			
			// add reflexive state 7
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			ClearTransitions();
			CreateTransition(false, 2, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);


			ltmc.Transitions.ShouldBe(6);
			ltmc.SourceStates.Count.ShouldBe(3);
			ltmc.SourceStates.First(state => state==5).ShouldBe(5);
			var transEnumerator = ltmc.GetInitialDistributionEnumerator();
			var transCount = 0;
			while (transEnumerator.MoveNext())
			{
				transCount++;
			}
			transCount.ShouldBe(3);

			transCount = 0;
			transEnumerator = ltmc.GetTransitionEnumerator(5);
			while (transEnumerator.MoveNext())
			{
				transCount++;
			}
			transCount.ShouldBe(1);
		}

		[Fact]
		public void ThreeReflexiveStatesFromNonInitialState()
		{
			var ltmc = new LabeledTransitionMarkovChain(StateCapacity, TransitionCapacity);
			var ltmcBuilder = new LabeledTransitionMarkovChain.LtmcBuilder<SimpleExecutableModel>(ltmc);

			// add initial state
			ClearTransitions();
			CreateTransition(false, 5, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 0, CreateTransitionCollection(), _transitionCount, true);

			// add state 5
			ClearTransitions();
			CreateTransition(false, 7, 0, 0.3);
			CreateTransition(false, 2, 0, 0.3);
			CreateTransition(false, 1, 0, 0.4);
			ltmcBuilder.ProcessTransitions(null, null, 5, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 7
			ClearTransitions();
			CreateTransition(false, 7, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 7, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 2
			ClearTransitions();
			CreateTransition(false, 2, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 2, CreateTransitionCollection(), _transitionCount, false);

			// add reflexive state 1
			ClearTransitions();
			CreateTransition(false, 1, 0, 1.0);
			ltmcBuilder.ProcessTransitions(null, null, 1, CreateTransitionCollection(), _transitionCount, false);


			ltmc.Transitions.ShouldBe(7);
			ltmc.SourceStates.Count.ShouldBe(4);
			ltmc.SourceStates.First(state => state == 5).ShouldBe(5);

			var transEnumerator = ltmc.GetInitialDistributionEnumerator();
			var transCount = 0;
			while (transEnumerator.MoveNext())
			{
				transCount++;
			}
			transCount.ShouldBe(1);
			
			transCount = 0;
			transEnumerator = ltmc.GetTransitionEnumerator(5);
			while (transEnumerator.MoveNext())
			{
				transCount++;
			}
			transCount.ShouldBe(3);
		}
	}
}
