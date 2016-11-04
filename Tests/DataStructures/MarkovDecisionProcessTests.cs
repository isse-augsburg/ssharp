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

	public class MarkovDecisionProcessTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		private MarkovDecisionProcess _mdp;

		private void CreateExemplaryMdp1()
		{
			Func<bool> returnTrue = () => true;
			Func<bool> returnFalse = () => false;

			_mdp = new MarkovDecisionProcess();
			_mdp.StateFormulaLabels = new string[] { "label1" , "label2" };
			_mdp.StateRewardRetrieverLabels = new string[] { };
			_mdp.StartWithInitialDistributions();
			_mdp.StartWithNewInitialDistribution();
			_mdp.AddTransitionToInitialDistribution(0,1.0);
			_mdp.FinishInitialDistribution();
			_mdp.FinishInitialDistributions();
			_mdp.StartWithNewDistributions(1);
			_mdp.StartWithNewDistribution();
			_mdp.AddTransition(1, 1.0);
			_mdp.SetStateLabeling(1, new StateFormulaSet(new []{ returnTrue, returnFalse }));
			_mdp.FinishDistribution();
			_mdp.FinishDistributions();
			_mdp.StartWithNewDistributions(0);
			_mdp.StartWithNewDistribution();
			_mdp.AddTransition(1, 0.6);
			_mdp.AddTransition(0, 0.4);
			_mdp.SetStateLabeling(0, new StateFormulaSet(new[] { returnFalse, returnTrue }));
			_mdp.FinishDistribution();
			_mdp.FinishDistributions();
			//_markovChain.ProbabilityMatrix.OptimizeAndSeal();
		}

		private void CreateExemplaryMdp2()
		{
			Func<bool> returnTrue = () => true;
			Func<bool> returnFalse = () => false;

			_mdp = new MarkovDecisionProcess();
			_mdp.StateFormulaLabels = new string[] { "label1", "label2" };
			_mdp.StateRewardRetrieverLabels = new string[] { };
			_mdp.StartWithInitialDistributions();
			_mdp.StartWithNewInitialDistribution();
			_mdp.AddTransitionToInitialDistribution(0, 1.0);
			_mdp.FinishInitialDistribution();
			_mdp.FinishInitialDistributions();
			_mdp.StartWithNewDistributions(1);
			_mdp.StartWithNewDistribution();
			_mdp.AddTransition(1, 1.0);
			_mdp.SetStateLabeling(1, new StateFormulaSet(new[] { returnTrue, returnFalse }));
			_mdp.FinishDistribution();
			_mdp.FinishDistributions();
			_mdp.StartWithNewDistributions(0);
			_mdp.StartWithNewDistribution();
			_mdp.AddTransition(1, 0.1);
			_mdp.SetStateLabeling(0, new StateFormulaSet(new[] { returnFalse, returnTrue }));
			_mdp.FinishDistribution();
			_mdp.FinishDistributions();
			//_markovChain.ProbabilityMatrix.OptimizeAndSeal();
		}

		public MarkovDecisionProcessTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTest()
		{
			CreateExemplaryMdp1();
			_mdp.RowsWithDistributions.PrintMatrix(Output.Log);
			_mdp.ValidateStates();
			_mdp.PrintPathWithStepwiseHighestProbability(10);
			var enumerator = _mdp.GetEnumerator();
			var counter = 0.0;
			while (enumerator.MoveNextState())
			{
				while (enumerator.MoveNextDistribution())
				{
					while (enumerator.MoveNextTransition())
					{
						counter += enumerator.CurrentTransition.Value;
					}
				}
			}
			Assert.Equal(2.0, counter);
		}

		[Fact]
		public void MarkovChainFormulaEvaluatorTest()
		{
			CreateExemplaryMdp1();

			Func<bool> returnTrue = () => true;
			var stateFormulaLabel1 = new StateFormula(returnTrue, "label1");
			var stateFormulaLabel2 = new StateFormula(returnTrue, "label2");
			var stateFormulaBoth = new BinaryFormula(stateFormulaLabel1,BinaryOperator.And, stateFormulaLabel2);
			var stateFormulaAny = new BinaryFormula(stateFormulaLabel1, BinaryOperator.Or, stateFormulaLabel2);
			var evaluateStateFormulaLabel1 = _mdp.CreateFormulaEvaluator(stateFormulaLabel1);
			var evaluateStateFormulaLabel2 = _mdp.CreateFormulaEvaluator(stateFormulaLabel2);
			var evaluateStateFormulaBoth = _mdp.CreateFormulaEvaluator(stateFormulaBoth);
			var evaluateStateFormulaAny = _mdp.CreateFormulaEvaluator(stateFormulaAny);
			
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
			CreateExemplaryMdp1();

			var underlyingDigraph = _mdp.CreateUnderlyingDigraphAllDistributions();
			var nodesToIgnore = new Dictionary<int,bool>();
			var selectedNodes1 = new Dictionary<int,bool>();
			selectedNodes1.Add(1,true);
			var result1 = underlyingDigraph.Graph.GetAncestors(selectedNodes1,nodesToIgnore.ContainsKey);
			
			var selectedNodes2 = new Dictionary<int, bool>();
			selectedNodes2.Add(0, true);
			var result2 = underlyingDigraph.Graph.GetAncestors(selectedNodes2, nodesToIgnore.ContainsKey);

			Assert.Equal(2, result1.Count);
			Assert.Equal(1, result2.Count);
		}


		[Fact]
		public void CalculateAncestors2Test()
		{
			CreateExemplaryMdp2();

			var underlyingDigraph = _mdp.CreateUnderlyingDigraphAllDistributions();
			var nodesToIgnore = new Dictionary<int, bool>();
			var selectedNodes1 = new Dictionary<int, bool>();
			selectedNodes1.Add(1, true);
			var result1 = underlyingDigraph.Graph.GetAncestors(selectedNodes1, nodesToIgnore.ContainsKey);

			var selectedNodes2 = new Dictionary<int, bool>();
			selectedNodes2.Add(0, true);
			var result2 = underlyingDigraph.Graph.GetAncestors(selectedNodes2, nodesToIgnore.ContainsKey);

			Assert.Equal(2, result1.Count);
			Assert.Equal(1, result2.Count);
		}
	}
}
