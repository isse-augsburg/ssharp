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
	using SafetySharp.Analysis;
	using SafetySharp.ModelChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;
	
	internal class ThreeExitsWithDice : ProbabilisticAnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfFinal1;
			
			var final1Formula =new UnaryFormula(c.InA(), UnaryOperator.Finally);

			var markovChainGenerator = new SafetySharpMarkovChainFromExecutableModelGenerator(TestModel.InitializeModel(c));
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.WriteGraphvizModels = true;
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			markovChainGenerator.AddFormulaToCheck(final1Formula);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			var typeOfModelChecker = (Type)Arguments[0];
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal1 = modelChecker.CalculateProbability(final1Formula);
			}

			probabilityOfFinal1.Between(0.3, 0.4).ShouldBe(true);
		}

		private class C : Component
		{
			[Hidden]
			public int Decision;

			public int State = 0;

			public Formula InA() => Decision == 1;

			public Formula InB()
			{
				return Decision == 2;
			}

			public Formula InC()
			{
				return Decision == 3;
			}

			public override void Update()
			{
				if (State == 0)
				{
					State = Choose(
						new Option<int>(new Probability(0.5), 1),
						new Option<int>(new Probability(0.5), 2));
					Decision = 0;
				}
				else if (State == 1)
				{
					State = 3;
					Decision = Choose(
						new Option<int>(new Probability(0.5), 1),
						new Option<int>(new Probability(0.5), 2));
				}
				else if (State == 2)
				{
					State = Choose(
						new Option<int>(new Probability(0.5), 0),
						new Option<int>(new Probability(0.5), 3));
					Decision = State == 3 ? 3 : 0;
				}
				else if (State == 3)
				{
					Decision = 0;
				}
			}
		}

	}
}