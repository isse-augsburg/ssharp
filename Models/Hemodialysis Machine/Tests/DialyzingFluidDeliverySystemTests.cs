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


	class DialyzingFluidDeliverySystemTestEnvironmentDialyzer : Component
	{
		// Order of Provided Port call (determined by flowConnectors)
		// 1. Suction of DialyzingFluid is calculated
		// 2. Element of DialyzingFluid is calculated

		public DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		public int IncomingSuctionRateOnDialyzingFluidSide = 0;
		public int IncomingQuantityOfDialyzingFluid = 0; //Amount of BloodUnits we can clean.
		public QualitativeTemperature IncomingFluidTemperature;

		public bool MembraneIntact = true;


		[Provided]
		public void SetDialyzingFluidFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			IncomingSuctionRateOnDialyzingFluidSide = incomingSuction.CustomSuctionValue;
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction;
		}

		[Provided]
		public void SetDialyzingFluidFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			IncomingFluidTemperature = incoming.Temperature;
			IncomingQuantityOfDialyzingFluid = incoming.Quantity;
			outgoing.CopyValuesFrom(incoming);
			outgoing.Quantity = IncomingSuctionRateOnDialyzingFluidSide;
			outgoing.WasUsed = true;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingBackward), nameof(SetDialyzingFluidFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingForward), nameof(SetDialyzingFluidFlow));
		}
	}

	class DialyzingFluidDeliverySystemTestEnvironment : Component
	{
		[Root(Role.SystemOfInterest)]
		public readonly DialyzingFluidDeliverySystem DialyzingFluidDeliverySystem = new DialyzingFluidDeliverySystem();

		[Root(Role.SystemContext)]
		public readonly DialyzingFluidFlowCombinator DialysingFluidFlowCombinator = new DialyzingFluidFlowCombinator();
		[Root(Role.SystemContext)]
		public readonly DialyzingFluidDeliverySystemTestEnvironmentDialyzer Dialyzer = new DialyzingFluidDeliverySystemTestEnvironmentDialyzer();

		public DialyzingFluidDeliverySystemTestEnvironment()
		{
			DialyzingFluidDeliverySystem.AddFlows(DialysingFluidFlowCombinator);
			DialysingFluidFlowCombinator.Replace(DialyzingFluidDeliverySystem.ToDialyzer.Incoming, Dialyzer.DialyzingFluidFlow.Incoming);
			DialysingFluidFlowCombinator.Replace(DialyzingFluidDeliverySystem.FromDialyzer.Outgoing, Dialyzer.DialyzingFluidFlow.Outgoing);
		}
		
	}
	class DialyzingFluidDeliverySystemTests
	{
		[Test]
		public void DialyzingFluidDeliverySystemWorks_Simulation()
		{
			var specification = new DialyzingFluidDeliverySystemTestEnvironment();

			var simulator = new Simulator(new Model(specification)); //Important: Call after all objects have been created
			var dialyzerAfterStep0 = simulator.Model.RootComponents.OfType<DialyzingFluidDeliverySystemTestEnvironmentDialyzer>().First();
			var dialyzingFluidDeliverySystemAfterStep0 = simulator.Model.RootComponents.OfType<DialyzingFluidDeliverySystem>().First();
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
			var analysis = new SafetyAnalysis();

			var result = analysis.ComputeMinimalCriticalSets(new Model(specification), specification.Dialyzer.MembraneIntact == false);
			result.SaveCounterExamples("counter examples/hdmachine");

			Console.WriteLine(result);
			
		}
	}
}
