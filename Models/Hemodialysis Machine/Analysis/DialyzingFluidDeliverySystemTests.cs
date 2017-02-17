// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using ModelChecking;
	using Modeling;
	using SafetySharp.Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using Modeling.DialyzingFluidDeliverySystem;
	using Runtime;


	class DialyzingFluidDeliverySystemTestEnvironmentDialyzer : Component
	{
		// Order of Provided Port call (determined by flowConnectors)
		// 1. Suction of DialyzingFluid is calculated
		// 2. Element of DialyzingFluid is calculated

		public DialyzingFluidFlowInToOut DialyzingFluidFlow = new DialyzingFluidFlowInToOut();

		public int IncomingSuctionRateOnDialyzingFluidSide = 0;
		public int IncomingQuantityOfDialyzingFluid = 0; //Amount of BloodUnits we can clean.
		public QualitativeTemperature IncomingFluidTemperature;

		public bool MembraneIntact = true;


		[Provided]
		public Suction SetDialyzingFluidFlowSuction(Suction incomingSuction)
		{
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			Suction outgoingSuction;
			IncomingSuctionRateOnDialyzingFluidSide = incomingSuction.CustomSuctionValue;
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction;
			return outgoingSuction;
		}

		[Provided]
		public DialyzingFluid SetDialyzingFluidFlow(DialyzingFluid incoming)
		{
			IncomingFluidTemperature = incoming.Temperature;
			IncomingQuantityOfDialyzingFluid = incoming.Quantity;
			var outgoing = incoming;
			outgoing.Quantity = IncomingSuctionRateOnDialyzingFluidSide;
			outgoing.WasUsed = true;
			return outgoing;
		}

		public DialyzingFluidDeliverySystemTestEnvironmentDialyzer()
		{
			DialyzingFluidFlow.UpdateBackward=SetDialyzingFluidFlowSuction;
			DialyzingFluidFlow.UpdateForward=SetDialyzingFluidFlow;
		}
	}

	class DialyzingFluidDeliverySystemTestEnvironment : ModelBase
	{
		[Root(RootKind.Controller)]
		public readonly DialyzingFluidDeliverySystem DialyzingFluidDeliverySystem = new DialyzingFluidDeliverySystem();

		[Root(RootKind.Plant)]
		public readonly DialyzingFluidFlowCombinator DialysingFluidFlowCombinator = new DialyzingFluidFlowCombinator();
		[Root(RootKind.Plant)]
		public readonly DialyzingFluidDeliverySystemTestEnvironmentDialyzer Dialyzer = new DialyzingFluidDeliverySystemTestEnvironmentDialyzer();

		public DialyzingFluidDeliverySystemTestEnvironment()
		{
			DialyzingFluidDeliverySystem.AddFlows(DialysingFluidFlowCombinator);
			DialysingFluidFlowCombinator.ConnectOutWithIn(DialyzingFluidDeliverySystem.ToDialyzer, Dialyzer.DialyzingFluidFlow);
			DialysingFluidFlowCombinator.ConnectOutWithIn(Dialyzer.DialyzingFluidFlow, DialyzingFluidDeliverySystem.FromDialyzer);
			DialysingFluidFlowCombinator.CommitFlow();
		}
		
	}
	class DialyzingFluidDeliverySystemTests
	{
		[Test]
		public void DialyzingFluidDeliverySystemWorks_Simulation()
		{
			var specification = new DialyzingFluidDeliverySystemTestEnvironment();

			var simulator = new SafetySharpSimulator(specification); //Important: Call after all objects have been created
			var dialyzerAfterStep0 = simulator.Model.Roots.OfType<DialyzingFluidDeliverySystemTestEnvironmentDialyzer>().First();
			var dialyzingFluidDeliverySystemAfterStep0 = simulator.Model.Roots.OfType<DialyzingFluidDeliverySystem>().First();
			Console.Out.WriteLine("Initial");
			//dialyzingFluidDeliverySystemAfterStep0.ArteryFlow.Outgoing.ForwardToSuccessor.PrintBloodValues("outgoing Blood");
			//patientAfterStep0.VeinFlow.Incoming.ForwardFromPredecessor.PrintBloodValues("incoming Blood");
			//patientAfterStep0.PrintBloodValues("");
			Console.Out.WriteLine("Step 1");
			simulator.SimulateStep();
			

			/*
			//dialyzerAfterStep1.Should().Be(1);
			patientAfterStep4.BigWasteProducts.Should().Be(0);
			patientAfterStep4.SmallWasteProducts.Should().Be(2);*/
		}
		[Test]
		public void DialyzingFluidDeliverySystemWorks_ModelChecking()
		{
			var specification = new DialyzingFluidDeliverySystemTestEnvironment();
			var analysis = new SafetySharpSafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(specification, specification.Dialyzer.MembraneIntact == false);
			result.SaveCounterExamples("counter examples/hdmachine");

			Console.WriteLine(result);
			
		}
	}
}
