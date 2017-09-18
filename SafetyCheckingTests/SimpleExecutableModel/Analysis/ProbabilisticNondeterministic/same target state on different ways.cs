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


namespace Tests.SimpleExecutableModel.Analysis.ProbabilisticNondeterministic
{
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using System;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;

	public class SameTargetStateOnDifferentWays : AnalysisTest
	{
		public SameTargetStateOnDifferentWays(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		protected void CheckMdp()
		{
			var m = new Model();
			Probability minProbabilityOfFinal1;
			Probability maxProbabilityOfFinal1;

			var finally1 = new UnaryFormula(Model.StateIs1, UnaryOperator.Finally);

			var nmdpGenerator = new SimpleMarkovDecisionProcessFromExecutableModelGenerator(m);
			nmdpGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			nmdpGenerator.AddFormulaToCheck(finally1);
			var nmdp = nmdpGenerator.GenerateMarkovDecisionProcess();
			nmdp.ExportToGv(Output.TextWriterAdapter());
			Output.Log("");
			var nmdpToMpd = new NmdpToMdp(nmdp);
			var mdp = nmdpToMpd.MarkovDecisionProcess;
			mdp.ExportToGv(Output.TextWriterAdapter());
			var typeOfModelChecker = typeof(BuiltinMdpModelChecker);
			var modelChecker = (MdpModelChecker)Activator.CreateInstance(typeOfModelChecker, mdp, Output.TextWriterAdapter());
			using (modelChecker)
			{
				minProbabilityOfFinal1 = modelChecker.CalculateMinimalProbability(finally1);
				maxProbabilityOfFinal1 = modelChecker.CalculateMaximalProbability(finally1);
			}

			minProbabilityOfFinal1.Is(0.65, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal1.Is(0.65, 0.000001).ShouldBe(true);
		}

		[Fact(Skip = "NotImplementedYet")]
		protected void CheckNmdp()
		{
			var m = new Model();
			Probability minProbabilityOfFinal1;
			Probability maxProbabilityOfFinal1;

			var finally1 = new UnaryFormula(Model.StateIs1, UnaryOperator.Finally);

			var nmdpGenerator = new SimpleMarkovDecisionProcessFromExecutableModelGenerator(m);
			nmdpGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			nmdpGenerator.AddFormulaToCheck(finally1);
			var nmdp = nmdpGenerator.GenerateMarkovDecisionProcess();
			var typeOfModelChecker = typeof(BuiltinNmdpModelChecker);
			var modelChecker = (NmdpModelChecker)Activator.CreateInstance(typeOfModelChecker, nmdp, Output.TextWriterAdapter());
			using (modelChecker)
			{
				minProbabilityOfFinal1 = modelChecker.CalculateMinimalProbability(finally1);
				maxProbabilityOfFinal1 = modelChecker.CalculateMaximalProbability(finally1);
			}

			minProbabilityOfFinal1.Is(0.65, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal1.Is(0.65, 0.000001).ShouldBe(true);
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
