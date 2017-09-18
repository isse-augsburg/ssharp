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
	
	public class StateIncreasesFrom5To9 : AnalysisTest
	{
		public StateIncreasesFrom5To9(ITestOutputHelper output = null) : base(output)
		{
		}

		[Fact]
		public void Check()
		{
			var m = new Model();
			Probability probability;

			//var final9Formula = new UnaryFormula(Model.IsInState9, UnaryOperator.Finally);
			var once7Formula = new UnaryFormula(Model.IsInState7, UnaryOperator.Once);
			var is9Once7Formula = new BinaryFormula(Model.IsInState9,BinaryOperator.And, once7Formula);
			var final9Once7Formula = new UnaryFormula(is9Once7Formula, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			markovChainGenerator.Configuration.WriteGraphvizModels = true;
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = true;
			markovChainGenerator.AddFormulaToCheck(final9Once7Formula);
			var dtmc = markovChainGenerator.GenerateMarkovChain(is9Once7Formula);
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probability = modelChecker.CalculateProbability(final9Once7Formula);
			}

			probability.Is(0.4*0.4+0.6, 0.00000001).ShouldBe(true);
		}


		[Fact]
		public void CheckOnceWithNonAtomarOperand()
		{
			var m = new Model();
			Probability probability;
			
			var once7MoreComplex = new BinaryFormula(Model.IsInState7, BinaryOperator.And, Model.IsInState7);
			var once7Formula = new UnaryFormula(once7MoreComplex, UnaryOperator.Once);
			var is9Once7Formula = new BinaryFormula(Model.IsInState9, BinaryOperator.And, once7Formula);
			var final9Once7Formula = new UnaryFormula(is9Once7Formula, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			markovChainGenerator.Configuration.DefaultTraceOutput = Output.TextWriterAdapter();
			markovChainGenerator.Configuration.WriteGraphvizModels = true;
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = true;
			markovChainGenerator.AddFormulaToCheck(final9Once7Formula);
			var dtmc = markovChainGenerator.GenerateMarkovChain(is9Once7Formula);
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probability = modelChecker.CalculateProbability(final9Once7Formula);
			}

			probability.Is(0.4 * 0.4 + 0.6, 0.00000001).ShouldBe(true);
		}

		public class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];

			public override void SetInitialState()
			{
				State = 5;
			}

			public override void Update()
			{
				if (State >= 9)
					return;
				var add = Choice.Choose(
					new Option<int>(new Probability(0.4), 1),
					new Option<int>(new Probability(0.6), 2));
				State += add;
				if (State >= 9)
					State = 9;
			}

			public static Formula IsInState9 = new SimpleStateInRangeFormula(9,"is9");
			public static Formula IsInState7 = new SimpleStateInRangeFormula(7,"is7");
		}
	}
}