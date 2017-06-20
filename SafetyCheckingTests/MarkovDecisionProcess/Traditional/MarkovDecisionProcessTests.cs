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

namespace Tests.MarkovDecisionProcess.Traditional
{
	using ISSE.SafetyChecking.GenericDataStructures;
	using JetBrains.Annotations;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using Examples;

	public class MarkovDecisionProcessTests
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

		public MarkovDecisionProcessTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void SumOfAllDistributionsOk(MarkovDecisionProcessExample example)
		{
			var mdp= example.Mdp;
			mdp.RowsWithDistributions.PrintMatrix(Output.Log);
			mdp.ValidateStates();
			mdp.PrintPathWithStepwiseHighestProbability(10);
			var enumerator = mdp.GetEnumerator();
			var counter = 0.0;
			while (enumerator.MoveNextState())
			{
				while (enumerator.MoveNextDistribution())
				{
					var counterOfTransition = 0.0;
					while (enumerator.MoveNextTransition())
					{
						counterOfTransition += enumerator.CurrentTransition.Value;
					}
					Assert.Equal(1.0, counterOfTransition);
					counter += counterOfTransition;
				}
			}
			Assert.Equal(example.StateDistributions*1.0, counter);
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void FormulaEvaluatorTest(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;

			var evaluateStateFormulaLabel1 = mdp.CreateFormulaEvaluator(MarkovDecisionProcessExample.Label1Formula);
			var evaluateStateFormulaLabel2 = mdp.CreateFormulaEvaluator(MarkovDecisionProcessExample.Label2Formula);
			var evaluateStateFormulaExample1 = mdp.CreateFormulaEvaluator(example.ExampleFormula1);
			var evaluateStateFormulaExample2 = mdp.CreateFormulaEvaluator(example.ExampleFormula2);

			var satisfyStateFormulaLabel1 = 0;
			var satisfyStateFormulaLabel2 = 0;
			var satisfyStateFormulaExample1 = 0;
			var satisfyStateFormulaExample2 = 0;

			for (var i = 0; i < mdp.States; i++)
			{
				if (evaluateStateFormulaLabel1(i))
				{
					Assert.True(example.StatesSatisfyDirectlyLabel1Formula.ContainsKey(i), $"Formula is satisfied in state {i}, which is not expected (label1)");
					satisfyStateFormulaLabel1++;
				}
				if (evaluateStateFormulaLabel2(i))
				{
					Assert.True(example.StatesSatisfyDirectlyLabel2Formula.ContainsKey(i), $"Formula is satisfied in state {i}, which is not expected (label2)");
					satisfyStateFormulaLabel2++;
				}
				if (evaluateStateFormulaExample1(i))
				{
					Assert.True(example.StatesSatisfyDirectlyExampleFormula1.ContainsKey(i), $"Formula is satisfied in state {i}, which is not expected (exampleformula1)");
					satisfyStateFormulaExample1++;
				}
				if (evaluateStateFormulaExample2(i))
				{
					Assert.True(example.StatesSatisfyDirectlyExampleFormula2.ContainsKey(i), $"Formula is satisfied in state {i}, which is not expected (exampleformula2)");
					satisfyStateFormulaExample2++;
				}
			}
			Assert.Equal(example.StatesSatisfyDirectlyLabel1Formula.Count, satisfyStateFormulaLabel1);
			Assert.Equal(example.StatesSatisfyDirectlyLabel2Formula.Count, satisfyStateFormulaLabel2);
			Assert.Equal(example.StatesSatisfyDirectlyExampleFormula1.Count, satisfyStateFormulaExample1);
			Assert.Equal(example.StatesSatisfyDirectlyExampleFormula2.Count, satisfyStateFormulaExample2);
		}

		[Theory, MemberData(nameof(DiscoverTests))]
		public void CalculateAncestorsTest(MarkovDecisionProcessExample example)
		{
			var mdp = example.Mdp;

			var underlyingDigraph = mdp.CreateUnderlyingDigraph();
			var nodesToIgnore = new Dictionary<int, bool>();

			var result1 = underlyingDigraph.BaseGraph.GetAncestors(example.StatesSatisfyDirectlyLabel1Formula, nodesToIgnore.ContainsKey);
			foreach (var result in result1.Keys)
			{
				Assert.True(example.AncestorsOfStatesWithLabel1.ContainsKey(result), $"state {result} not found in expected results (label1)");
			}
			foreach (var result in example.AncestorsOfStatesWithLabel1.Keys)
			{
				Assert.True(result1.ContainsKey(result), $"state {result} not found in calculated results (label1)");
			}


			var result2 = underlyingDigraph.BaseGraph.GetAncestors(example.StatesSatisfyDirectlyLabel2Formula, nodesToIgnore.ContainsKey);

			foreach (var result in result2.Keys)
			{
				Assert.True(example.AncestorsOfStatesWithLabel2.ContainsKey(result), $"state {result} not found in expected results (label2)");
			}
			foreach (var result in example.AncestorsOfStatesWithLabel2.Keys)
			{
				Assert.True(result2.ContainsKey(result), $"state {result} not found in calculated results (label2)");
			}
		}
	}
}
