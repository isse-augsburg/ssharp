using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking;
using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using SafetyLustre;

namespace Lustre_Models
{
	class EvaluationTests
	{
		private ExecutableModelCreator<LustreExecutableModel> _createModel;

		private Formula _invariant = new LustrePressureBelowThreshold();

		private Formula _hazard = new UnaryFormula(new LustrePressureBelowThreshold(), UnaryOperator.Not);

		private Fault[] _faults;

		public EvaluationTests()
		{
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";
			LustrePressureBelowThreshold.threshold = 60;
			var faultK1 = new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) };
			var faultK2 = new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = new Probability(3.0E-6) };
			var faultSensor = new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) };
			_faults = new[] { faultK1, faultK2, faultSensor };

			_createModel = LustreExecutableModel.CreateExecutedModelFromFormulasCreator("pressureTank", _faults);
		}
		

		[Test]
		public void CreateMarkovChainWithFalseFormula()
		{
			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new BinaryFormula(_invariant, BinaryOperator.And,
				new UnaryFormula(_invariant, UnaryOperator.Not)));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazards()
		{
			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			var result = LustreModelChecker.CalculateProbabilityToReachStateBounded("pressureTank", _faults, _hazard, 25);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardRetraversal1()
		{
			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(_hazard);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(_hazard);
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardsRetraversal2()
		{
			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(_hazard);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(_hazard);
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}


		[Test]
		public void CreateMarkovChainWithHazardFaultsInState()
		{
			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			var result = LustreModelChecker.CalculateProbabilityToReachStateBounded("pressureTank", _faults, _hazard, 25);
			foreach (var fault in _faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
				markovChainGenerator.AddFormulaToPlainlyIntegrateIntoStateSpace(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}


		[Test]
		public void CreateFaultAwareMarkovChainAllFaults()
		{
			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(_createModel) { Configuration = LustreModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			foreach (var fault in _faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			var result = LustreModelChecker.CalculateProbabilityToReachStateBounded("pressureTank", _faults, _hazard, 25);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CalculateHazardSingleCore()
		{
			LustreModelChecker.TraversalConfiguration.CpuCount = 1;
			var result = LustreModelChecker.CalculateProbabilityToReachStateBounded("pressureTank", _faults, _hazard, 25);
			LustreModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			Console.Write($"Probability of hazard: {result}");
		}



		[Test]
		public void CalculateHazardWithoutEarlyTermination()
		{
			LustreModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = LustreModelChecker.CalculateProbabilityToReachStateBounded("pressureTank", _faults, _hazard, 25);
			LustreModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}
	}
}
