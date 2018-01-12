using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking.Formula;
using NUnit.Framework;
using ISSE.SafetyChecking.Modeling;
using SafetyLustre;

namespace Lustre_Models
{
	public class SafetyAnalysisTests
	{

		[Test]
		public void TankRupture()
		{
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

			Formula invariant = new LustrePressureBelowThreshold();
			Formula hazard = new UnaryFormula(invariant, UnaryOperator.Not);

			LustrePressureBelowThreshold.threshold = 60;

			var faults = new List<Fault>();
			faults.Add(new TransientFault() { Name = "fault_switch", Identifier = 0, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_timer", Identifier = 3, ProbabilityOfOccurrence = new Probability(1.0E-5) });
			faults.Add(new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) });
			var result = LustreSafetyAnalysis.AnalyzeHazard("pressureTank", faults, hazard);
			Console.WriteLine($"Minimal Critical Sets: {result.MinimalCriticalSets.Count}");
		}
	}
}
