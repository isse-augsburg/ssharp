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

	public class CustomProbabilityOfTransientFault  : AnalysisTest
	{
		public CustomProbabilityOfTransientFault(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfInvariantViolation;

			Formula invariantViolated = new SimpleLocalVarIsTrue(0);
			var finallyInvariantViolated = new UnaryFormula(invariantViolated,UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(finallyInvariantViolated);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfInvariantViolation = modelChecker.CalculateProbability(finallyInvariantViolated);
			}

			probabilityOfInvariantViolation.Is(0.01, 0.0001).ShouldBe(true);
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
			public override Fault[] Faults { get; } = new Fault[] { new TransientFault { Identifier = 0, ProbabilityOfOccurrence = new Probability(0.01) } };
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

			public override void Update()
			{
				ViolateInvariant = false;
				if (State == 0)
				{
					CriticalStep();
				}
				State = 1;
			}
		}
	}
}