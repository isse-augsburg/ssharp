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

namespace Tests.MarkovDecisionProcess
{
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using System;
	using System.Collections.Generic;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.GenericDataStructures;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class NmdpToMdpTests
	{
		public TestTraceOutput Output { get; }

		public NestedMarkovDecisionProcess NestedMarkovDecisionProcess { get; private set; }

		public MarkovDecisionProcess MarkovDecisionProcess { get; private set; }
		
		public NmdpToMdpTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		private void CountDistributionsAndTransitionsOfInitialState(out int distributions, out int transitions)
		{
			var enumerator = MarkovDecisionProcess.GetEnumerator();
			enumerator.SelectInitialDistributions();
			distributions = 0;
			transitions = 0;
			while (enumerator.MoveNextDistribution())
			{
				distributions++;
				while (enumerator.MoveNextTransition())
				{
					transitions++;
				}
			}
		}

		private void CountDistributionsAndTransitionsOfState(int state, out int distributions, out int transitions)
		{
			var enumerator = MarkovDecisionProcess.GetEnumerator();
			enumerator.SelectSourceState(state);
			distributions = 0;
			transitions = 0;
			while (enumerator.MoveNextDistribution())
			{
				distributions++;
				while (enumerator.MoveNextTransition())
				{
					transitions++;
				}
			}
		}

		[Fact]
		public void ExampleNoChoices()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleNoChoices.Create();
			
			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0,out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
		}

		[Fact]
		public void ExampleOneInitialProbabilisticSplit()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleOneInitialProbabilisticSplit.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(3);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
		}
		
		[Fact]
		public void ExampleTwoInitialProbabilisticSplits()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleTwoInitialProbabilisticSplits.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(3);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
		}

		[Fact]
		public void ExampleTwoInitialSplitsNondeterministicThenProbabilistic()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleTwoInitialSplitsNondeterministicThenProbabilistic.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(2);
			initialTransitions.ShouldBe(3);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
		}

		[Fact]
		public void ExampleTwoInitialSplitsProbabilisticThenNondeterministic()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleTwoInitialSplitsProbabilisticThenNondeterministic.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(2);
			initialTransitions.ShouldBe(4);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
		}
		
		[Fact]
		public void ExampleOneStateProbabilisticSplit()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleOneStateProbabilisticSplit.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(3);
		}

		[Fact]
		public void ExampleTwoStateProbabilisticSplits()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleTwoStateProbabilisticSplits.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(3);
		}

		[Fact]
		public void ExampleTwoStateSplitsNondeterministicThenProbabilistic()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleTwoStateSplitsNondeterministicThenProbabilistic.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(2);
			state0Transitions.ShouldBe(3);
		}

		[Fact]
		public void ExampleTwoStateSplitsProbabilisticThenNondeterministic()
		{
			NestedMarkovDecisionProcess = DataStructures.NestedMarkovDecisionProcessExamples.ExampleTwoStateSplitsProbabilisticThenNondeterministic.Create();

			var converter = new NmdpToMdp(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			CountDistributionsAndTransitionsOfInitialState(out initialDistributions, out initialTransitions);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);

			int state0Distributions;
			int state0Transitions;
			CountDistributionsAndTransitionsOfState(0, out state0Distributions, out state0Transitions);
			state0Distributions.ShouldBe(2);
			state0Transitions.ShouldBe(4);
		}
	}
}
