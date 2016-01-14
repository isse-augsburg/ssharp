using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Tests
{
	using FluentAssertions;
	using Model;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;
	

	class DialyzerTestEnvironment : Component
	{
		public class DialyzerTestEnvironmentPatient : Component
		{
			public readonly BloodFlowSource ArteryFlow = new BloodFlowSource();
			public readonly BloodFlowSink VeinFlow = new BloodFlowSink();

			public int UnfiltratedBloodUnits = 10;
			public int FiltratedBloodUnits = 0;

			[Provided]
			public void CreateBlood(Blood outgoingBlood)
			{
				var unitsToDeliver = ArteryFlow.Outgoing.BackwardFromSuccessor;
				if (UnfiltratedBloodUnits > unitsToDeliver.Value)
				{
					outgoingBlood.UnfiltratedBloodUnits = unitsToDeliver.Value;
					outgoingBlood.FiltratedBloodUnits = 0;
				}
				else
				{
					outgoingBlood.UnfiltratedBloodUnits = UnfiltratedBloodUnits; // Deliver rest of unfiltrated blood or none
					outgoingBlood.FiltratedBloodUnits = unitsToDeliver.Value - UnfiltratedBloodUnits;
				}
				UnfiltratedBloodUnits -= outgoingBlood.UnfiltratedBloodUnits;
				FiltratedBloodUnits -= outgoingBlood.FiltratedBloodUnits;
				outgoingBlood.HasHeparin = true;
				outgoingBlood.ChemicalCompositionOk = true;
				outgoingBlood.GasFree = true;
				outgoingBlood.Pressure = QualitativePressure.GoodPressure;
				outgoingBlood.Temperature = QualitativeTemperature.BodyHeat;
			}

			[Provided]
			public void CreateBloodSuction(Suction outgoingSuction)
			{
				outgoingSuction.Value = 2;
			}

			[Provided]
			public void BloodReceived(Blood incomingBlood)
			{
				UnfiltratedBloodUnits += incomingBlood.UnfiltratedBloodUnits;
				FiltratedBloodUnits += incomingBlood.FiltratedBloodUnits;
			}

			[Provided]
			public void DoNothing(Suction incomingSuction)
			{
			}

			protected override void CreateBindings()
			{
				Bind(nameof(ArteryFlow.SetOutgoingForward), nameof(CreateBlood));
				Bind(nameof(ArteryFlow.BackwardFromSuccessorWasUpdated), nameof(DoNothing));
				Bind(nameof(VeinFlow.SetOutgoingBackward), nameof(CreateBloodSuction));
				Bind(nameof(VeinFlow.ForwardFromPredecessorWasUpdated), nameof(BloodReceived));
			}
		}


		[Root(Role.SystemOfInterest)]
		public readonly Dialyzer Dialyzer = new Dialyzer();

		[Root(Role.SystemContext)]
		public readonly DialyzingFluidFlowCombinator DialysingFluidFlowCombinator = new DialyzingFluidFlowCombinator();
		[Root(Role.SystemContext)]
		public readonly BloodFlowCombinator BloodFlowCombinator = new BloodFlowCombinator();
		[Root(Role.SystemContext)]
		public readonly DialyzingFluidFlowSource DialyzingFluidFlowSource = new DialyzingFluidFlowSource();
		[Root(Role.SystemContext)]
		public readonly DialyzingFluidFlowSink DialyzingFluidFlowSink = new DialyzingFluidFlowSink();
		[Root(Role.SystemContext)]
		public readonly DialyzerTestEnvironmentPatient Patient = new DialyzerTestEnvironmentPatient();
		
		[Provided]
		public void CreateDialyzingFluid(DialyzingFluid outgoingDialyzingFluid)
		{
			//Hard code delivered quantity 2 and suction 3. We simulate if Ultra Filtration works with Dialyzer.
			outgoingDialyzingFluid.Quantity = 2;
			outgoingDialyzingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;
			outgoingDialyzingFluid.ContaminatedByBlood = false;
			outgoingDialyzingFluid.Temperature=QualitativeTemperature.BodyHeat;
	}

		[Provided]
		public void CreateDialyzingFluidSuction(Suction outgoingSuction)
		{
			//Hard code delivered quantity 2 and suction 3. We simulate if Ultra Filtration works with Dialyzer.
			outgoingSuction.Value = 3;
		}

		[Provided]
		public void DoNothing(Suction incomingSuction)
		{
		}

		[Provided]
		public void DoNothing(DialyzingFluid incomingDialyzingFluid)
		{
		}

		public DialyzerTestEnvironment()
		{
			Bind(nameof(DialyzingFluidFlowSource.SetOutgoingForward), nameof(CreateDialyzingFluid));
			Bind(nameof(DialyzingFluidFlowSource.BackwardFromSuccessorWasUpdated), nameof(DoNothing));
			Bind(nameof(DialyzingFluidFlowSink.SetOutgoingBackward), nameof(CreateDialyzingFluidSuction));
			Bind(nameof(DialyzingFluidFlowSink.ForwardFromPredecessorWasUpdated), nameof(DoNothing));
			DialysingFluidFlowCombinator.Connect(DialyzingFluidFlowSource.Outgoing, Dialyzer.DialyzingFluidFlow.Incoming);
			DialysingFluidFlowCombinator.Connect(Dialyzer.DialyzingFluidFlow.Outgoing, DialyzingFluidFlowSink.Incoming);
			BloodFlowCombinator.Connect(Patient.ArteryFlow.Outgoing, Dialyzer.BloodFlow.Incoming);
			BloodFlowCombinator.Connect(Dialyzer.BloodFlow.Outgoing, Patient.VeinFlow.Incoming);
		}
	}


	class DialyzerTests
	{
		[Test]
		public void DialyzerWorks()
		{
			var testModel = new DialyzerTestEnvironment();

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();
			var dialyzerAfterStep1 = (Dialyzer)simulator.Model.RootComponents.OfType<Dialyzer>().First();
			var patientAfterStep1 = (DialyzerTestEnvironment.DialyzerTestEnvironmentPatient)simulator.Model.RootComponents.OfType<DialyzerTestEnvironment.DialyzerTestEnvironmentPatient>().First();
			simulator.SimulateStep();
			var dialyzerAfterStep2 = (Dialyzer)simulator.Model.RootComponents.OfType<Dialyzer>().First();
			var patientAfterStep2 = (DialyzerTestEnvironment.DialyzerTestEnvironmentPatient)simulator.Model.RootComponents.OfType<DialyzerTestEnvironment.DialyzerTestEnvironmentPatient>().First();
			simulator.SimulateStep();
			var dialyzerAfterStep3 = (Dialyzer)simulator.Model.RootComponents.OfType<Dialyzer>().First();
			var patientAfterStep3 = (DialyzerTestEnvironment.DialyzerTestEnvironmentPatient)simulator.Model.RootComponents.OfType<DialyzerTestEnvironment.DialyzerTestEnvironmentPatient>().First();
			simulator.SimulateStep();
			var dialyzerAfterStep4 = (Dialyzer)simulator.Model.RootComponents.OfType<Dialyzer>().First();
			var patientAfterStep4 = (DialyzerTestEnvironment.DialyzerTestEnvironmentPatient)simulator.Model.RootComponents.OfType<DialyzerTestEnvironment.DialyzerTestEnvironmentPatient>().First();

			//dialyzerAfterStep1.Should().Be(1);
			patientAfterStep4.FiltratedBloodUnits.Should().Be(10);
			patientAfterStep4.UnfiltratedBloodUnits.Should().Be(2);
		}
	}
}
