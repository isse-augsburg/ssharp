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
using SafetyLustre;
using LtmcModelChecker = ISSE.SafetyChecking.LtmcModelChecker;

namespace Lustre_Models
{
	public class HazardProbabilityTests
	{
		[Test]
		public void TankRupture()
		{
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

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

		[Test]
		public void Parametric()
		{
			Program.ocExaplesPath = Directory.GetCurrentDirectory() + "\\";

			Formula invariant = new LustrePressureBelowThreshold();
			Formula hazard = new UnaryFormula(invariant, UnaryOperator.Not);
			LustrePressureBelowThreshold.threshold = 60;
			var faultK1 = new PermanentFault() { Name = "fault_k1", Identifier = 1, ProbabilityOfOccurrence = new Probability(3.0E-6) };
			var faultK2 = new PermanentFault() { Name = "fault_k2", Identifier = 2, ProbabilityOfOccurrence = new Probability(3.0E-6) };
			var faultSensor = new PermanentFault() { Name = "fault_sensor", Identifier = 4, ProbabilityOfOccurrence = new Probability(1.0E-5) };
			var faults = new []{ faultK1, faultK2, faultSensor };
			
			var parameter = new QuantitativeParametricAnalysisParameter
			{
				StateFormula = hazard,
				Bound = null,
				From = 3.0E-7,
				To = 3.0E-5,
				Steps = 25,
				UpdateParameterInModel = value => { faultK1.ProbabilityOfOccurrence=new Probability(value); }
			};
			var result = LustreModelChecker.ConductQuantitativeParametricAnalysis("pressureTank", faults, parameter);
			var fileWriter = new StreamWriter("pressureTank_varyK1", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();

			parameter.UpdateParameterInModel = value => { faultK2.ProbabilityOfOccurrence = new Probability(value); };
			result = LustreModelChecker.ConductQuantitativeParametricAnalysis("pressureTank", faults, parameter);
			fileWriter = new StreamWriter("pressureTank_varyK2", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();

			parameter.UpdateParameterInModel = value => { faultSensor.ProbabilityOfOccurrence = new Probability(value); };
			result = LustreModelChecker.ConductQuantitativeParametricAnalysis("pressureTank", faults, parameter);
			fileWriter = new StreamWriter("pressureTank_varySensor", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();
		}
	}
}
