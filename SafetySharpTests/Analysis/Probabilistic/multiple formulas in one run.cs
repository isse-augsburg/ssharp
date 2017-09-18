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

namespace Tests.Analysis.Probabilistic
{
	using System;
	using System.Diagnostics;
	using SafetySharp.Analysis;
	using SafetySharp.ModelChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class MultipleFormulasInOneRun : ProbabilisticAnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfFinal2;
			Probability probabilityOfFinal3;
			
			Formula valueIs2 = c.Value == 2;
			Formula valueIs3 = c.Value == 3;
			var final2 = new UnaryFormula(valueIs2, UnaryOperator.Finally);
			var final3 = new UnaryFormula(valueIs3, UnaryOperator.Finally);

			var markovChainGenerator = new SafetySharpMarkovChainFromExecutableModelGenerator(TestModel.InitializeModel(c));
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.LtmcModelChecker = (ISSE.SafetyChecking.LtmcModelChecker)Arguments[0];
			markovChainGenerator.AddFormulaToCheck(final2);
			markovChainGenerator.AddFormulaToCheck(final3);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal2 = modelChecker.CalculateProbability(final2);
				probabilityOfFinal3 = modelChecker.CalculateProbability(final3);
			}

			probabilityOfFinal2.Is(0.3, tolerance: 0.0001).ShouldBe(true);
			probabilityOfFinal3.Is(0.6, tolerance: 0.0001).ShouldBe(true);
		}

		private class C : Component
		{
			private int _value;
			public int Value
			{
				set { _value = value; }
				get {  return _value; }
			}

			public override void Update()
			{
				if (Value == 0)
				{
					Value = Choose(new Option<int>(new Probability(0.1), 1),
								   new Option<int>(new Probability(0.3), 2),
								   new Option<int>(new Probability(0.6), 3));
				}
			}
		}

	}
}