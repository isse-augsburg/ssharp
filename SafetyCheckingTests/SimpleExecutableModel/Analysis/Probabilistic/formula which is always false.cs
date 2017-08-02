using System;

namespace Tests.SimpleExecutableModel.Analysis.Probabilistic
{
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

		[Fact(Skip = "Not Implemented, yet")]
		public void Check()
		{
			var m = new Model();
			Probability probabilityOfFalse;
			
			Formula falseFormula = false;
			var finallyFalseFormula = new UnaryFormula(falseFormula, UnaryOperator.Finally);

			var markovChainGenerator = new SimpleMarkovChainFromExecutableModelGenerator(m);
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.AddFormulaToCheck(finallyFalseFormula);
			var dtmc = markovChainGenerator.GenerateMarkovChain();
			var typeOfModelChecker = typeof(BuiltinDtmcModelChecker);
			var modelChecker = (DtmcModelChecker)Activator.CreateInstance(typeOfModelChecker, dtmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFalse = modelChecker.CalculateProbability(finallyFalseFormula);
			}

			probabilityOfFalse.Is(0.0, 0.001).ShouldBe(true);
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
