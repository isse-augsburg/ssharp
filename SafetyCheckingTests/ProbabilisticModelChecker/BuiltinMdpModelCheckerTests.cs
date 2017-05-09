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

namespace Tests.DataStructures
{
	using System.Diagnostics;
	using ISSE.SafetyChecking.Formula;
	using JetBrains.Annotations;
	using MarkovDecisionProcessExamples;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using Shouldly;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	public class BuiltinMdpModelCheckerTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		[UsedImplicitly]
		public static IEnumerable<object[]> DiscoverTests()
		{
			foreach (var example in AllExamples.Examples)
			{
				yield return new object[] { example };// only one parameter
			}
		}

		public BuiltinMdpModelCheckerTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}


		[Theory, MemberData(nameof(DiscoverTests))]
		public void Prob1ETest_Label1(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;

			var excludedStates = new Dictionary<int, bool>() { };
			var directlySatisfiedStates = example.StatesSatisfyDirectlyLabel1Formula;

			using (var checker = new BuiltinMdpModelChecker(mdp, Output.TextWriterAdapter()))
			{
				var results = checker.StatesReachableWithProbabilityExactlyOneForAtLeastOneScheduler(directlySatisfiedStates, excludedStates);

				foreach (var result in results.Keys)
				{
					Assert.True(example.StatesProb1ELabel1.ContainsKey(result), $"state {result} not found in expected results");
				}
				foreach (var result in example.StatesProb1ELabel1.Keys)
				{
					Assert.True(results.ContainsKey(result), $"state {result} not found in calculated results");
				}
			}
		}


		[Theory, MemberData(nameof(DiscoverTests))]
		public void Prob0ATest_Label1(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;

			var excludedStates = new Dictionary<int, bool>() { };
			var directlySatisfiedStates = example.StatesSatisfyDirectlyLabel1Formula;

			using (var checker = new BuiltinMdpModelChecker(mdp, Output.TextWriterAdapter()))
			{
				var results = checker.StatesReachableWithProbabilityExactlyZeroWithAllSchedulers(directlySatisfiedStates, excludedStates);

				foreach (var result in results.Keys)
				{
					Assert.True(example.StatesProb0ALabel1.ContainsKey(result), $"state {result} not found in expected results");
				}
				foreach (var result in example.StatesProb0ALabel1.Keys)
				{
					Assert.True(results.ContainsKey(result), $"state {result} not found in calculated results");
				}
			}
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void Prob0ETest_Label1(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;

			var excludedStates = new Dictionary<int, bool>() { };
			var directlySatisfiedStates = example.StatesSatisfyDirectlyLabel1Formula;

			using (var checker = new BuiltinMdpModelChecker(mdp, Output.TextWriterAdapter()))
			{
				var results = checker.StatesReachableWithProbabilityExactlyZeroForAtLeastOneScheduler(directlySatisfiedStates, excludedStates);

				foreach (var result in results.Keys)
				{
					Assert.True(example.StatesProb0ELabel1.ContainsKey(result), $"state {result} not found in expected results");
				}
				foreach (var result in example.StatesProb0ELabel1.Keys)
				{
					Assert.True(results.ContainsKey(result), $"state {result} not found in calculated results");
				}
			}
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void MaximalProbabilityToReach_Label1(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;

			var finallyLabel1 = new UnaryFormula(MarkovDecisionProcessExample.Label1Formula, UnaryOperator.Finally);

			using (var checker = new BuiltinMdpModelChecker(mdp, Output.TextWriterAdapter()))
			{
				var result = checker.CalculateMaximalProbability(finallyLabel1);
				result.Is(example.MaximalProbabilityFinallyLabel1, 0.0001).ShouldBe(true);
			}
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void MinimalProbabilityToReach_Label1(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;

			var finallyLabel1 = new UnaryFormula(MarkovDecisionProcessExample.Label1Formula, UnaryOperator.Finally);

			using (var checker = new BuiltinMdpModelChecker(mdp, Output.TextWriterAdapter()))
			{
				var result = checker.CalculateMinimalProbability(finallyLabel1);
				result.Is(example.MinimalProbabilityFinallyLabel1, 0.0001).ShouldBe(true);
			}
		}
		
		[Theory, MemberData(nameof(DiscoverTests))]
		public void MaximalProbabilityToReachIn50Steps_Label1(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;
			var steps = 50;

			var finallyLabel1 = new BoundedUnaryFormula(MarkovDecisionProcessExample.Label1Formula, UnaryOperator.Finally, steps);

			using (var checker = new BuiltinMdpModelChecker(mdp, Output.TextWriterAdapter()))
			{
				var result = checker.CalculateMaximalProbability(finallyLabel1);
				result.Is(example.MaximalProbabilityFinallyLabel1, 0.0001).ShouldBe(true);
			}
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void MinimalProbabilityToReachIn50Steps_Label1(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;
			var steps = 50;

			var finallyLabel1 = new BoundedUnaryFormula(MarkovDecisionProcessExample.Label1Formula, UnaryOperator.Finally, steps);

			using (var checker = new BuiltinMdpModelChecker(mdp, Output.TextWriterAdapter()))
			{
				var result = checker.CalculateMinimalProbability(finallyLabel1);
				result.Is(example.MinimalProbabilityFinallyLabel1, 0.0001).ShouldBe(true);
			}
		}
	}
}
