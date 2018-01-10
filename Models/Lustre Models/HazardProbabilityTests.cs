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
			LustrePressureBelowThreshold.threshold = 60;
			var faults = new List<Fault>();
			faults.Add(new TransientFault() { Name = "fault_switch", Identifier = 0, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_timer", Identifier = 3, ProbabilityOfOccurrence = new Probability(1.0E-5) });
			faults.Add(new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) });
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
