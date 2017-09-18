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
	
	internal class PhiUntilPsi : ProbabilisticAnalysisTestObject
	{
		// A Model for \phi Until \psi (or in this case Label1 U Label2)
		//   0⟶0.1⟼1⟲
		//       0.2⟼2⟼3⟲
		//       0.7⟼4↗

		private enum S
		{
			State0WithPhi,
			State1WithNothing,
			State2WithPhi,
			State3WithPsi,
			State4WithNothing
		}

		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfFinal1;
			
			var final1Formula = new BoundedBinaryFormula(c.IsPhi(), BinaryOperator.Until, c.IsPsi(),10);

			var markovChainGenerator = new SafetySharpMarkovChainFromExecutableModelGenerator(TestModel.InitializeModel(c));
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.LtmcModelChecker = (ISSE.SafetyChecking.LtmcModelChecker)Arguments[0];
			markovChainGenerator.AddFormulaToCheck(final1Formula);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal1 = modelChecker.CalculateProbability(final1Formula);
			}

			probabilityOfFinal1.Is(0.2, 0.000001).ShouldBe(true);
		}

		private class C : Component
		{
			public Formula IsPhi()
			{
				return State == S.State0WithPhi || State == S.State2WithPhi;
			}
			public Formula IsPsi()
			{
				return State == S.State3WithPsi;
			}

			private S State = S.State0WithPhi;

			public override void Update()
			{
				//   0⟶0.1⟼1⟲
				//       0.2⟼2⟼3⟲
				//       0.7⟼4↗

				switch (State)
				{
					case S.State0WithPhi:
						State = Choose(new Option<S>(new Probability(0.1), S.State1WithNothing),
									   new Option<S>(new Probability(0.2), S.State2WithPhi),
									   new Option<S>(new Probability(0.7), S.State4WithNothing));
						return;
					case S.State2WithPhi:
						State = S.State3WithPsi;
						return;
					case S.State4WithNothing:
						State = S.State3WithPsi;
						return;
					default:
						return;
				}
			}
		}

	}
}