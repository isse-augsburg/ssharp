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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Tests
{
	using FluentAssertions;
	using Model;
	using NUnit.Framework;
	using Analysis;
	using Modeling;
	using Runtime;

	class ExtracorporealBloodCircuitTestEnvironmentDialyzer : Component
	{
		// Order of Provided Port call (determined by flowConnectors)
		// 1. Suction of DialyzingFluid is calculated
		// 2. Element of DialyzingFluid is calculated
		// 3. Suction of Blood is calculated
		// 4. Element of Blood is calculated

		public BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment();

		public int IncomingSuctionRateOnDialyzingFluidSide = 3;
		public int IncomingQuantityOfDialyzingFluid = 2;
		public QualitativeTemperature IncomingFluidTemperature = QualitativeTemperature.TooCold;

		public bool MembraneIntact = true;
		
		[Provided]
		public void SetBloodFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		[Provided]
		public void SetBloodFlow(Blood outgoing, Blood incoming)
		{
			if (incoming.Water > 0 || incoming.BigWasteProducts > 0)
			{
				outgoing.CopyValuesFrom(incoming);
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
				outgoing.CopyValuesFrom(incoming);
			}
		}

		protected override void CreateBindings()
		{
			Bind(nameof(BloodFlow.SetOutgoingBackward), nameof(SetBloodFlowSuction));
			Bind(nameof(BloodFlow.SetOutgoingForward), nameof(SetBloodFlow));
		}
	}

	class ExtracorporealBloodCircuitTestEnvironment : ModelBase
	{
		[Root(Role.SystemOfInterest)]
		public readonly ExtracorporealBloodCircuit ExtracorporealBloodCircuit = new ExtracorporealBloodCircuit();

		[Root(Role.SystemContext)]
		public readonly Patient Patient = new Patient();
		[Root(Role.SystemContext)]
		public readonly ExtracorporealBloodCircuitTestEnvironmentDialyzer Dialyzer = new ExtracorporealBloodCircuitTestEnvironmentDialyzer();
		[Root(Role.SystemContext)]
		public readonly BloodFlowCombinator BloodFlowCombinator = new BloodFlowCombinator();

		public ExtracorporealBloodCircuitTestEnvironment()
		{
			BloodFlowCombinator.Connect(Patient.ArteryFlow.Outgoing, ExtracorporealBloodCircuit.BloodFlow.Incoming);
			ExtracorporealBloodCircuit.AddFlows(BloodFlowCombinator);
			BloodFlowCombinator.Connect(ExtracorporealBloodCircuit.BloodFlow.Outgoing, Patient.VeinFlow.Incoming);
			BloodFlowCombinator.Replace(ExtracorporealBloodCircuit.ToDialyzer.Incoming, Dialyzer.BloodFlow.Incoming);
			BloodFlowCombinator.Replace(ExtracorporealBloodCircuit.FromDialyzer.Outgoing, Dialyzer.BloodFlow.Outgoing);

		}
	}
	class ExtracorporealBloodCircuitTests
	{
		
		[Test]
		public void ExtracorporealBloodCircuitWorks_Simulation()
		{
			var specification = new ExtracorporealBloodCircuitTestEnvironment();

			var simulator = new Simulator(specification); //Important: Call after all objects have been created
			var extracorporealBloodCircuitAfterStep0 = simulator.Model.RootComponents.OfType<ExtracorporealBloodCircuit>().First();
			var patientAfterStep0 = simulator.Model.RootComponents.OfType<Patient>().First();
			Console.Out.WriteLine("Initial");
			patientAfterStep0.ArteryFlow.Outgoing.ForwardToSuccessor.PrintBloodValues("outgoing Blood");
			patientAfterStep0.VeinFlow.Incoming.ForwardFromPredecessor.PrintBloodValues("incoming Blood");
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
