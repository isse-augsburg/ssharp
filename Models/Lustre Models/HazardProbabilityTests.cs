using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking;
using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using Tests.SimpleExecutableModel;
using LtmcModelChecker = ISSE.SafetyChecking.LtmcModelChecker;

namespace Lustre_Models
{
	public class HazardProbabilityTests
	{
		[Test]
		public void TankRupture()
		{
			BachelorarbeitLustre.Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

			Formula invariant = new LustrePressureBelowThreshold();
			Formula hazard = new UnaryFormula(invariant,UnaryOperator.Not);
			LustrePressureBelowThreshold.threshold = 50;
			var faults = new List<Fault>();
			faults.Add(new TransientFault() { Name = "fault_k1", Identifier = 0, ProbabilityOfOccurrence = new Probability(0.1) });
			faults.Add(new TransientFault() { Name = "fault_sensor", Identifier = 1, ProbabilityOfOccurrence = new Probability(0.2) });
			var modelChecker = new LustreMarkovChainFromExecutableModelGenerator("pressureTank", faults);
			modelChecker.AddFormulaToCheck(hazard);
			modelChecker.Configuration.UseCompactStateStorage = true;
			var lmc = modelChecker.GenerateLabeledMarkovChain();

			var ltmcModelChecker = new BuiltinLtmcModelChecker(lmc, Console.Out);
			var finallyHazard = new BoundedUnaryFormula(hazard,UnaryOperator.Finally, 200);
			var result = ltmcModelChecker.CalculateProbability(finallyHazard);
			
			Console.Write($"Probability of hazard: {result}");
		}
	}
}
