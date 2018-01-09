using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using Tests.SimpleExecutableModel;

namespace Lustre_Models
{
	public class ModelCheckingTests
	{
		[Test]
		public void TankRupture()
		{
			BachelorarbeitLustre.Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

			Formula invariant = new LustrePressureBelowThreshold();
			LustrePressureBelowThreshold.threshold = 50;
			var faults = new List<Fault>();
			faults.Add(new TransientFault() { Name = "fault_k1", Identifier = 0, ProbabilityOfOccurrence = new Probability(0.1) });
			faults.Add(new TransientFault() { Name = "fault_sensor", Identifier = 1, ProbabilityOfOccurrence = new Probability(0.2) });
			var modelChecker = new LustreQualitativeChecker("pressureTank", faults, invariant);

			modelChecker.CheckInvariant(invariant, 100);
		}
	}
}
