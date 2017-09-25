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
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.GenericDataStructures;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using NmdpExamples;

	public class NmdpToMdpByNewStatesWithConstantDistanceTests
	{
		public TestTraceOutput Output { get; }

		public NestedMarkovDecisionProcess NestedMarkovDecisionProcess { get; private set; }

		public MarkovDecisionProcess MarkovDecisionProcess { get; private set; }
		
		public NmdpToMdpByNewStatesWithConstantDistanceTests(ITestOutputHelper output)
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

		private void CalculateMinAndMaxDistanceToRealState(bool starting, int currentState, out int minDistance, out int maxDistance, int artificialFormulaIndex)
		{
			var currentIsArtificial = MarkovDecisionProcess.StateLabeling[currentState][artificialFormulaIndex];
			if (!currentIsArtificial && !starting)
			{
				minDistance = 0;
				maxDistance = 0;
				return;
			}
			var enumerator = MarkovDecisionProcess.GetEnumerator();
			enumerator.SelectSourceState(currentState);
			minDistance = int.MaxValue;
			maxDistance = int.MinValue;
			while (enumerator.MoveNextDistribution())
			{
				while (enumerator.MoveNextTransition())
				{
					var childState = enumerator.CurrentTransition.Column;
					int childMinDistance;
					int childMaxDistance;
					CalculateMinAndMaxDistanceToRealState(false, childState, out childMinDistance, out childMaxDistance, artificialFormulaIndex);
					minDistance = Math.Min(minDistance, childMinDistance);
					maxDistance = Math.Max(maxDistance, childMaxDistance);
				}
			}
		}
		
		private void AssertDistanceIsEqual(Formula artificialFormulaI)
		{
			var artificialFormulaIndex = Array.IndexOf(MarkovDecisionProcess.StateFormulaLabels, artificialFormulaI.Label);

			var requiredDistance = 0;
			var isSet = false;

			for (var i = 0; i < MarkovDecisionProcess.States; i++)
			{
				var currentIsArtificial = MarkovDecisionProcess.StateLabeling[i][artificialFormulaIndex];
				if (!currentIsArtificial)
				{
					int minDistance;
					int maxDistance;
					CalculateMinAndMaxDistanceToRealState(true, i, out minDistance, out maxDistance, artificialFormulaIndex);
					if (!isSet)
					{
						isSet = true;
						requiredDistance = minDistance;
					}
					minDistance.ShouldBe(requiredDistance);
					maxDistance.ShouldBe(requiredDistance);
				}
			}
		}


		[Fact]
		public void ExampleNoChoices()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleNoChoices.Create();
			
			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
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

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}

		[Fact]
		public void ExampleNoChoices2()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleNoChoices2.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
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
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}

		[Fact]
		public void ExampleOneInitialProbabilisticSplit()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleOneInitialProbabilisticSplit.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
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

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}
		
		[Fact]
		public void ExampleTwoInitialProbabilisticSplits()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoInitialProbabilisticSplits.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(2);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}

		[Fact]
		public void ExampleTwoInitialSplitsNondeterministicThenProbabilistic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoInitialSplitsNondeterministicThenProbabilistic.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(2);
			initialTransitions.ShouldBe(2);
			initialProbabilities.ShouldBe(2.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}

		[Fact]
		public void ExampleTwoInitialSplitsProbabilisticThenNondeterministic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoInitialSplitsProbabilisticThenNondeterministic.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
			MarkovDecisionProcess = converter.MarkovDecisionProcess;

			int initialDistributions;
			int initialTransitions;
			double initialProbabilities;
			CalculateMetricsOfInitialState(out initialDistributions, out initialTransitions, out initialProbabilities);
			initialDistributions.ShouldBe(1);
			initialTransitions.ShouldBe(2);
			initialProbabilities.ShouldBe(1.0, 0.0000001);

			int state0Distributions;
			int state0Transitions;
			double state0Probabilities;
			CalculateMetricsOfState(0, out state0Distributions, out state0Transitions, out state0Probabilities);
			state0Distributions.ShouldBe(1);
			state0Transitions.ShouldBe(1);
			state0Probabilities.ShouldBe(1.0, 0.0000001);

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}
		
		[Fact]
		public void ExampleOneStateProbabilisticSplit()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleOneStateProbabilisticSplit.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
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

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}

		[Fact]
		public void ExampleTwoStateProbabilisticSplits()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoStateProbabilisticSplits.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
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
			state0Transitions.ShouldBe(2);
			state0Probabilities.ShouldBe(1.0, 0.0000001);

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}

		[Fact]
		public void ExampleTwoStateSplitsNondeterministicThenProbabilistic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoStateSplitsNondeterministicThenProbabilistic.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
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
			state0Transitions.ShouldBe(2);
			state0Probabilities.ShouldBe(2.0, 0.0000001);

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}

		[Fact]
		public void ExampleTwoStateSplitsProbabilisticThenNondeterministic()
		{
			NestedMarkovDecisionProcess = NmdpExamples.ExampleTwoStateSplitsProbabilisticThenNondeterministic.Create();

			var converter = new NmdpToMdpByNewStates(NestedMarkovDecisionProcess, Output.TextWriterAdapter(), true);
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
			state0Transitions.ShouldBe(2);
			state0Probabilities.ShouldBe(1.0, 0.0000001);

			AssertDistanceIsEqual(converter.FormulaForArtificalState);
		}
	}
}
