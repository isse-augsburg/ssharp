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

	// Knuth's die. Described e.g. on http://wwwhome.cs.utwente.nl/~timmer/scoop/casestudies/knuth/knuth.html

	public class EmulateDiceWithCoin : AnalysisTest
	{
		public EmulateDiceWithCoin(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfFinal1;

			var final1Formula = new UnaryFormula(Model.IsInStateFinal1, UnaryOperator.Finally);
			
			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(final1Formula);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal1 = modelChecker.CalculateProbability(final1Formula);
			}

			probabilityOfFinal1.Between(0.1, 0.2).ShouldBe(true);
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

		[Fact]
		public void Simulate()
		{
			var m = new Model();

			//var final1Formula = new UnaryFormula(Model.IsInStateFinal1, UnaryOperator.Finally);

			var simulator = new SimpleProbabilisticSimulator(m, Model.IsInStateFinal1, Model.IsInStateInitialThrow);
			simulator.SimulateSteps(100);

			var initialThrowCount = simulator.GetCountOfSatisfiedOnTrace(Model.IsInStateInitialThrow);
			var inStateFinal1Count = simulator.GetCountOfSatisfiedOnTrace(Model.IsInStateFinal1);
			initialThrowCount.ShouldBe(1);
			inStateFinal1Count.ShouldBeLessThan(100);
			//probabilityOfFinal1.Between(0.0, 0.2).ShouldBe(true);
		}

		private enum S
		{
			InitialThrow = 0,
			Throw1To3 = 1,
			Throw4To6 = 2,
			Throw1Or2 = 3,
			Throw3OrRethrow = 4,
			Throw4Or5 = 5,
			Throw6OrRethrow = 6,
			Final1 = 7,
			Final2 = 8,
			Final3 = 9,
			Final4 = 10,
			Final5 = 11,
			Final6 = 12,
		}

		public class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];

			public override void SetInitialState()
			{
				State = (int)S.InitialThrow;
			}

			public override void Update()
			{
				var s = (S)State;
				switch (s)
				{
					case S.InitialThrow:
						s = Choice.Choose(S.Throw1To3, S.Throw4To6);
						break;
					case S.Throw1To3:
						s = Choice.Choose(S.Throw1Or2, S.Throw3OrRethrow);
						break;
					case S.Throw4To6:
						s = Choice.Choose(S.Throw4Or5, S.Throw6OrRethrow);
						break;
					case S.Throw1Or2:
						s = Choice.Choose(S.Final1, S.Final2);
						break;
					case S.Throw3OrRethrow:
						s = Choice.Choose(S.Final3, S.Throw1To3);
						break;
					case S.Throw4Or5:
						s = Choice.Choose(S.Final4, S.Final5);
						break;
					case S.Throw6OrRethrow:
						s = Choice.Choose(S.Final6, S.Throw4To6);
						break;
					case S.Final1:
						break;
					case S.Final2:
						break;
					case S.Final3:
						break;
					case S.Final4:
						break;
					case S.Final5:
						break;
					case S.Final6:
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				State = (int)s;
			}

			public static Formula IsInStateFinal1 = new SimpleStateInRangeFormula((int)S.Final1);

			public static Formula IsInStateInitialThrow = new SimpleStateInRangeFormula((int)S.InitialThrow);
		}
	}
}