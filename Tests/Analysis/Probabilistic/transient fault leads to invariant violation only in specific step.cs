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
	
	internal class TransientFaultLeadsToInvariantViolationOnlyInSpecificStep : ProbabilisticAnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfInvariantViolation;

			using (var probabilityChecker = new ProbabilityChecker(TestModel.InitializeModel(c)))
			{
				var typeOfModelChecker = (Type)Arguments[0];
				var modelChecker = (ProbabilisticModelChecker)Activator.CreateInstance(typeOfModelChecker,probabilityChecker);
				
				Formula invariantViolated = c.ViolateInvariant;
				var checkProbabilityOfInvariantViolation = probabilityChecker.CalculateProbabilityToReachStates(invariantViolated);
				probabilityChecker.CreateProbabilityMatrix();
				probabilityChecker.DefaultChecker = modelChecker;
				probabilityOfInvariantViolation = checkProbabilityOfInvariantViolation.Calculate();
				//probabilityOfFinal1 = checkProbabilityOf1.CheckWithChecker(modelChecker);
			}

			probabilityOfInvariantViolation.Be(0.1, 0.00001).ShouldBe(true);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault
			{
				ProbabilityOfOccurrence = new Probability(0.1)
			};

			[Range(0,11,OverflowBehavior.Clamp)]
			private int _timestep;

			public bool ViolateInvariant;
			
			protected virtual void CriticalStep()
			{
				ViolateInvariant = false;
			}

			public override void Update()
			{
				_timestep++;
				if (_timestep==10)
					CriticalStep();
			}


			[FaultEffect(Fault = nameof(F1)), Priority(1)]
			public class E1 : C
			{
				protected override void CriticalStep()
				{
					base.ViolateInvariant = true;
				}
			}
		}
	}
}