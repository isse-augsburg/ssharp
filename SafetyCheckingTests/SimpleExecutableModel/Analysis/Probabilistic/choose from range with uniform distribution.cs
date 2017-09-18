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
	using System.ComponentModel;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;

	public class ChooseFromRangeWithUniformDistribution : AnalysisTest
	{
		public ChooseFromRangeWithUniformDistribution(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfFinal2;
			Probability probabilityOfFinal3;
			Probability probabilityOfFinal4;

			var final2 = new UnaryFormula(new SimpleStateInRangeFormula(2), UnaryOperator.Finally);
			var final3 = new UnaryFormula(new SimpleStateInRangeFormula(3), UnaryOperator.Finally);
			var final4 = new UnaryFormula(new SimpleStateInRangeFormula(4), UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(final2);
			markovChainGenerator.AddFormulaToCheck(final3);
			markovChainGenerator.AddFormulaToCheck(final4);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration,ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal2 = modelChecker.CalculateProbability(final2);
				probabilityOfFinal3 = modelChecker.CalculateProbability(final3);
				probabilityOfFinal4 = modelChecker.CalculateProbability(final4);
			}

			probabilityOfFinal2.Is(1.0 / 3, tolerance: 0.0001).ShouldBe(true);
			probabilityOfFinal3.Is(1.0 / 3, tolerance: 0.0001).ShouldBe(true);
			probabilityOfFinal3.Is(1.0 / 3, tolerance: 0.0001).ShouldBe(true);
		}

		[Fact]
		public void CheckBuiltinDtmc()
		{
			var configuration = AnalysisConfiguration.Default;
			configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			configuration.WriteGraphvizModels = true;
			configuration.LtmdpModelChecker = ISSE.SafetyChecking.LtmdpModelChecker.BuildInMdp;
			Check(configuration);
		}

		private class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];

			public override void Update()
			{
				if (State == 0)
				{
					State = Choice.ChooseFromRangeWithUniformDistribution(2,4);
				}
			}
		}

	}
}