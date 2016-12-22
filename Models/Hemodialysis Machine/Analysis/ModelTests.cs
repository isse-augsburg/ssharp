// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace SafetySharp.CaseStudies.HemodialysisMachine.Analysis
{
	using System;
	using System.Linq;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using FluentAssertions;
	using ModelChecking;

	public class ModelTests
	{
		[Test]
		public void Simulate_1Step()
		{
			var testModel = new Model();
			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (DialyzingFluidFlowCombinator)simulator.Model.Roots[0];
		}

		[Test]
		public void Simulate_10_Step()
		{
			var testModel = new Model();
			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (DialyzingFluidFlowCombinator)simulator.Model.Roots[0];
		}

		[Test]
		public void Simulate_10_Step_DialyzerRuptured()
		{
			var testModel = new Model();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			var patient = simulator.Model.Roots.OfType<Patient>().First();
			var hdMachine = simulator.Model.Roots.OfType<HdMachine>().First();
			hdMachine.Dialyzer.DialyzerMembraneRupturesFault.Activation = Activation.Forced;
			simulator.SimulateStep();
			patient = simulator.Model.Roots.OfType<Patient>().First();
			simulator.SimulateStep();
			patient = simulator.Model.Roots.OfType<Patient>().First();
			simulator.SimulateStep();
			patient = simulator.Model.Roots.OfType<Patient>().First();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
		}

		[Test]
		public void Simulate_10_Step_HeaterAndPumpToBalanceChamberDefect()
		{
			var testModel = new Model();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			var patient = simulator.Model.Roots.OfType<Patient>().First();
			var hdMachine = simulator.Model.Roots.OfType<HdMachine>().First();
			hdMachine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefect.Activation = Activation.Forced;
			hdMachine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpDefect.Activation = Activation.Forced;
			simulator.SimulateStep();
			var patient2 = simulator.Model.Roots.OfType<Patient>().First();
			simulator.SimulateStep();
			var patient3 = simulator.Model.Roots.OfType<Patient>().First();
			simulator.SimulateStep();
			var patient4 = simulator.Model.Roots.OfType<Patient>().First();
			var hdMachine4 = simulator.Model.Roots.OfType<HdMachine>().First();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
		}

		[Test]
		public void IncomingBloodIsContaminated_ModelChecking(
			[Values(SafetyAnalysisBackend.FaultOptimizedStateGraph, SafetyAnalysisBackend.FaultOptimizedOnTheFly)] SafetyAnalysisBackend backend)
		{
			var specification = new Model();
			var analysis = new SafetySharpSafetyAnalysis
			{
				Configuration = { StateCapacity = 1310720 },
				Backend = backend,
				Heuristics = { new MaximalSafeSetHeuristic(specification) }
			};

			var result = analysis.ComputeMinimalCriticalSets(specification, specification.IncomingBloodWasNotOk);
			result.SaveCounterExamples("counter examples/hdmachine_contamination");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}

		[Test]
		public void DialysisFinishedAndBloodNotCleaned_ModelChecking(
			[Values(SafetyAnalysisBackend.FaultOptimizedStateGraph, SafetyAnalysisBackend.FaultOptimizedOnTheFly)] SafetyAnalysisBackend backend)
		{
			var specification = new Model();
			var analysis = new SafetySharpSafetyAnalysis { Configuration = { StateCapacity = 1310720 }, Backend = backend };

			var result = analysis.ComputeMinimalCriticalSets(specification, specification.BloodNotCleanedAndDialyzingFinished);
			result.SaveCounterExamples("counter examples/hdmachine_unsuccessful");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}

		private static void Main()
		{
		}

		[Test]
		public void Test()
		{
			var specification = new Model();
			var model = specification;
			var faults = model.Faults;

			for (var i = 0; i < faults.Length; ++i)
				faults[i].Activation = Activation.Nondeterministic;

			var checker = new SafetySharpQualitativeChecker { Configuration = { StateCapacity = 1310720 } };
			checker.CheckInvariant(model, true);
		}
		
		[Test]
		public void StateGraphAllStates()
		{
			var specification = new Model();
			var model = specification;
			var result = SafetySharpModelChecker.CheckInvariants(model, !specification.IncomingBloodWasNotOk, !specification.BloodNotCleanedAndDialyzingFinished);

			result[0].FormulaHolds.Should().BeFalse();
			result[1].FormulaHolds.Should().BeFalse();
		}
	}
}