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

namespace Tests.SimpleExecutableModel.Analysis.Probabilistic
{
	using System;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;

	public class PermanentFaultLeadsToInvariantViolationOnlyInSpecificStep : AnalysisTest
	{
		public PermanentFaultLeadsToInvariantViolationOnlyInSpecificStep(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		protected void Check()
		{
			var m = new Model();
			Probability probabilityOfInvariantViolation;
			
			var finallyInvariantViolated = new UnaryFormula(Model.InvariantViolated, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleDtmcFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.AddFormulaToCheck(finallyInvariantViolated);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfInvariantViolation = modelChecker.CalculateProbability(finallyInvariantViolated);
			}

			// 1.0-(1.0-0.1)^11 = 0.68618940391
			probabilityOfInvariantViolation.Is(0.68618940391, 0.00001).ShouldBe(true);
		}

		private class Model : SimpleModelBase
		{
			private Fault F1 => Faults[0];

			private bool ViolateInvariant
			{
				get { return LocalBools[0]; }
				set { LocalBools[0]=value; }
			}

			public override Fault[] Faults { get; } = { new PermanentFault { ProbabilityOfOccurrence = new Probability(0.1) } };
			public override bool[] LocalBools { get; } = new bool[] {false};
			public override int[] LocalInts { get; } = new int[0];
			
			private void CriticalStep()
			{
				if (F1.IsActivated)
					ViolateInvariant = true;
				else
					ViolateInvariant = false;
			}

			public override void Update()
			{
				if (State == 11)
					return;
				State++;
				if (State == 10)
					CriticalStep();
			}
			
			public static readonly Formula InvariantViolated = new SimpleLocalVarIsTrue(0);
		}
	}

}