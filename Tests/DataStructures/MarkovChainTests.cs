// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	using SafetySharp.Utilities.Graph;
	using SafetySharp.Analysis.Probabilistic.DtmcBased.ExportToGv;

	public class MarkovChainTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }
		
		public MarkovChainTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTest()
		{
			var markovChain= (new MarkovChainExamples.Example1()).MarkovChain;
			markovChain.ProbabilityMatrix.PrintMatrix(Output.Log);
			markovChain.ValidateStates();
			markovChain.PrintPathWithStepwiseHighestProbability(10);
			var enumerator = markovChain.GetEnumerator();
			var counter = 0.0;
			while (enumerator.MoveNextState())
			{
				while (enumerator.MoveNextTransition())
				{
					counter += enumerator.CurrentTransition.Value;
				}
			}
			Assert.Equal(2.0, counter);
		}

		[Fact]
		public void MarkovChainFormulaEvaluatorTest()
		{
			var markovChain = (new MarkovChainExamples.Example1()).MarkovChain;

			Func<bool> returnTrue = () => true;
			var stateFormulaLabel1 = new StateFormula(returnTrue, "label1");
			var stateFormulaLabel2 = new StateFormula(returnTrue, "label2");
			var stateFormulaBoth = new BinaryFormula(stateFormulaLabel1,BinaryOperator.And, stateFormulaLabel2);
			var stateFormulaAny = new BinaryFormula(stateFormulaLabel1, BinaryOperator.Or, stateFormulaLabel2);
			var evaluateStateFormulaLabel1 = markovChain.CreateFormulaEvaluator(stateFormulaLabel1);
			var evaluateStateFormulaLabel2 = markovChain.CreateFormulaEvaluator(stateFormulaLabel2);
			var evaluateStateFormulaBoth = markovChain.CreateFormulaEvaluator(stateFormulaBoth);
			var evaluateStateFormulaAny = markovChain.CreateFormulaEvaluator(stateFormulaAny);
			
			Assert.Equal(evaluateStateFormulaLabel1(0), false);
			Assert.Equal(evaluateStateFormulaLabel2(0), true);
			Assert.Equal(evaluateStateFormulaBoth(0), false);
			Assert.Equal(evaluateStateFormulaAny(0), true);
			Assert.Equal(evaluateStateFormulaLabel1(1), true);
			Assert.Equal(evaluateStateFormulaLabel2(1), false);
			Assert.Equal(evaluateStateFormulaBoth(1), false);
			Assert.Equal(evaluateStateFormulaAny(1), true);
		}


		[Fact]
		public void CalculateAncestorsTest()
		{
			var markovChain = (new MarkovChainExamples.Example1()).MarkovChain;

			var underlyingDigraph = markovChain.CreateUnderlyingDigraph();
			var nodesToIgnore = new Dictionary<int,bool>();
			var selectedNodes1 = new Dictionary<int,bool>();
			selectedNodes1.Add(1,true);
			var result1 = underlyingDigraph.BaseGraph.GetAncestors(selectedNodes1,nodesToIgnore.ContainsKey);
			
			var selectedNodes2 = new Dictionary<int, bool>();
			selectedNodes2.Add(0, true);
			var result2 = underlyingDigraph.BaseGraph.GetAncestors(selectedNodes2, nodesToIgnore.ContainsKey);

			Assert.Equal(2, result1.Count);
			Assert.Equal(1, result2.Count);
		}


		[Fact]
		public void CalculateAncestors2Test()
		{
			var markovChain = (new MarkovChainExamples.Example2()).MarkovChain;

			var underlyingDigraph = markovChain.CreateUnderlyingDigraph();
			var nodesToIgnore = new Dictionary<int, bool>();
			var selectedNodes1 = new Dictionary<int, bool>();
			selectedNodes1.Add(1, true);
			var result1 = underlyingDigraph.BaseGraph.GetAncestors(selectedNodes1, nodesToIgnore.ContainsKey);

			var selectedNodes2 = new Dictionary<int, bool>();
			selectedNodes2.Add(0, true);
			var result2 = underlyingDigraph.BaseGraph.GetAncestors(selectedNodes2, nodesToIgnore.ContainsKey);

			Assert.Equal(2, result1.Count);
			Assert.Equal(1, result2.Count);
		}
	}
}
