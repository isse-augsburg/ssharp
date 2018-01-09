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
			var faults = new Dictionary<string, Fault>();
			var modelChecker = new LustreQualitativeChecker("pressureTank", faults, invariant);

			modelChecker.CheckInvariant(invariant, 100);
		}
	}
}
