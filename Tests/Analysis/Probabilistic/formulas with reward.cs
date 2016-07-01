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

namespace Tests.Analysis.Probabilistic
{
	using System;
	using System.Diagnostics;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class FormulasWithReward : ProbabilisticAnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			bool formulaOfReward1;
			bool formulaOfReward2;

			using (var probabilityChecker = new ProbabilityChecker(TestModel.InitializeModel(c)))
			{
				var typeOfModelChecker = (Type)Arguments[0];
				var modelChecker = (ProbabilisticModelChecker)Activator.CreateInstance(typeOfModelChecker,probabilityChecker);

				Formula final2 = c.Value == 2;
				Formula final3 = c.Value == 3;

				var calculateSteadyStateReward1 = probabilityChecker.CalculateFormula(new LongRunExpectedRewardFormula(() => c.reward1, 2.5-0.001,2.5+0.001));
				var calculateSteadyStateReward2 = probabilityChecker.CalculateFormula(new LongRunExpectedRewardFormula(()=> c.reward2, 2.0 - 0.001, 2.0 + 0.001));
				probabilityChecker.CreateMarkovChain();
				probabilityChecker.DefaultChecker = modelChecker;
				formulaOfReward1 = calculateSteadyStateReward1.Calculate();
				formulaOfReward2 = calculateSteadyStateReward2.Calculate();
			}

			formulaOfReward1.ShouldBe(true);
			formulaOfReward2.ShouldBe(true);
		}

		private class C : Component
		{
			private int _value;
			public Reward reward1 = new Reward(false);
			public Reward reward2 = new Reward(false);

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
				switch (Value)
				{
					case 1:
						reward1.Positive(1);
						break;
					case 2:
						reward1.Positive(2);
						break;
					default:
						reward1.Positive(3);
						break;
				}
				reward2.Positive(2);
			}
		}
	}
}