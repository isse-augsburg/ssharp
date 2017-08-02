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
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using JetBrains.Annotations;
	using LabeledTransitionMarkovChainExamples;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;

	public class BuiltinLtmcModelCheckerTests
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

		public BuiltinLtmcModelCheckerTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		/*
		[Theory, MemberData(nameof(DiscoverTests))]
		public void ProbabilityToReach_Label1(LabeledTransitionMarkovChain example)
		{
			var dtmc = example.MarkovChain;

			var finallyLabel1 = new UnaryFormula(LabeledTransitionMarkovChainExample.Label1Formula, UnaryOperator.Finally);

			using (var prismChecker = new BuiltinDtmcModelChecker(dtmc, Output.TextWriterAdapter()))
			{
				var result = prismChecker.CalculateProbability(finallyLabel1);
				result.Is(example.ProbabilityFinallyLabel1, 0.0001).ShouldBe(true);
			}
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void ProbabilityToReachIn10Steps_Label1(LabeledTransitionMarkovChain example)
		{
			var dtmc = example.MarkovChain;

			var finallyLabel1 = new BoundedUnaryFormula(LabeledTransitionMarkovChainExample.Label1Formula, UnaryOperator.Finally, 10);

			using (var prismChecker = new BuiltinDtmcModelChecker(dtmc, Output.TextWriterAdapter()))
			{
				var result = prismChecker.CalculateProbability(finallyLabel1);
				result.Is(example.ProbabilityFinally10Label1, 0.0001).ShouldBe(true);
			}
		}


		[Theory, MemberData(nameof(DiscoverTests))]
		public void ProbabilityIn10Steps_Label1UntilLabel2(LabeledTransitionMarkovChain example)
		{
			var dtmc = example.MarkovChain;

			var label1UntilLabel2 = new BoundedBinaryFormula(LabeledTransitionMarkovChainExample.Label1Formula, BinaryOperator.Until, LabeledTransitionMarkovChainExample.Label2Formula, 10);

			using (var prismChecker = new BuiltinDtmcModelChecker(dtmc, Output.TextWriterAdapter()))
			{
				var result = prismChecker.CalculateProbability(label1UntilLabel2);
				result.Is(example.ProbabilityLabel1UntilLabel2, 0.0001).ShouldBe(true);
			}
		}*/

		[Fact]
		public void StepwiseProbablityOfLabel2ForExample4()
		{
			var example = new Example4();
			var ltmc = example.Ltmc;

			var results = new List<Probability>();
			
			using (var prismChecker = new BuiltinLtmcModelChecker(ltmc, Output.TextWriterAdapter()))
			{
				for (var i = 0; i <= 10; i++)
				{
					var boundedStepi = new BoundedUnaryFormula(LabeledTransitionMarkovChainExample.Label2Formula, UnaryOperator.Finally, i);
					var resultBoundedStepi = prismChecker.CalculateProbability(boundedStepi);
					Output.Log($"Result {i}:\t{resultBoundedStepi}");
					results.Add(resultBoundedStepi);
				}

				var boundedStep200 = new BoundedUnaryFormula(LabeledTransitionMarkovChainExample.Label2Formula, UnaryOperator.Finally, 200);
				var resultBoundedStep200 = prismChecker.CalculateProbability(boundedStep200);
				Output.Log($"Result {200}:\t{resultBoundedStep200}");

				/*
				var inf = new UnaryFormula(LabeledTransitionMarkovChainExample.Label2Formula, UnaryOperator.Finally);
				var resultInf = prismChecker.CalculateProbability(inf);
				Output.Log($"Result inf:\t{resultInf}");
				*/
			}
		}

		[Fact]
		public void StepwiseProbablityOfNotLabel1UntilLabel2ForExample4()
		{
			var example = new Example4();
			var ltmc = example.Ltmc;

			var results = new List<Probability>();

			using (var prismChecker = new BuiltinLtmcModelChecker(ltmc, Output.TextWriterAdapter()))
			{
				for (var i = 0; i <= 10; i++)
				{
					var boundedStepi = new BoundedBinaryFormula(new UnaryFormula(LabeledTransitionMarkovChainExample.Label1Formula,UnaryOperator.Not), BinaryOperator.Until, LabeledTransitionMarkovChainExample.Label2Formula, i);
					var resultBoundedStepi = prismChecker.CalculateProbability(boundedStepi);
					Output.Log($"Result {i}:\t{resultBoundedStepi}");
					results.Add(resultBoundedStepi);
				}

				var boundedStep200 = new BoundedBinaryFormula(new UnaryFormula(LabeledTransitionMarkovChainExample.Label1Formula, UnaryOperator.Not), BinaryOperator.Until, LabeledTransitionMarkovChainExample.Label2Formula, 200);
				var resultBoundedStep200 = prismChecker.CalculateProbability(boundedStep200);
				Output.Log($"Result {200}:\t{resultBoundedStep200}");

				/*
				var inf = new BinaryFormula(new UnaryFormula(LabeledTransitionMarkovChainExample.Label1Formula, UnaryOperator.Not), BinaryOperator.Until, LabeledTransitionMarkovChainExample.Label2Formula);
				var resultInf = prismChecker.CalculateProbability(inf);
				Output.Log($"Result inf:\t{resultInf}");
				*/
			}
		}
	}
}
