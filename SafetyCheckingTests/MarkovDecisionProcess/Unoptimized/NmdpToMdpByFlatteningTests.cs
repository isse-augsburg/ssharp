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

namespace Tests.MarkovDecisionProcess.Unoptimized
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
	using NmdpExamples;

	public class NmdpToMdpByFlatteningTests
	{
		public TestTraceOutput Output { get; }

		public NestedMarkovDecisionProcess NestedMarkovDecisionProcess { get; private set; }

		public MarkovDecisionProcess MarkovDecisionProcess { get; private set; }
		
		public NmdpToMdpByFlatteningTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		private void CalculateMetricsOfInitialState(out int distributions, out int transitions, out double summedProbability)
		{
			var enumerator = MarkovDecisionProcess.GetEnumerator();
			enumerator.SelectInitialDistributions();
			distributions = 0;
			transitions = 0;
			summedProbability = 0.0;
			while (enumerator.MoveNextDistribution())
			{
				distributions++;
				while (enumerator.MoveNextTransition())
				{
					transitions++;
					summedProbability += enumerator.CurrentTransition.Value;
				}
			}
		}

		private void CalculateMetricsOfState(int state, out int distributions, out int transitions, out double summedProbability)
		{
			var enumerator = MarkovDecisionProcess.GetEnumerator();
			enumerator.SelectSourceState(state);
			distributions = 0;
			transitions = 0;
			summedProbability = 0.0;
			while (enumerator.MoveNextDistribution())
			{
				distributions++;
				while (enumerator.MoveNextTransition())
				{
					transitions++;
					summedProbability += enumerator.CurrentTransition.Value;
				}
			}
		}

		[Fact]
		public void ExampleNoChoices()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleNoChoices.Create();
			
			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0,out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);
		}

		[Fact]
		public void ExampleOneInitialProbabilisticSplit()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleOneInitialProbabilisticSplit.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(3);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);
		}
		
		[Fact]
		public void ExampleTwoInitialProbabilisticSplits()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoInitialProbabilisticSplits.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(3);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);
		}

		[Fact]
		public void ExampleTwoInitialSplitsNondeterministicThenProbabilistic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoInitialSplitsNondeterministicThenProbabilistic.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(2);
			initialTransitions.ShouldBe(3);
			initialProbabilities.ShouldBe(2.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);
		}

		[Fact]
		public void ExampleTwoInitialSplitsProbabilisticThenNondeterministic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoInitialSplitsProbabilisticThenNondeterministic.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(2);
			initialTransitions.ShouldBe(4);
			initialProbabilities.ShouldBe(2.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);
		}
		
		[Fact]
		public void ExampleOneStateProbabilisticSplit()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleOneStateProbabilisticSplit.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(3);
			state0Probabilities.ShouldBe(1.0, 0.0000001);
		}

		[Fact]
		public void ExampleTwoStateProbabilisticSplits()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoStateProbabilisticSplits.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(3);
			state0Probabilities.ShouldBe(1.0, 0.0000001);
		}

		[Fact]
		public void ExampleTwoStateSplitsNondeterministicThenProbabilistic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoStateSplitsNondeterministicThenProbabilistic.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(2);
			state0Transitions.ShouldBe(3);
			state0Probabilities.ShouldBe(2.0, 0.0000001);
		}

		[Fact]
		public void ExampleTwoStateSplitsProbabilisticThenNondeterministic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoStateSplitsProbabilisticThenNondeterministic.Create();

			var converter = new NmdpToMdpByFlattening(NestedMarkovDecisionProcess);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(1);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(2);
			state0Transitions.ShouldBe(4);
			state0Probabilities.ShouldBe(2.0, 0.0000001);
		}
	}
}
