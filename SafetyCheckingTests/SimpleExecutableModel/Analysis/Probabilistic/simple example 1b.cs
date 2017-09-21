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
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	

	public class SimpleExample1b : AnalysisTest
	{
		public SimpleExample1b(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfFinal1;
			Probability probabilityOfFinal2;
			Probability probabilityOfFinal3;

			var final1Formula = new UnaryFormula(new SimpleStateInRangeFormula(1), UnaryOperator.Finally);
			var final2Formula = new UnaryFormula(new SimpleStateInRangeFormula(2), UnaryOperator.Finally);
			var final3Formula = new UnaryFormula(new SimpleStateInRangeFormula(3), UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(final1Formula);
			markovChainGenerator.AddFormulaToCheck(final2Formula);
			markovChainGenerator.AddFormulaToCheck(final3Formula);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal1 = modelChecker.CalculateProbability(final1Formula);
				probabilityOfFinal2 = modelChecker.CalculateProbability(final2Formula);
				probabilityOfFinal3 = modelChecker.CalculateProbability(final3Formula);
			}

			probabilityOfFinal1.Is(0.4, 0.000001).ShouldBe(true);
			probabilityOfFinal2.Is(0.3, 0.000001).ShouldBe(true);
			probabilityOfFinal3.Is(0.3, 0.000001).ShouldBe(true);
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

		[Fact]
		public void CheckBuiltinLtmc()
		{
			var configuration = AnalysisConfiguration.Default;
			configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			configuration.WriteGraphvizModels = true;
			configuration.UseCompactStateStorage = true;
			configuration.LtmcModelChecker = ISSE.SafetyChecking.LtmcModelChecker.BuiltInLtmc;
			Check(configuration);
		}

		public class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = { false };
			public override int[] LocalInts { get; } = new int[0];

			private bool L
			{
				get { return LocalBools[0]; }
				set { LocalBools[0] = value; }
			}

			private int Y
			{
				get { return State; }
				set { State = value; }
			}

			public override void SetInitialState()
			{
				State = 0;
			}

			public override void Update()
			{
				if (Y != 0)
					return;
				L = Choice.Choose(
					new Option<bool>(new Probability(0.6), true),
					new Option<bool>(new Probability(0.4), false));
				Y = 1;
				if (L)
				{
					Y = Choice.Choose(2, 3);
				}
			}
		}
	}
}