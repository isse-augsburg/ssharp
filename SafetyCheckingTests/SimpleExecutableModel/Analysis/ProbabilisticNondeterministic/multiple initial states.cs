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
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;

	public class MultipleInitialStates : AnalysisTest
	{
		public MultipleInitialStates(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void Check()
		{
			var m = new Model();
			Probability minProbabilityOfFinally2;
			Probability maxProbabilityOfFinally2;

			var finally2 = new UnaryFormula(new SimpleStateInRangeFormula(2), UnaryOperator.Finally);

			var mdpGenerator = new SimpleMdpFromExecutableModelGenerator(m);
			mdpGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			mdpGenerator.AddFormulaToCheck(finally2);
			var mdp = mdpGenerator.GenerateMarkovDecisionProcess();
			var typeOfModelChecker = typeof(BuiltinMdpModelChecker);
			var modelChecker = (MdpModelChecker)Activator.CreateInstance(typeOfModelChecker, mdp, Output.TextWriterAdapter());
			using (modelChecker)
			{
				minProbabilityOfFinally2 = modelChecker.CalculateMinimalProbability(finally2);
				maxProbabilityOfFinally2 = modelChecker.CalculateMaximalProbability(finally2);
			}

			minProbabilityOfFinally2.Between(0.33, 0.34).ShouldBe(true);
			maxProbabilityOfFinally2.Between(0.33, 0.34).ShouldBe(true);
		}

		private class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];
			
			public override void SetInitialState()
			{
				State = Choice.Choose(1, 2, 3);
			}

			public override void Update()
			{
			}
		}
	}
}