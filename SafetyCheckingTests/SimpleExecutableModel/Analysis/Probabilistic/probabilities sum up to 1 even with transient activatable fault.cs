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
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;

	public class ProbabilitiesSumUpTo1EvenWithTransienttActivatableFault : AnalysisTest
	{
		public ProbabilitiesSumUpTo1EvenWithTransienttActivatableFault(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfStep11FrozenValue0AndInvariantViolated;
			Probability probabilityOfStep11FrozenValue1AndInvariantViolated;
			Probability probabilityOfStep11FrozenValue0AndInvariantNotViolated;
			Probability probabilityOfStep11FrozenValue1AndInvariantNotViolated;


			Formula formulaProbabilityOfStep11FrozenValue0AndInvariantViolated = new BinaryFormula(new SimpleStateInRangeFormula(10), BinaryOperator.And, new SimpleLocalVarIsTrue(0));
			Formula formulaProbabilityOfStep11FrozenValue1AndInvariantViolated = new BinaryFormula(new SimpleStateInRangeFormula(10 + 128), BinaryOperator.And, new SimpleLocalVarIsTrue(0));
			Formula formulaProbabilityOfStep11FrozenValue0AndInvariantNotViolated = new BinaryFormula(new SimpleStateInRangeFormula(10), BinaryOperator.And, new UnaryFormula(new SimpleLocalVarIsTrue(0), UnaryOperator.Not));
			Formula formulaProbabilityOfStep11FrozenValue1AndInvariantNotViolated = new BinaryFormula(new SimpleStateInRangeFormula(10 + 128), BinaryOperator.And, new UnaryFormula(new SimpleLocalVarIsTrue(0), UnaryOperator.Not));

			var finallyFormulaProbabilityOfStep11FrozenValue0AndInvariantViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue0AndInvariantViolated, UnaryOperator.Finally);
			var finallyFormulaProbabilityOfStep11FrozenValue1AndInvariantViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue1AndInvariantViolated, UnaryOperator.Finally);
			var finallyFormulaProbabilityOfStep11FrozenValue0AndInvariantNotViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue0AndInvariantNotViolated, UnaryOperator.Finally);
			var finallyFormulaProbabilityOfStep11FrozenValue1AndInvariantNotViolated = new UnaryFormula(formulaProbabilityOfStep11FrozenValue1AndInvariantNotViolated, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue0AndInvariantViolated);
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue1AndInvariantViolated);
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue0AndInvariantNotViolated);
			markovChainGenerator.AddFormulaToCheck(finallyFormulaProbabilityOfStep11FrozenValue1AndInvariantNotViolated);

			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration, ltmc, Output.TextWriterAdapter());
			{
				probabilityOfStep11FrozenValue0AndInvariantViolated = modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue0AndInvariantViolated);
				probabilityOfStep11FrozenValue1AndInvariantViolated = modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue1AndInvariantViolated);
				probabilityOfStep11FrozenValue0AndInvariantNotViolated = modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue0AndInvariantNotViolated);
				probabilityOfStep11FrozenValue1AndInvariantNotViolated = modelChecker.CalculateProbability(finallyFormulaProbabilityOfStep11FrozenValue1AndInvariantNotViolated);
			}

			var probabilitiesSummedUp =
				probabilityOfStep11FrozenValue0AndInvariantViolated +
				probabilityOfStep11FrozenValue1AndInvariantViolated +
				probabilityOfStep11FrozenValue0AndInvariantNotViolated +
				probabilityOfStep11FrozenValue1AndInvariantNotViolated;

			probabilitiesSummedUp.Is(1.0, 0.000001).ShouldBe(true);
		}

		[Fact]
		public void CheckBuiltinDtmc()
		{
			var configuration = AnalysisConfiguration.Default;
			configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			configuration.WriteGraphvizModels = true;
			configuration.LtmcModelChecker = ISSE.SafetyChecking.LtmcModelChecker.BuiltInDtmc;
			Check(configuration);
		}

		private class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[] { new TransientFault { Identifier = 0, ProbabilityOfOccurrence = new Probability(0.5) } };
			public override bool[] LocalBools { get; } = new bool[] { false };
			public override int[] LocalInts { get; } = new int[0];

			private Fault F1 => Faults[0];

			private bool ViolateInvariant
			{
				get { return LocalBools[0]; }
				set { LocalBools[0] = value; }
			}
			
			protected virtual void CriticalStep()
			{
				F1.TryActivate();

				if (F1.IsActivated)
					ViolateInvariant = true;
			}

			public override void SetInitialState()
			{
				// frozenValue is set as bit
				State = Choice.Choose(0, 128);
			}

			private int TimeStep => State & 127;

			public override void Update()
			{
				ViolateInvariant = false;
				if (TimeStep == 11)
					return;

				State++;
				if (TimeStep == 10)
					CriticalStep();
			}
		}
	}
}