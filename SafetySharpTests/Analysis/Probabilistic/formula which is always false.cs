using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.Analysis.Probabilistic
{
	using SafetySharp.Analysis;
	using SafetySharp.ModelChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class FormulaWhichIsAlwaysFalse : ProbabilisticAnalysisTestObject
	{


		protected override void Check()
		{
			var c = new C();
			Probability probabilityOfFalse;
			
			Formula falseFormula = false;
			var finallyFalseFormula = new UnaryFormula(falseFormula, UnaryOperator.Finally);

			var markovChainGenerator = new SafetySharpMarkovChainFromExecutableModelGenerator(TestModel.InitializeModel(c));
			markovChainGenerator.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			markovChainGenerator.Configuration.LtmcModelChecker = (ISSE.SafetyChecking.LtmcModelChecker)Arguments[0];
			markovChainGenerator.AddFormulaToCheck(finallyFalseFormula);
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, ltmc, Output.TextWriterAdapter());
			using (modelChecker)
			{
				probabilityOfFalse = modelChecker.CalculateProbability(finallyFalseFormula);
			}

			probabilityOfFalse.Is(0.0, 0.001).ShouldBe(true);
		}

		private class C : Component
		{
			[Range(0, 4, OverflowBehavior.Clamp)]
			private int _value;

			protected internal override void Initialize()
			{
				_value = Choose(0, 1);
			}


			public override void Update()
			{
				_value++;
			}

		}
	}
}
