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
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class SameTargetStateWithDifferentFaults : ProbabilisticAnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfFinal1;

			using (var probabilityChecker = new ProbabilityChecker(TestModel.InitializeModel(c)))
			{
				var typeOfModelChecker = (Type)Arguments[0];
				var modelChecker = (ProbabilisticModelChecker)Activator.CreateInstance(typeOfModelChecker, probabilityChecker);

				Formula final1 = c.Result == 1;
				var checkProbabilityOfFinal1 = probabilityChecker.CalculateProbabilityToReachStates(final1);
				probabilityChecker.CreateProbabilityMatrix();
				probabilityChecker.DefaultChecker = modelChecker;
				probabilityOfFinal1 = checkProbabilityOfFinal1.Check();
			}
			
			probabilityOfFinal1.Be(0.325, 0.000001).ShouldBe(true);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new TransientFault();

			public int Result;

			public virtual void Helper1()
			{
				Result = 2;
			}

			public virtual void Helper2()
			{
				Result = 3;
			}


			public override void Update()
			{
				if (Result == 0)
				{
					if (Choose(new Option<bool>(new Probability(0.1), true),
						new Option<bool>(new Probability(0.9), false)))
					{
						// way 1
						Result = 1;
					}
					else
					{
						Helper1();
					}
				}
			}


			[FaultEffect(Fault = nameof(F1)), Priority(1)]
			public class E1 : C
			{
				public override void Helper1()
				{
					Helper2();
				}
			}

			[FaultEffect(Fault = nameof(F2)), Priority(2)]
			public class E2 : C
			{
				public override void Helper2()
				{
					// way 2: Fault F1 and F2 makes this reachable
					Result = 1;
				}
			}
		}
	}
}
