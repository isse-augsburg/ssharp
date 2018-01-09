using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISSE.SafetyChecking.Formula;
using NUnit.Framework;
using Tests.SimpleExecutableModel;
using ISSE.SafetyChecking.Modeling;

namespace Lustre_Models
{
	public class SafetyAnalysisTests
	{

		[Test]
		public void TankRupture()
		{
			BachelorarbeitLustre.Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

			Formula invariant = new LustrePressureBelowThreshold();
			LustrePressureBelowThreshold.threshold = 50;

			var faults = new Dictionary<string, Fault>();
			var result = LustreSafetyAnalysis.AnalyzeHazard("pressureTank", faults, invariant);
		}
	}
}
