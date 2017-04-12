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

	public class LtmdpContinuationDistributionMapperTests
	{
		public TestTraceOutput Output { get; }


		public LtmdpContinuationDistributionMapperTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		private LtmdpContinuationDistributionMapper _mapper = new LtmdpContinuationDistributionMapper();


		private int CountEntriesAndAddDistributionsToMap(int cid, Dictionary<int, bool> existingDistributions)
		{
			var enumerator = _mapper.GetDistributionsOfContinuationEnumerator(cid);
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
			_mapper.AddInitialDistributionAndContinuation();

			var existingDistributions = new Dictionary<int, bool>();

			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			entries.ShouldBe(1);
		}
		
		[Fact]
		public void ProbabilisticSplitRemovesInitialContinuation()
		{
			_mapper.AddInitialDistributionAndContinuation();
			_mapper.ProbabilisticSplit(0, 1, 3);

			var existingDistributions = new Dictionary<int, bool>();
			var entries = CountEntriesAndAddDistributionsToMap(0, existingDistributions);
			entries.ShouldBe(0);
		}
		
		[Fact]
		public void OneDistributionWithThreeContinuations()
		{
			_mapper.AddInitialDistributionAndContinuation();
			_mapper.ProbabilisticSplit(0, 1, 3);

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
			_mapper.AddInitialDistributionAndContinuation();
			_mapper.ProbabilisticSplit(0, 1, 3);
			_mapper.ProbabilisticSplit(1, 4, 5);


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
			_mapper.AddInitialDistributionAndContinuation();
			_mapper.ProbabilisticSplit(0, 1, 3);
			_mapper.NonDeterministicSplit(1, 4, 5);


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
			_mapper.AddInitialDistributionAndContinuation();
			_mapper.ProbabilisticSplit(0, 1, 3);
			_mapper.NonDeterministicSplit(1, 4, 5);

			// simulate undo of probabilistic split (2 and 3 should not be executed anymore)
			_mapper.RemoveCidInDistributions(2);
			_mapper.RemoveCidInDistributions(3);


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
			_mapper.AddInitialDistributionAndContinuation();
			_mapper.ProbabilisticSplit(0, 1, 3);
			_mapper.NonDeterministicSplit(1, 4, 5);

			// simulate undo of nondeterministic split (5 should not be executed anymore)
			_mapper.RemoveDistributionsWithCid(5);

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
	}
}
