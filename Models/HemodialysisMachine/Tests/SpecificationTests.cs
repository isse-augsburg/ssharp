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

namespace HemodialysisMachine.Tests
{
	using System;
	using System.Linq;
	using Model;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Reflection;

	public class SpecificationTests
	{
		[Test]
		public void Simulate_1Step()
		{
			var testModel = new Specification();
			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (DialyzingFluidFlowCombinator)simulator.Model.RootComponents[0];
		}

		[Test]
		public void Simulate_10_Step()
		{
			var testModel = new Specification();
			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
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

			var flowCombinatorAfterStep = (DialyzingFluidFlowCombinator)simulator.Model.RootComponents[0];
		}

		[Test]
		public void Simulate_10_Step_DialyzerRuptured()
		{
			var testModel = new Specification();

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			var patient = simulator.Model.RootComponents.OfType<Patient>().First();
			var hdMachine = simulator.Model.RootComponents.OfType<HdMachine>().First();
			hdMachine.Dialyzer.DialyzerMembraneRupturesFault.Activation = Activation.Forced;
			simulator.SimulateStep();
			patient = simulator.Model.RootComponents.OfType<Patient>().First();
			simulator.SimulateStep();
			patient = simulator.Model.RootComponents.OfType<Patient>().First();
			simulator.SimulateStep();
			patient = simulator.Model.RootComponents.OfType<Patient>().First();
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
			var testModel = new Specification();

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			var patient = simulator.Model.RootComponents.OfType<Patient>().First();
			var hdMachine = simulator.Model.RootComponents.OfType<HdMachine>().First();
			hdMachine.DialyzingFluidDeliverySystem.DialyzingFluidWaterPreparation.WaterHeaterDefect.Activation = Activation.Forced;
			hdMachine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpToBalanceChamberDefect.Activation = Activation.Forced;
			simulator.SimulateStep();
			var patient2 = simulator.Model.RootComponents.OfType<Patient>().First();
			simulator.SimulateStep();
			var patient3 = simulator.Model.RootComponents.OfType<Patient>().First();
			simulator.SimulateStep();
			var patient4 = simulator.Model.RootComponents.OfType<Patient>().First();
			var hdMachine4 = simulator.Model.RootComponents.OfType<HdMachine>().First();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
			simulator.SimulateStep();
		}

		[Test]
		public void IncomingBloodIsContaminated_ModelChecking()
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis { Configuration = { StateCapacity = 1310720 } };

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.IncomingBloodNotOk);
			result.SaveCounterExamples("counter examples/hdmachine");

			Console.WriteLine(result);
		}

		[Test]
		public void DialysisFinishedAndBloodNotCleaned_ModelChecking()
		{
			var specification = new Specification();
			var analysis = new SafetyAnalysis { Configuration = { StateCapacity = 1310720 } };

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.BloodNotCleanedAndDialyzingFinished);
			result.SaveCounterExamples("counter examples/hdmachine");

			Console.WriteLine(result);
		}

		private static void Main()
		{
			new SpecificationTests().IncomingBloodIsContaminated_ModelChecking();
		}

		[Test]
		public void Test()
		{
			var specification = new Specification();
			var model = new Model(specification);
			var faults = model.GetFaults();

			for (var i = 0; i < faults.Length; ++i)
				faults[i].Activation = Activation.Nondeterministic;

			var checker = new SSharpChecker { Configuration = { StateCapacity = 1310720 } };
			checker.CheckInvariant(model, true);
		}
	}
}