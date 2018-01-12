using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using SafetyLustre;

namespace Lustre_Models
{
	public class ModelCheckingTests
	{
		[Test]
		public void TankRupture()
		{
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

			Formula invariant = new LustrePressureBelowThreshold();
			LustrePressureBelowThreshold.threshold = 60;
			var faults = new List<Fault>();
			faults.Add(new TransientFault() { Name = "fault_switch", Identifier = 0, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = new Probability(3.0E-6) });
			faults.Add(new PermanentFault() { Name = "fault_timer", Identifier = 3, ProbabilityOfOccurrence = new Probability(1.0E-5) });
			faults.Add(new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) });
			var modelChecker = new LustreQualitativeChecker("pressureTank", faults, invariant);

			modelChecker.CheckInvariant(invariant, 100);
		}
	}
}
