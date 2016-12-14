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

	internal class ProbabilitiesSumUpTo1EvenWithTransienttActivatableFault : ProbabilisticAnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfStep11FrozenValue2AndInvariantViolated;
			Probability probabilityOfStep11FrozenValue3AndInvariantViolated;
			Probability probabilityOfStep11FrozenValue2AndInvariantNotViolated;
			Probability probabilityOfStep11FrozenValue3AndInvariantNotViolated;
			
			Formula formulaProbabilityOfStep11FrozenValue2AndInvariantViolated = c._timestep == 11 && c._frozenValue == 2 && c.ViolateInvariant;
			Formula formulaProbabilityOfStep11FrozenValue3AndInvariantViolated = c._timestep == 11 && c._frozenValue == 3 && c.ViolateInvariant;
			Formula formulaProbabilityOfStep11FrozenValue2AndInvariantNotViolated = c._timestep == 11 && c._frozenValue == 2 && !c.ViolateInvariant;
			Formula formulaProbabilityOfStep11FrozenValue3AndInvariantNotViolated = c._timestep == 11 && c._frozenValue == 3 && !c.ViolateInvariant;

			var finallyFormulaProbabilityOfStep11FrozenValue2AndInvariantViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue2AndInvariantViolated, UnaryOperator.Finally);
			var finallyFormulaProbabilityOfStep11FrozenValue3AndInvariantViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue3AndInvariantViolated, UnaryOperator.Finally);
			var finallyFormulaProbabilityOfStep11FrozenValue2AndInvariantNotViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue2AndInvariantNotViolated, UnaryOperator.Finally);
			var finallyFormulaProbabilityOfStep11FrozenValue3AndInvariantNotViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue3AndInvariantNotViolated, UnaryOperator.Finally);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator(TestModel.InitializeModel(c));
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue2AndInvariantViolated);
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue3AndInvariantViolated);
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue2AndInvariantNotViolated);
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue3AndInvariantNotViolated);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			var typeOfModelChecker = (Type)Arguments[0];
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());

			probabilityOfStep11FrozenValue2AndInvariantViolated = modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue2AndInvariantViolated);
			probabilityOfStep11FrozenValue3AndInvariantViolated  =modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue3AndInvariantViolated);
			probabilityOfStep11FrozenValue2AndInvariantNotViolated = modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue2AndInvariantNotViolated);
			probabilityOfStep11FrozenValue3AndInvariantNotViolated = modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue3AndInvariantNotViolated);

			var probabilitiesSummedUp =
				probabilityOfStep11FrozenValue2AndInvariantViolated +
				probabilityOfStep11FrozenValue3AndInvariantViolated +
				probabilityOfStep11FrozenValue2AndInvariantNotViolated +
				probabilityOfStep11FrozenValue3AndInvariantNotViolated;

			probabilitiesSummedUp.Is(1.0, 0.000001).ShouldBe(true);
		}

		private class C : Component
		{
			public int _frozenValue;

			protected internal override void Initialize()
			{
				_frozenValue = Choose(2, 3);
			}

			public readonly Fault F1 = new TransientFault();

			[Range(0, 11, OverflowBehavior.Clamp)]
			public int _timestep;

			public bool ViolateInvariant;

			protected virtual void CriticalStep()
			{
				ViolateInvariant = false;
			}

			public override void Update()
			{
				_timestep++;
				if (_timestep == 10)
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