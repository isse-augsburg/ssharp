using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using SafetyLustre;
using LtmcModelChecker = ISSE.SafetyChecking.LtmcModelChecker;
using LtmdpModelChecker = ISSE.SafetyChecking.LtmdpModelChecker;

namespace Lustre_Models
{
	public class HazardProbabilityRangeTests
	{
		[Test]
		public void TankRupture()
		{
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";
			Program.modelChecking = true;

			Formula invariant = new LustrePressureBelowThreshold();
			Formula hazard = new UnaryFormula(invariant,UnaryOperator.Not);
			LustrePressureBelowThreshold.threshold = 60;
			var faults = new List<Fault>();
			faults.Add(new TransientFault() { Name = "fault_switch", Identifier = 0, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = null });
			faults.Add(new PermanentFault() { Name = "fault_timer", Identifier = 3, ProbabilityOfOccurrence = new Probability(1.0E-5) });
			faults.Add(new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) });

			var createModel = LustreExecutableModel.CreateExecutedModelFromFormulasCreator("pressureTank", faults.ToArray());

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(createModel);
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = true;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.AddFormulaToCheck(hazard);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();

			var ltmcModelChecker = new ConfigurationDependentLtmdpModelChecker(markovChainGenerator.Configuration, markovChain, Console.Out);
			var finallyHazard = new BoundedUnaryFormula(hazard,UnaryOperator.Finally, 200);
			var result = ltmcModelChecker.CalculateProbabilityRange(finallyHazard);
			
			Console.Write($"Probability of hazard: {result}");
		}
	}
}
