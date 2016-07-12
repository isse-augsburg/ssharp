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

	public class MarkovChainTests
	{
		/// <summary>
		///   Gets the output that writes to the test output stream.
		/// </summary>
		public TestTraceOutput Output { get; }

		private MarkovChain _markovChain;

		private void CreateExemplaryMarkovChain()
		{
			Func<bool> returnTrue = () => true;
			Func<bool> returnFalse = () => false;

			_markovChain = new MarkovChain();
			_markovChain.StateFormulaLabels = new string[] { "label1" , "label2" };
			_markovChain.StateRewardRetrieverLabels = new string[] { };
			_markovChain.AddInitialState(0,1.0);
			_markovChain.SetMarkovChainSourceStateOfUpcomingTransitions(1);
			_markovChain.AddTransition(1, 1.0);
			_markovChain.SetStateLabeling(1, new StateFormulaSet(new []{ returnTrue, returnFalse }));
			_markovChain.FinishSourceState();
			_markovChain.SetMarkovChainSourceStateOfUpcomingTransitions(0);
			_markovChain.AddTransition(1, 0.6);
			_markovChain.AddTransition(0, 0.4);
			_markovChain.SetStateLabeling(0, new StateFormulaSet(new[] { returnFalse, returnTrue }));
			_markovChain.FinishSourceState();
			//_markovChain.ProbabilityMatrix.OptimizeAndSeal();
		}

		public MarkovChainTests(ITestOutputHelper output)
		{
			Output = new TestTraceOutput(output);
		}

		[Fact]
		public void PassingTest()
		{
			CreateExemplaryMarkovChain();
			_markovChain.ProbabilityMatrix.PrintMatrix(Output.Log);
			_markovChain.ValidateStates();
			_markovChain.PrintPathWithStepwiseHighestProbability(10);
			var enumerator = _markovChain.ProbabilityMatrix.GetEnumerator();
			var counter = 0.0;
			while (enumerator.MoveNextRow())
			{
				while (enumerator.MoveNextColumn())
				{
					if (enumerator.CurrentColumnValue!=null)
						counter += enumerator.CurrentColumnValue.Value.Value;
					else
						throw new Exception("Entry must not be null");
				}
			}
			Assert.Equal(2.0, counter);
		}

		[Fact]
		public void MarkovChainFormulaEvaluatorTest()
		{
			CreateExemplaryMarkovChain();

			Func<bool> returnTrue = () => true;
			var stateFormulaLabel1 = new StateFormula(returnTrue, "label1");
			var stateFormulaLabel2 = new StateFormula(returnTrue, "label2");
			var stateFormulaBoth = new BinaryFormula(stateFormulaLabel1,BinaryOperator.And, stateFormulaLabel2);
			var stateFormulaAny = new BinaryFormula(stateFormulaLabel1, BinaryOperator.Or, stateFormulaLabel2);
			var evaluateStateFormulaLabel1 = _markovChain.CreateFormulaEvaluator(stateFormulaLabel1);
			var evaluateStateFormulaLabel2 = _markovChain.CreateFormulaEvaluator(stateFormulaLabel2);
			var evaluateStateFormulaBoth = _markovChain.CreateFormulaEvaluator(stateFormulaBoth);
			var evaluateStateFormulaAny = _markovChain.CreateFormulaEvaluator(stateFormulaAny);
			
			Assert.Equal(evaluateStateFormulaLabel1(0), false);
			Assert.Equal(evaluateStateFormulaLabel2(0), true);
			Assert.Equal(evaluateStateFormulaBoth(0), false);
			Assert.Equal(evaluateStateFormulaAny(0), true);
			Assert.Equal(evaluateStateFormulaLabel1(1), true);
			Assert.Equal(evaluateStateFormulaLabel2(1), false);
			Assert.Equal(evaluateStateFormulaBoth(1), false);
			Assert.Equal(evaluateStateFormulaAny(1), true);
		}
	}
}
