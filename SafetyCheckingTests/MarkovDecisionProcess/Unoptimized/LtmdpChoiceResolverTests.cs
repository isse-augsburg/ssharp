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
	using System.Collections.Generic;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.GenericDataStructures;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using SimpleExecutableModel;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class LtmdpChoiceResolverTests
	{
		public TestTraceOutput Output { get; }


		public LtmdpChoiceResolverTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
			_stepGraph = new LtmdpStepGraph();
			_choiceResolver = new LtmdpChoiceResolver(_stepGraph,true);
		}

		private readonly LtmdpStepGraph _stepGraph;
		private readonly LtmdpChoiceResolver _choiceResolver;

		private int CountEntries (int cid)
		{
			var enumerator = _stepGraph.GetDirectChildrenEnumerator(cid);
			var entries = 0;
			while (enumerator.MoveNext())
			{
				entries++;
			}
			return entries;
		}
 

		[Fact]
		public void NoSplit()
		{
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var path1Exists = _choiceResolver.PrepareNextPath();
			var path2Exists = _choiceResolver.PrepareNextPath();

			var entries = CountEntries(0);
			path1Exists.ShouldBe(true);
			path2Exists.ShouldBe(false);
			entries.ShouldBe(0);
		}
		
		[Fact]
		public void OneSplitInTwo()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(2);

			var entries = CountEntries(0);
			entries.ShouldBe(2);
			entries = CountEntries(1);
			entries.ShouldBe(0);
			entries = CountEntries(2);
			entries.ShouldBe(0);
		}
		
		[Fact]
		public void OneSplitInThree()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);

			var entries = CountEntries(0);
			entries.ShouldBe(3);
			entries = CountEntries(1);
			entries.ShouldBe(0);
			entries = CountEntries(2);
			entries.ShouldBe(0);
			entries = CountEntries(3);
			entries.ShouldBe(0);
		}



		[Fact]
		public void TwoSplits()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.HandleProbabilisticChoice(2);
			
			var entries = CountEntries(0);
			entries.ShouldBe(3);
			entries = CountEntries(1);
			entries.ShouldBe(2);
			entries = CountEntries(2);
			entries.ShouldBe(0);
			entries = CountEntries(3);
			entries.ShouldBe(0);
			entries = CountEntries(4);
			entries.ShouldBe(0);
			entries = CountEntries(5);
			entries.ShouldBe(0);
		}

		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplits()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.HandleChoice(2);

			var entries = CountEntries(0);
			entries.ShouldBe(3);
			entries = CountEntries(1);
			entries.ShouldBe(2);
			entries = CountEntries(2);
			entries.ShouldBe(0);
			entries = CountEntries(3);
			entries.ShouldBe(0);
			entries = CountEntries(4);
			entries.ShouldBe(0);
			entries = CountEntries(5);
			entries.ShouldBe(0);
		}
		
		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplitsWithRemove()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			var choiceToMakeDeterministic = _choiceResolver.LastChoiceIndex;
			_choiceResolver.HandleChoice(2);

			// MakeChoiceAtIndexDeterministic of probabilistic split
			var cidToForwardTo = _choiceResolver.GetContinuationId();
			_choiceResolver.ForwardUntakenChoicesAtIndex(choiceToMakeDeterministic);
			var choiceOfCid0 = _stepGraph.GetChoiceOfCid(0);
			var choiceOfCid1 = _stepGraph.GetChoiceOfCid(1);
			var choiceOfCid2 = _stepGraph.GetChoiceOfCid(2);

			var entries = CountEntries(0);
			entries.ShouldBe(2);
			entries = CountEntries(1);
			entries.ShouldBe(2);
			entries = CountEntries(4);
			entries.ShouldBe(0);
			entries = CountEntries(5);
			entries.ShouldBe(0);
			entries = CountEntries(2);
			entries.ShouldBe(1);
			cidToForwardTo.ShouldBe(4);
			choiceOfCid0.ChoiceType.ShouldBe(LtmdpChoiceType.Probabilitstic);
			choiceOfCid0.From.ShouldBe(1);
			choiceOfCid0.To.ShouldBe(2);
			choiceOfCid0.Probability.ShouldBe(1.0);
			choiceOfCid1.ChoiceType.ShouldBe(LtmdpChoiceType.Nondeterministic);
			choiceOfCid1.From.ShouldBe(4);
			choiceOfCid1.To.ShouldBe(5);
			choiceOfCid1.Probability.ShouldBe(1.0/3);
			choiceOfCid2.ChoiceType.ShouldBe(LtmdpChoiceType.Forward);
			choiceOfCid2.To.ShouldBe(4);
			choiceOfCid2.Probability.ShouldBe(1.0 - (1.0 / 3));
		}

		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplitsWithRemove2()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.HandleChoice(2);
			var choiceToMakeDeterministic = _choiceResolver.LastChoiceIndex;

			// MakeChoiceAtIndexDeterministic of nondeterministic split
			var cidToForwardTo = _choiceResolver.GetContinuationId();
			_choiceResolver.ForwardUntakenChoicesAtIndex(choiceToMakeDeterministic);
			var choiceOfCid1 = _stepGraph.GetChoiceOfCid(1);
			var choiceOfCid4 = _stepGraph.GetChoiceOfCid(4);
			var choiceOfCid5 = _stepGraph.GetChoiceOfCid(5);

			var entries = CountEntries(0);
			entries.ShouldBe(3);
			entries = CountEntries(1);
			entries.ShouldBe(2);
			entries = CountEntries(2);
			entries.ShouldBe(0);
			entries = CountEntries(3);
			entries.ShouldBe(0);
			entries = CountEntries(4);
			entries.ShouldBe(0);
			entries = CountEntries(5);
			entries.ShouldBe(1);
			cidToForwardTo.ShouldBe(4);
			choiceOfCid1.ChoiceType.ShouldBe(LtmdpChoiceType.Nondeterministic);
			choiceOfCid1.From.ShouldBe(4);
			choiceOfCid1.To.ShouldBe(5);
			choiceOfCid1.Probability.ShouldBe(1.0/3);
			choiceOfCid4.ChoiceType.ShouldBe(LtmdpChoiceType.UnsplitOrFinal);
			choiceOfCid4.Probability.ShouldBe(1.0);
			choiceOfCid5.ChoiceType.ShouldBe(LtmdpChoiceType.Forward);
			choiceOfCid5.To.ShouldBe(4);
			choiceOfCid5.Probability.ShouldBe(1.0);
		}



		[Fact]
		public void MakeProabilisticChoiceDeterministicWhichWasPreviouslyNondeterministic()
		{
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleChoice(2);
			_choiceResolver.HandleChoice(2);
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleChoice(2);
			_choiceResolver.HandleChoice(2);
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleChoice(2);
			_choiceResolver.HandleProbabilisticChoice(2);
			var choiceToMakeDeterministic = _choiceResolver.LastChoiceIndex;
			var cidToForwardTo = _choiceResolver.GetContinuationId();
			_choiceResolver.ForwardUntakenChoicesAtIndex(choiceToMakeDeterministic);
			var choiceOfCid6 = _stepGraph.GetChoiceOfCid(6);
			_choiceResolver.PrepareNextPath();

			var entries = CountEntries(0);
			entries.ShouldBe(2);
			entries = CountEntries(1);
			entries.ShouldBe(2);
			entries = CountEntries(2);
			entries.ShouldBe(2);
			entries = CountEntries(3);
			entries.ShouldBe(0);
			entries = CountEntries(4);
			entries.ShouldBe(0);
			entries = CountEntries(5);
			entries.ShouldBe(0);
			entries = CountEntries(6);
			entries.ShouldBe(1);
			choiceOfCid6.ChoiceType.ShouldBe(LtmdpChoiceType.Forward);
			choiceOfCid6.To.ShouldBe(cidToForwardTo);
		}

		[Fact]
		public void SimpleExample1b()
		{
			// Act
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var state1Path1Exists = _choiceResolver.PrepareNextPath();
			var state1Path1Choice1 = _choiceResolver.HandleProbabilisticChoice(2);
			_choiceResolver.SetProbabilityOfLastChoice(new Probability(0.6));
			var state1Path1Choice2 = _choiceResolver.HandleChoice(2);
			var state1Path2Exists = _choiceResolver.PrepareNextPath();
			var state1Path2Choice1 = _choiceResolver.HandleProbabilisticChoice(2);
			var state1Path2Choice2 = _choiceResolver.HandleChoice(2);
			var state1Path3Exists = _choiceResolver.PrepareNextPath();
			var state1Path3Choice1 = _choiceResolver.HandleProbabilisticChoice(2);
			_choiceResolver.SetProbabilityOfLastChoice(new Probability(0.4));
			var state1Path4Exists = _choiceResolver.PrepareNextPath();
			// Collect data
			var state1EntriesOfCid0 = CountEntries(0);
			var state1EntriesOfCid1 = CountEntries(1);
			var state1EntriesOfCid2 = CountEntries(2);
			var state1EntriesOfCid3 = CountEntries(3);
			var state1EntriesOfCid4 = CountEntries(4);
			var state1POfCid0 = _stepGraph.GetProbabilityOfContinuationId(0);
			var state1POfCid1 = _stepGraph.GetProbabilityOfContinuationId(1);
			var state1POfCid2 = _stepGraph.GetProbabilityOfContinuationId(2);

			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var state2Path1Exists = _choiceResolver.PrepareNextPath();
			var state2Path2Exists = _choiceResolver.PrepareNextPath();
			
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var state3Path1Exists = _choiceResolver.PrepareNextPath();
			var state3Path2Exists = _choiceResolver.PrepareNextPath();

			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var state4Path1Exists = _choiceResolver.PrepareNextPath();
			var state4Path2Exists = _choiceResolver.PrepareNextPath();
			// Collect data
			var state4EntriesOfCid0 = CountEntries(0);


			// Assert
			state1Path1Exists.ShouldBe(true);
			state1Path1Choice1.ShouldBe(0);
			state1Path1Choice2.ShouldBe(0);
			state1Path2Exists.ShouldBe(true);
			state1Path2Choice1.ShouldBe(0);
			state1Path2Choice2.ShouldBe(1);
			state1Path3Exists.ShouldBe(true);
			state1Path3Choice1.ShouldBe(1);
			state1Path4Exists.ShouldBe(false);
			state1EntriesOfCid0.ShouldBe(2);
			state1EntriesOfCid1.ShouldBe(2);
			state1EntriesOfCid2.ShouldBe(0);
			state1EntriesOfCid3.ShouldBe(0);
			state1EntriesOfCid4.ShouldBe(0);
			state1POfCid0.ShouldBe(1.0);
			state1POfCid1.ShouldBe(0.6);
			state1POfCid2.ShouldBe(0.4);

			state2Path1Exists.ShouldBe(true);
			state2Path2Exists.ShouldBe(false);

			state3Path1Exists.ShouldBe(true);
			state3Path2Exists.ShouldBe(false);

			state4Path1Exists.ShouldBe(true);
			state4Path2Exists.ShouldBe(false);
			state4EntriesOfCid0.ShouldBe(0);
		}
		
		[Fact]
		public void NondeterministicChoiceSetsProbabilitiesCorrectly()
		{
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var state1Path1Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleChoice(3);
			var state1Path2Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleChoice(3);
			var state1Path3Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleChoice(3);
			var state1Path4Exists = _choiceResolver.PrepareNextPath();
			
			state1Path1Exists.ShouldBe(true);
			state1Path2Exists.ShouldBe(true);
			state1Path3Exists.ShouldBe(true);
			state1Path4Exists.ShouldBe(false);

			var pOfCid0 = _stepGraph.GetProbabilityOfContinuationId(0);
			var pOfCid1 = _stepGraph.GetProbabilityOfContinuationId(1);
			var pOfCid2 = _stepGraph.GetProbabilityOfContinuationId(2);
			var pOfCid3 = _stepGraph.GetProbabilityOfContinuationId(3);

			var entries = CountEntries(0);
			entries.ShouldBe(3);
			entries = CountEntries(1);
			entries.ShouldBe(0);
			entries = CountEntries(2);
			entries.ShouldBe(0);
			entries = CountEntries(3);
			entries.ShouldBe(0);
			pOfCid0.ShouldBe(1.0);
			pOfCid1.ShouldBe(1.0);
			pOfCid2.ShouldBe(1.0);
			pOfCid3.ShouldBe(1.0);
		}


		[Fact]
		public void ProbabilisticChoiceSetsProbabilitiesCorrectly()
		{
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var state1Path1Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.SetProbabilityOfLastChoice(new Probability(0.3));
			var state1Path2Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.SetProbabilityOfLastChoice(new Probability(0.3));
			var state1Path3Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.SetProbabilityOfLastChoice(new Probability(0.4));
			var state1Path4Exists = _choiceResolver.PrepareNextPath();

			state1Path1Exists.ShouldBe(true);
			state1Path2Exists.ShouldBe(true);
			state1Path3Exists.ShouldBe(true);
			state1Path4Exists.ShouldBe(false);

			var pOfCid0 = _stepGraph.GetProbabilityOfContinuationId(0);
			var pOfCid1 = _stepGraph.GetProbabilityOfContinuationId(1);
			var pOfCid2 = _stepGraph.GetProbabilityOfContinuationId(2);
			var pOfCid3 = _stepGraph.GetProbabilityOfContinuationId(3);

			var entries = CountEntries(0);
			entries.ShouldBe(3);
			entries = CountEntries(1);
			entries.ShouldBe(0);
			entries = CountEntries(2);
			entries.ShouldBe(0);
			entries = CountEntries(3);
			entries.ShouldBe(0);
			pOfCid0.ShouldBe(1.0);
			pOfCid1.ShouldBe(0.3);
			pOfCid2.ShouldBe(0.3);
			pOfCid3.ShouldBe(0.4);
		}


		[Fact]
		public void ProbabilisticChoiceSetsProbabilitiesToDefaultsCorrectly()
		{
			_choiceResolver.PrepareNextState();
			_stepGraph.Clear();
			var state1Path1Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			var state1Path2Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			var state1Path3Exists = _choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			var state1Path4Exists = _choiceResolver.PrepareNextPath();

			state1Path1Exists.ShouldBe(true);
			state1Path2Exists.ShouldBe(true);
			state1Path3Exists.ShouldBe(true);
			state1Path4Exists.ShouldBe(false);

			var pOfCid0 = _stepGraph.GetProbabilityOfContinuationId(0);
			var pOfCid1 = _stepGraph.GetProbabilityOfContinuationId(1);
			var pOfCid2 = _stepGraph.GetProbabilityOfContinuationId(2);
			var pOfCid3 = _stepGraph.GetProbabilityOfContinuationId(3);

			var entries = CountEntries(0);
			entries.ShouldBe(3);
			entries = CountEntries(1);
			entries.ShouldBe(0);
			entries = CountEntries(2);
			entries.ShouldBe(0);
			entries = CountEntries(3);
			entries.ShouldBe(0);
			pOfCid0.ShouldBe(1.0);
			pOfCid1.ShouldBe(1.0 / 3);
			pOfCid2.ShouldBe(1.0 / 3);
			pOfCid3.ShouldBe(1.0 / 3);
		}
	}
}
