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

	public class TerminateEarly : AnalysisTest
	{
		public TerminateEarly(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void CheckWithoutEarlyTermination()
		{
			var m = new Model();
			Probability probabilityOfFinally3;

			var stateIs3 = new SimpleStateInRangeFormula(3);
			var finally3 = new UnaryFormula(stateIs3, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.AddFormulaToCheck(finally3);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			dtmc.ExportToGv(Output.TextWriterAdapter());
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinally3 = modelChecker.CalculateProbability(finally3);
			}

			probabilityOfFinally3.Between(0.66, 0.67).ShouldBe(true);
		}

		[Fact]
		public void CheckWithEarlyTermination()
		{
			var m = new Model();
			Probability probabilityOfFinally3;

			var stateIs3 = new SimpleStateInRangeFormula(3);
			var finally3 = new UnaryFormula(stateIs3, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.AddFormulaToCheck(finally3);
			var dtmc = markovChainGenerator.GenerateMarkovChain(stateIs3);
			dtmc.ExportToGv(Output.TextWriterAdapter());
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinally3 = modelChecker.CalculateProbability(finally3);
			}

			probabilityOfFinally3.Between(0.66, 0.67).ShouldBe(true);
		}

		private class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];
			
			public override void SetInitialState()
			{
				State = Choice.Choose(1, 3, 7);
			}

			public override void Update()
			{
				if (State<1 || State > 7)
					throw new Exception("Bug: State must be between 1 and 5 in this example");
				if (State < 7)
					State++;
			}
		}
	}
}