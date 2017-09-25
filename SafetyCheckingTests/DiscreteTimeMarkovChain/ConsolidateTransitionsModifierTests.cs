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
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public unsafe class LtmcTestBuilderWithStatesAsByteVector
	{
		private const int StateCapacity = 1024;
		private const int TransitionCapacity = 4096;

		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly LtmcTransition* _transitions;

		public readonly TemporaryStateStorage TemporaryStateStorage;
		private int _transitionCount = 0;

		
		public LtmcTestBuilderWithStatesAsByteVector()
		{
			_transitionBuffer.Resize(TransitionCapacity * sizeof(LtmcTransition), zeroMemory: false);
			_transitions = (LtmcTransition*)_transitionBuffer.Pointer;
			TemporaryStateStorage = new TemporaryStateStorage(sizeof(int), StateCapacity);
		}

		public byte* CreateState(int stateContent)
		{
			var position = TemporaryStateStorage.GetFreeTemporalSpaceAddress();
			var positionAsInt = (int*) position;
			*positionAsInt = stateContent;
			return position;
		}

		internal void CreateTransition(bool isFormulaSatisfied, byte* targetState, double p)
		{
			var transition = _transitionCount;
			_transitionCount++;
			_transitions[transition] = new LtmcTransition { Probability = p };
			var t = _transitions + transition;
			t->TargetStatePointer = targetState;
			t->Formulas = new StateFormulaSet(new Func<bool>[] { () => isFormulaSatisfied });
			t->Flags = TransitionFlags.IsValidFlag | TransitionFlags.IsStateTransformedToIndexFlag;
			t->ActivatedFaults = new FaultSet();
		}

		internal void CreateTransition(bool[] isFormulaSatisfied, byte* targetState, double p)
		{
			var transition = _transitionCount;
			_transitionCount++;
			_transitions[transition] = new LtmcTransition { Probability = p };
			var t = _transitions + transition;
			t->TargetStatePointer = targetState;
			t->Formulas = new StateFormulaSet(isFormulaSatisfied);
			t->Flags = TransitionFlags.IsValidFlag | TransitionFlags.IsStateTransformedToIndexFlag;
			t->ActivatedFaults = new FaultSet();
		}

		internal TransitionCollection CreateTransitionCollection()
		{
			return new TransitionCollection((Transition*)_transitions, _transitionCount, _transitionCount, sizeof(LtmcTransition));
		}

		internal void Clear()
		{
			TemporaryStateStorage.Clear();
			_transitionCount = 0;
		}
	}


	public unsafe class ConsolidateTransitionsModifierTests
	{
		public TestTraceOutput Output { get; }

		public LtmcTestBuilderWithStatesAsByteVector LtmcTestBuilder { get; } = new LtmcTestBuilderWithStatesAsByteVector();

		internal ConsolidateTransitionsModifier ConsolidateTransitionsModifier = new ConsolidateTransitionsModifier {RelevantStateVectorSize = sizeof(int) };

		public ConsolidateTransitionsModifierTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		private void TransitionsShouldSumUpToOne(TransitionCollection transitions)
		{
			var sum = 0.0;
			foreach (LtmcTransition* transition in transitions)
			{
				sum += transition->Probability;
			}
			Xunit.Assert.Equal(1.0, sum, 10);
		}

		private int CountValid(TransitionCollection transitions)
		{
			var valid = 0;
			foreach (var transition in transitions)
			{
				valid++;
			}
			return valid;
		}

		[Fact]
		public void OneTransitionCollectionWithSeveralSameFormulasWithoutHash()
		{
			ConsolidateTransitionsModifier.UseHashLimit = int.MaxValue;
			LtmcTestBuilder.Clear();
			var state1A = LtmcTestBuilder.CreateState(1);
			var state555 = LtmcTestBuilder.CreateState(555);
			var state1B = LtmcTestBuilder.CreateState(1);
			var state7 = LtmcTestBuilder.CreateState(7);

			LtmcTestBuilder.CreateTransition(false, state1A, 0.01);
			LtmcTestBuilder.CreateTransition(false, state555, 0.02);
			LtmcTestBuilder.CreateTransition(false, state7, 0.03);
			LtmcTestBuilder.CreateTransition(false, state1A, 0.04);
			LtmcTestBuilder.CreateTransition(true, state7, 0.10);
			LtmcTestBuilder.CreateTransition(false, state1B, 0.80);

			var collection = LtmcTestBuilder.CreateTransitionCollection();
			TransitionsShouldSumUpToOne(collection);
			collection.Count.ShouldBe(6);
			CountValid(collection).ShouldBe(6);

			ConsolidateTransitionsModifier.ModifyTransitions(null, null, collection, null, 0, true);
			TransitionsShouldSumUpToOne(collection);
			collection.Count.ShouldBe(6);
			CountValid(collection).ShouldBe(4);
		}

		[Fact]
		public void OneTransitionCollectionWithSeveralSameFormulasWithHash()
		{
			ConsolidateTransitionsModifier.UseHashLimit = 0;
			LtmcTestBuilder.Clear();
			var state1A = LtmcTestBuilder.CreateState(1);
			var state555 = LtmcTestBuilder.CreateState(555);
			var state1B = LtmcTestBuilder.CreateState(1);
			var state7 = LtmcTestBuilder.CreateState(7);

			LtmcTestBuilder.CreateTransition(false, state1A, 0.01);
			LtmcTestBuilder.CreateTransition(false, state555, 0.02);
			LtmcTestBuilder.CreateTransition(false, state7, 0.03);
			LtmcTestBuilder.CreateTransition(false, state1A, 0.04);
			LtmcTestBuilder.CreateTransition(true, state7, 0.10);
			LtmcTestBuilder.CreateTransition(false, state1B, 0.80);

			var collection = LtmcTestBuilder.CreateTransitionCollection();
			TransitionsShouldSumUpToOne(collection);
			collection.Count.ShouldBe(6);
			CountValid(collection).ShouldBe(6);

			ConsolidateTransitionsModifier.ModifyTransitions(null, null, collection, null, 0, true);
			TransitionsShouldSumUpToOne(collection);
			collection.Count.ShouldBe(6);
			CountValid(collection).ShouldBe(4);
		}
	}
}
