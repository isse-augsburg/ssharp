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
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Simulator;
	using ISSE.SafetyChecking.Utilities;
	using Shouldly;
	using Utilities;
	using Xunit;
	using Xunit.Abstractions;
	
	public class UndoFaultActivationOfFault : AnalysisTest
	{
		// This test case resembles the optimization done by S#'s FaultEffectNormalizer

		public UndoFaultActivationOfFault(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfFinal100;
			Probability probabilityOfFinal200;

			var final100Formula = new UnaryFormula(Model.StateIs100, UnaryOperator.Finally);
			var final200Formula = new UnaryFormula(Model.StateIs200, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(final100Formula);
			markovChainGenerator.AddFormulaToCheck(final200Formula);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFinal100 = modelChecker.CalculateProbability(final100Formula);
				probabilityOfFinal200 = modelChecker.CalculateProbability(final200Formula);
			}

			probabilityOfFinal100.Is(0.6 * 0.8, 0.000001).ShouldBe(true);
			probabilityOfFinal200.Is(0.4 + 0.6 * 0.2, 0.000001).ShouldBe(true);
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
			public override Fault[] Faults { get; } = new Fault[]
			{
				new TransientFault { Identifier = 0, ProbabilityOfOccurrence = new Probability(0.4)}
			};

			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];


			private Fault F1 => Faults[0];

			public override void SetInitialState()
			{
				State = 0;
			}

			public bool BaseMethod()
			{
				return Choice.Choose(
					new Option<bool>(new Probability(0.8), true),
					new Option<bool>(new Probability(0.2), false));
			}

			public bool InnerPossibleFaultMethod()
			{
				F1.TryActivate();
				//!F1.IsActivated is checked first in traversal
				if (!F1.IsActivated)
				{
					var __tmp__ = BaseMethod();
					if (__tmp__ == false)
					{
						//If F1.IsActivated has no effect, do not try it.
						F1.UndoActivation();
					}
					return __tmp__;
				}
				return false;
			}

			public override void Update()
			{
				if (State != 0)
					return;
				if (InnerPossibleFaultMethod())
					State = 100;
				else
					State = 200;
			}

			public static Formula StateIs100 = new SimpleStateInRangeFormula(100);
			public static Formula StateIs200 = new SimpleStateInRangeFormula(200);
		}
	}
}