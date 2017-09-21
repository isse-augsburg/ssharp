using System;

namespace Tests.SimpleExecutableModel.Analysis.Probabilistic
{
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;
	using Xunit;
	using Xunit.Abstractions;

	public class FormulaWhichIsAlwaysFalse : AnalysisTest
	{
		public FormulaWhichIsAlwaysFalse(ITestOutputHelper output = null) : base(output)
		{
		}

		private void Check(AnalysisConfiguration configuration)
		{
			var m = new Model();
			Probability probabilityOfFalse;
			
			Formula falseFormula = new BinaryFormula(new SimpleStateInRangeFormula(1), BinaryOperator.And, new SimpleStateInRangeFormula(2));
			var finallyFalseFormula = new UnaryFormula(falseFormula, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration = configuration;
			markovChainGenerator.AddFormulaToCheck(finallyFalseFormula);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFalse = modelChecker.CalculateProbability(finallyFalseFormula);
			}

			probabilityOfFalse.Is(0.0, 0.001).ShouldBe(true);
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

		private class Model : SimpleModelBase
		{
			public override Fault[] Faults { get; } = new Fault[0];
			public override bool[] LocalBools { get; } = new bool[0];
			public override int[] LocalInts { get; } = new int[0];

			public override void SetInitialState()
			{
				State = Choice.Choose(0, 1);
			}
			
			public override void Update()
			{
				if (State >= 4)
					return;
				State++;
			}

		}
	}
}
