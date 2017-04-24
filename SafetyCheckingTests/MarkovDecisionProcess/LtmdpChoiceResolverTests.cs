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
	using System.Collections.Generic;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.GenericDataStructures;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
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
		}

		private LtmdpChoiceResolver _choiceResolver = new LtmdpChoiceResolver();


		private int CountEntriesAndAddDistributionsToMap(int cid, Dictionary<int, bool> existingDistributions)
		{
			var enumerator = _choiceResolver.CidToDidMapper.GetDistributionsOfContinuationEnumerator(cid);
			var entries = 0;
			while (enumerator.MoveNext())
			{
				entries++;
				existingDistributions[enumerator.CurrentDistributionId] = true;
			}
			return entries;
		}

		[Fact]
		public void OnlyOneInitialDistribution()
		{
			_choiceResolver.PrepareNextState();
			var path1Exists = _choiceResolver.PrepareNextPath();
			var path2Exists = _choiceResolver.PrepareNextPath();
			var existingDistributions = new Dictionary<int, bool>();

			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			path1Exists.ShouldBe(true);
			path2Exists.ShouldBe(false);
			entries.ShouldBe(1);
		}
		
		[Fact]
		public void ProbabilisticSplitRemovesInitialContinuation()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(2);

			var existingDistributions = new Dictionary<int, bool>();
			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			entries.ShouldBe(0);
		}
		
		[Fact]
		public void OneDistributionWithThreeContinuations()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);

			var existingDistributions = new Dictionary<int, bool>();

			var entries = CountEntriesAndAddDistributionsToMap(1, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(2, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(3, existingDistributions);
			entries.ShouldBe(1);

			existingDistributions.Count.ShouldBe(1);
			existingDistributions.ShouldContainKey(0);
		}



		[Fact]
		public void OneDistributionWithFiveContinuationsAfterTwoSplits()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.HandleProbabilisticChoice(2);

			var existingDistributions = new Dictionary<int, bool>();

			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(1, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(2, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(3, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(4, existingDistributions);
			entries.ShouldBe(1);
			
			entries = CountEntriesAndAddDistributionsToMap(5, existingDistributions);
			entries.ShouldBe(1);

			existingDistributions.Count.ShouldBe(1);
			existingDistributions.ShouldContainKey(0);
		}

		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplits()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.HandleChoice(2);

			var existingDistributions = new Dictionary<int, bool>();
			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(1, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(2, existingDistributions);
			entries.ShouldBe(2);

			entries = CountEntriesAndAddDistributionsToMap(3, existingDistributions);
			entries.ShouldBe(2);

			entries = CountEntriesAndAddDistributionsToMap(4, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(5, existingDistributions);
			entries.ShouldBe(1);

			existingDistributions.Count.ShouldBe(2);
			existingDistributions.ShouldContainKey(0);
			existingDistributions.ShouldContainKey(1);
		}
		
		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplitsWithRemove()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			var choiceToMakeDeterministic = _choiceResolver.LastChoiceIndex;
			_choiceResolver.HandleChoice(2);

			// MakeChoiceAtIndexDeterministic of probabilistic split
			_choiceResolver.MakeChoiceAtIndexDeterministic(choiceToMakeDeterministic);


			var existingDistributions = new Dictionary<int, bool>();
			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(1, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(2, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(3, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(4, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(5, existingDistributions);
			entries.ShouldBe(1);

			existingDistributions.Count.ShouldBe(2);
			existingDistributions.ShouldContainKey(0);
			existingDistributions.ShouldContainKey(1);
		}

		[Fact]
		public void TwoDistributionWithFiveContinuationsAfterTwoSplitsWithRemove2()
		{
			// Only first path tested, yet.
			_choiceResolver.PrepareNextState();
			_choiceResolver.PrepareNextPath();
			_choiceResolver.HandleProbabilisticChoice(3);
			_choiceResolver.HandleChoice(2);
			var choiceToMakeDeterministic = _choiceResolver.LastChoiceIndex;

			// MakeChoiceAtIndexDeterministic of nondeterministic split
			_choiceResolver.MakeChoiceAtIndexDeterministic(choiceToMakeDeterministic);

			var existingDistributions = new Dictionary<int, bool>();
			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(1, existingDistributions);
			entries.ShouldBe(0);

			entries = CountEntriesAndAddDistributionsToMap(2, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(3, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(4, existingDistributions);
			entries.ShouldBe(1);

			entries = CountEntriesAndAddDistributionsToMap(5, existingDistributions);
			entries.ShouldBe(0);

			existingDistributions.Count.ShouldBe(1);
			existingDistributions.ShouldContainKey(0);
		}

		[Fact]
		public void SimpleExample1b()
		{
			// Act
			_choiceResolver.PrepareNextState();
			var state1Path1Exists = _choiceResolver.PrepareNextPath();
			var state1Path1Choice1 = _choiceResolver.HandleProbabilisticChoice(2);
			var state1Path1Choice2 = _choiceResolver.HandleChoice(2);
			var state1Path2Exists = _choiceResolver.PrepareNextPath();
			var state1Path2Choice1 = _choiceResolver.HandleProbabilisticChoice(2);
			var state1Path2Choice2 = _choiceResolver.HandleChoice(2);
			var state1Path3Exists = _choiceResolver.PrepareNextPath();
			var state1Path3Choice1 = _choiceResolver.HandleProbabilisticChoice(2);
			var state1Path4Exists = _choiceResolver.PrepareNextPath();
			// Collect data
			var state1ExistingDistributions = new Dictionary<int, bool>();
			var state1DidsOfCid0 = CountEntriesAndAddDistributionsToMap(0, state1ExistingDistributions);
			var state1DidsOfCid1 = CountEntriesAndAddDistributionsToMap(1, state1ExistingDistributions);
			var state1DidsOfCid2 = CountEntriesAndAddDistributionsToMap(2, state1ExistingDistributions);
			var state1DidsOfCid3 = CountEntriesAndAddDistributionsToMap(3, state1ExistingDistributions);
			var state1DidsOfCid4 = CountEntriesAndAddDistributionsToMap(4, state1ExistingDistributions);

			_choiceResolver.PrepareNextState();
			var state2Path1Exists = _choiceResolver.PrepareNextPath();
			var state2Path2Exists = _choiceResolver.PrepareNextPath();
			
			_choiceResolver.PrepareNextState();
			var state3Path1Exists = _choiceResolver.PrepareNextPath();
			var state3Path2Exists = _choiceResolver.PrepareNextPath();

			_choiceResolver.PrepareNextState();
			var state4Path1Exists = _choiceResolver.PrepareNextPath();
			var state4Path2Exists = _choiceResolver.PrepareNextPath();
			// Collect data
			var state4ExistingDistributions = new Dictionary<int, bool>();
			var state4DidsOfCid0 = CountEntriesAndAddDistributionsToMap(0, state4ExistingDistributions);


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
			state1DidsOfCid0.ShouldBe(0);
			state1DidsOfCid1.ShouldBe(0);
			state1DidsOfCid2.ShouldBe(2);
			state1DidsOfCid3.ShouldBe(1);
			state1DidsOfCid4.ShouldBe(1);
			state1ExistingDistributions.Count.ShouldBe(2);
			state1ExistingDistributions.ShouldContainKey(0);
			state1ExistingDistributions.ShouldContainKey(1);

			state2Path1Exists.ShouldBe(true);
			state2Path2Exists.ShouldBe(false);

			state3Path1Exists.ShouldBe(true);
			state3Path2Exists.ShouldBe(false);

			state4Path1Exists.ShouldBe(true);
			state4Path2Exists.ShouldBe(false);
			state4DidsOfCid0.ShouldBe(1);
			state4ExistingDistributions.Count.ShouldBe(1);
			state4ExistingDistributions.ShouldContainKey(0);
		}
	}
}
