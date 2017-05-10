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
	using System;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;
	

	public class SimpleExample1a : AnalysisTest
	{
		public SimpleExample1a(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void CheckMdp()
		{
			var m = new Model();
			Probability minProbabilityOfFinal1;
			Probability minProbabilityOfFinal2;
			Probability minProbabilityOfFinal3;
			Probability maxProbabilityOfFinal1;
			Probability maxProbabilityOfFinal2;
			Probability maxProbabilityOfFinal3;

			var final1Formula = new UnaryFormula(new SimpleStateInRangeFormula(1), UnaryOperator.Finally);
			var final2Formula = new UnaryFormula(new SimpleStateInRangeFormula(2), UnaryOperator.Finally);
			var final3Formula = new UnaryFormula(new SimpleStateInRangeFormula(3), UnaryOperator.Finally);

			var nmdpGenerator = new SimpleNmdpFromExecutableModelGenerator(m);
			nmdpGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			nmdpGenerator.AddFormulaToCheck(final1Formula);
			nmdpGenerator.AddFormulaToCheck(final2Formula);
			nmdpGenerator.AddFormulaToCheck(final3Formula);
			var nmdp = nmdpGenerator.GenerateMarkovDecisionProcess();
			nmdp.ExportToGv(Output.TextWriterAdapter());
			var nmdpToMpd = new NmdpToMdp(nmdp);
			var mdp = nmdpToMpd.MarkovDecisionProcess;
			var typeOfModelChecker = typeof(BuiltinMdpModelChecker);
			var modelChecker = (MdpModelChecker)Activator.CreateInstance(typeOfModelChecker, mdp, Output.TextWriterAdapter());
			using (modelChecker)
			{
				minProbabilityOfFinal1 = modelChecker.CalculateMinimalProbability(final1Formula);
				minProbabilityOfFinal2 = modelChecker.CalculateMinimalProbability(final2Formula);
				minProbabilityOfFinal3 = modelChecker.CalculateMinimalProbability(final3Formula);
				maxProbabilityOfFinal1 = modelChecker.CalculateMaximalProbability(final1Formula);
				maxProbabilityOfFinal2 = modelChecker.CalculateMaximalProbability(final2Formula);
				maxProbabilityOfFinal3 = modelChecker.CalculateMaximalProbability(final3Formula);
			}

			minProbabilityOfFinal1.Is(1.0, 0.000001).ShouldBe(true);
			minProbabilityOfFinal2.Is(0.0, 0.000001).ShouldBe(true);
			minProbabilityOfFinal3.Is(0.0, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal1.Is(1.0, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal2.Is(1.0, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal3.Is(1.0, 0.000001).ShouldBe(true);
		}

		[Fact(Skip = "NotImplementedYet")]
		public void CheckNmdp()
		{
			var m = new Model();
			Probability minProbabilityOfFinal1;
			Probability minProbabilityOfFinal2;
			Probability minProbabilityOfFinal3;
			Probability maxProbabilityOfFinal1;
			Probability maxProbabilityOfFinal2;
			Probability maxProbabilityOfFinal3;

			var final1Formula = new UnaryFormula(new SimpleStateInRangeFormula(1), UnaryOperator.Finally);
			var final2Formula = new UnaryFormula(new SimpleStateInRangeFormula(2), UnaryOperator.Finally);
			var final3Formula = new UnaryFormula(new SimpleStateInRangeFormula(3), UnaryOperator.Finally);

			var nmdpGenerator = new SimpleNmdpFromExecutableModelGenerator(m);
			nmdpGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			nmdpGenerator.AddFormulaToCheck(final1Formula);
			nmdpGenerator.AddFormulaToCheck(final2Formula);
			nmdpGenerator.AddFormulaToCheck(final3Formula);
			var nmdp = nmdpGenerator.GenerateMarkovDecisionProcess();
			nmdp.ExportToGv(Output.TextWriterAdapter());
			var typeOfModelChecker = typeof(BuiltinNmdpModelChecker);
			var modelChecker = (NmdpModelChecker)Activator.CreateInstance(typeOfModelChecker, nmdp, Output.TextWriterAdapter());
			using (modelChecker)
			{
				minProbabilityOfFinal1 = modelChecker.CalculateMinimalProbability(final1Formula);
				minProbabilityOfFinal2 = modelChecker.CalculateMinimalProbability(final2Formula);
				minProbabilityOfFinal3 = modelChecker.CalculateMinimalProbability(final3Formula);
				maxProbabilityOfFinal1 = modelChecker.CalculateMaximalProbability(final1Formula);
				maxProbabilityOfFinal2 = modelChecker.CalculateMaximalProbability(final2Formula);
				maxProbabilityOfFinal3 = modelChecker.CalculateMaximalProbability(final3Formula);
			}

			minProbabilityOfFinal1.Is(1.0, 0.000001).ShouldBe(true);
			minProbabilityOfFinal2.Is(0.0, 0.000001).ShouldBe(true);
			minProbabilityOfFinal3.Is(0.0, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal1.Is(1.0, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal2.Is(1.0, 0.000001).ShouldBe(true);
			maxProbabilityOfFinal3.Is(1.0, 0.000001).ShouldBe(true);
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