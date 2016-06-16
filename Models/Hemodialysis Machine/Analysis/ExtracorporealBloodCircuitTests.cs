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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Analysis
{
	using FluentAssertions;
	using Modeling;
	using SafetySharp.Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using Modeling.ExtracorporealBloodCircuit;
	using Runtime;

	class ExtracorporealBloodCircuitTestEnvironmentDialyzer : Component
	{
		// Order of Provided Port call (determined by flowConnectors)
		// 1. Suction of DialyzingFluid is calculated
		// 2. Element of DialyzingFluid is calculated
		// 3. Suction of Blood is calculated
		// 4. Element of Blood is calculated

		public BloodFlowInToOut BloodFlow = new BloodFlowInToOut();

		public int IncomingSuctionRateOnDialyzingFluidSide = 3;
		public int IncomingQuantityOfDialyzingFluid = 2;
		public QualitativeTemperature IncomingFluidTemperature = QualitativeTemperature.TooCold;

		public bool MembraneIntact = true;
		
		[Provided]
		public Suction SetBloodFlowSuction(Suction incomingSuction)
		{
			return incomingSuction;
		}

		[Provided]
		public Blood SetBloodFlow(Blood incoming)
		{
			Blood outgoing;
			if (incoming.Water > 0 || incoming.BigWasteProducts > 0)
			{
				outgoing=incoming;
				outgoing.Temperature = IncomingFluidTemperature;
				// First step: Filtrate Blood
				if (IncomingQuantityOfDialyzingFluid >= outgoing.SmallWasteProducts)
				{
					outgoing.SmallWasteProducts = 0;
				}
				else
				{
					outgoing.SmallWasteProducts -= IncomingQuantityOfDialyzingFluid;
				}
				// Second step: Ultra Filtration
				// To satisfy the incoming suction rate we must take the fluid from the blood.
				// The ultrafiltrationRate is the amount of fluid we take from the blood-side.
				var ultrafiltrationRate = IncomingSuctionRateOnDialyzingFluidSide - IncomingQuantityOfDialyzingFluid;

				if (ultrafiltrationRate < outgoing.BigWasteProducts)
				{
					outgoing.BigWasteProducts -= ultrafiltrationRate;
				}
				else
				{
					// Remove water instead of BigWasteProducts
					// Assume Water >= (ultrafiltrationRate - outgoing.BigWasteProducts)
					outgoing.Water -= (ultrafiltrationRate - outgoing.BigWasteProducts);
					outgoing.BigWasteProducts = 0;
				}
			}
			else
			{
				outgoing=incoming;
			}
			return outgoing;
		}

		protected override void CreateBindings()
		{
			BloodFlow.UpdateBackward=SetBloodFlowSuction;
			BloodFlow.UpdateForward=SetBloodFlow;
		}
	}

	class ExtracorporealBloodCircuitTestEnvironment : ModelBase
	{
		[Root(RootKind.Controller)]
		public readonly ExtracorporealBloodCircuit ExtracorporealBloodCircuit = new ExtracorporealBloodCircuit();

		[Root(RootKind.Plant)]
		public readonly Patient Patient = new Patient();
		[Root(RootKind.Plant)]
		public readonly ExtracorporealBloodCircuitTestEnvironmentDialyzer Dialyzer = new ExtracorporealBloodCircuitTestEnvironmentDialyzer();
		[Root(RootKind.Plant)]
		public readonly BloodFlowCombinator BloodFlowCombinator = new BloodFlowCombinator();

		public ExtracorporealBloodCircuitTestEnvironment()
		{
			BloodFlowCombinator.ConnectOutWithIn(Patient.ArteryFlow, ExtracorporealBloodCircuit.FromPatientArtery);
			ExtracorporealBloodCircuit.AddFlows(BloodFlowCombinator);
			BloodFlowCombinator.ConnectOutWithIn(ExtracorporealBloodCircuit.ToPatientVein, Patient.VeinFlow);
			BloodFlowCombinator.ConnectOutWithIn(ExtracorporealBloodCircuit.ToDialyzer, Dialyzer.BloodFlow);
			BloodFlowCombinator.ConnectOutWithIn(Dialyzer.BloodFlow, ExtracorporealBloodCircuit.FromDialyzer);
			BloodFlowCombinator.CommitFlow();

		}
	}
	class ExtracorporealBloodCircuitTests
	{
		
		[Test]
		public void ExtracorporealBloodCircuitWorks_Simulation()
		{
			var specification = new ExtracorporealBloodCircuitTestEnvironment();

			var simulator = new Simulator(specification); //Important: Call after all objects have been created
			var extracorporealBloodCircuitAfterStep0 = simulator.Model.Roots.OfType<ExtracorporealBloodCircuit>().First();
			var patientAfterStep0 = simulator.Model.Roots.OfType<Patient>().First();
			Console.Out.WriteLine("Initial");
			patientAfterStep0.ArteryFlow.Outgoing.Forward.PrintBloodValues("outgoing Blood");
			patientAfterStep0.VeinFlow.Incoming.Forward.PrintBloodValues("incoming Blood");
			patientAfterStep0.PrintBloodValues("");
			Console.Out.WriteLine("Step 1");
			simulator.SimulateStep();

			/*
			//dialyzerAfterStep1.Should().Be(1);
			patientAfterStep4.BigWasteProducts.Should().Be(0);
			patientAfterStep4.SmallWasteProducts.Should().Be(2);
			*/
		}
		[Test]
		public void ExtracorporealBloodCircuitWorks_ModelChecking()
		{
			var specification = new ExtracorporealBloodCircuitTestEnvironment();
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(specification, specification.Dialyzer.MembraneIntact == false);
			result.SaveCounterExamples("counter examples/hdmachine");

			Console.WriteLine(result);
		}
	}
}
