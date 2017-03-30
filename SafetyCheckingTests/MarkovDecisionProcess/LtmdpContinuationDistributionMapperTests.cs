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

		[Fact]
		public void OnlyOneInitialDistribution()
		{
			var mapper = new LtmdpContinuationDistributionMapper();

			mapper.AddInitialDistributionAndContinuation();

			var enumerator = mapper.GetDistributionsOfContinuationEnumerator(0);
			var entries = 0;
			while (enumerator.MoveNext())
			{
				entries++;
			}

			entries.ShouldBe(1);
		}
		
		[Fact]
		public void ProbabilisticSplitRemovesInitialContinuation()
		{
			var mapper = new LtmdpContinuationDistributionMapper();

			mapper.AddInitialDistributionAndContinuation();
			mapper.ProbabilisticSplit(0, 1, 3);

			var enumerator = mapper.GetDistributionsOfContinuationEnumerator(0);
			var entries = 0;
			while (enumerator.MoveNext())
			{
				entries++;
			}
			entries.ShouldBe(0);
		}
		
		[Fact]
		public void OneDistributionWithThreeContinuations()
		{
			var mapper = new LtmdpContinuationDistributionMapper();
			var existingDistributions = new Dictionary<int, bool>();

			mapper.AddInitialDistributionAndContinuation();
			mapper.ProbabilisticSplit(0, 1, 3);

			var enumerator = mapper.GetDistributionsOfContinuationEnumerator(1);
			var entries = 0;
			while (enumerator.MoveNext())
			{
				entries++;
				existingDistributions[enumerator.CurrentDistributionId] = true;
			}
			entries.ShouldBe(1);

			enumerator = mapper.GetDistributionsOfContinuationEnumerator(2);
			entries = 0;
			while (enumerator.MoveNext())
			{
				entries++;
				existingDistributions[enumerator.CurrentDistributionId] = true;
			}
			entries.ShouldBe(1);


			enumerator = mapper.GetDistributionsOfContinuationEnumerator(3);
			entries = 0;
			while (enumerator.MoveNext())
			{
				entries++;
				existingDistributions[enumerator.CurrentDistributionId] = true;
			}
			entries.ShouldBe(1);

			existingDistributions.Count.ShouldBe(1);
			existingDistributions.ShouldContainKey(0);
		}
	}
}
