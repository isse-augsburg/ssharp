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

	public class SameTargetStateOnDifferentWays : AnalysisTest
	{
		public SameTargetStateOnDifferentWays(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfFinal1;
			
			var finally1 = new UnaryFormula(Model.StateIs1, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(finally1);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal1 = modelChecker.CalculateProbability(finally1);
			}

			probabilityOfFinal1.Is(0.65, 0.000001).ShouldBe(true);
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
					if (Choice.Choose(new Option<bool>(new Probability(0.1), true),
							   new Option<bool>(new Probability(0.9), false)))
					{
						if (Choice.Choose(new Option<bool>(new Probability(0.2), true),
							   new Option<bool>(new Probability(0.8), false)))
						{
							//way 1
							State = 1;
						}
						else
						{
							State = 2;
						}
					}
					else
					{
						if (Choice.Choose(new Option<bool>(new Probability(0.3), true),
							   new Option<bool>(new Probability(0.7), false)))
						{
							State = 3;
						}
						else
						{
							//way 2
							State = 1;
						}
					}
				}
			}

			public static Formula StateIs1 = new SimpleStateInRangeFormula(1);
		}
	}
}
