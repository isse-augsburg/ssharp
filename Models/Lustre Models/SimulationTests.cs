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
	public class SimulationTests
	{

		[Test]
		public void TankRupture()
		{
			BachelorarbeitLustre.Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

			Formula invariant = new LustrePressureBelowThreshold();
			Formula hazard = new UnaryFormula(invariant, UnaryOperator.Not);

			LustrePressureBelowThreshold.threshold = 60;

			var faults = new List<Fault>();
			faults.Add(new TransientFault() { Name = "fault_k1", Identifier = 0, ProbabilityOfOccurrence = new Probability(0.1) });
			faults.Add(new TransientFault() { Name = "fault_k2", Identifier = 1, ProbabilityOfOccurrence = new Probability(0.1) });
			faults.Add(new TransientFault() { Name = "fault_sensor", Identifier = 2, ProbabilityOfOccurrence = new Probability(0.2) });

			var simulator = new LustreSimulator("pressureTank", faults, invariant);
			var model = simulator.Model;
			simulator.FastForward(steps: 120);

			Console.WriteLine($"Output= {model.output}");
		}
	}
}
