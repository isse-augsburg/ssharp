using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;
	using Utilities;

	// More details on the balance chamber on the internet:
	// http://principlesofdialysis.weebly.com/uploads/5/6/1/3/5613613/2008ccmodule4.pdf
	// -> Chapter "Volumetric UF Control"

	class DialyzingFluidWaterSupply : Component
	{
		public readonly DialyzingFluidFlowSource MainFlow = new DialyzingFluidFlowSource();

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing)
		{
			outgoing.ContaminatedByBlood = false;
			outgoing.Quantity = 0;
		}
		
		[Provided]
		public void ReceivedSuction(int incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingElement), nameof(SetMainFlow));
			Bind(nameof(MainFlow.SuctionFromSuccessorWasUpdated), nameof(ReceivedSuction));
		}
	}

	class DialyzingFluidWaterPreparation : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment MainFlow = new DialyzingFluidFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(ref int outgoingSuction, int incomingSuction)
		{
			outgoingSuction = incomingSuction;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingSuction), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingElement), nameof(SetMainFlow));
		}
	}

	class DialyzingFluidConcentrateSupply : Component
	{
		public readonly DialyzingFluidFlowSource Concentrate = new DialyzingFluidFlowSource();

		[Provided]
		public void SetConcentrateFlow(DialyzingFluid outgoing)
		{
			outgoing.ContaminatedByBlood = false;
			outgoing.Quantity = 0;
		}

		[Provided]
		public void ReceivedSuction(int incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Concentrate.SetOutgoingElement), nameof(SetConcentrateFlow));
			Bind(nameof(Concentrate.SuctionFromSuccessorWasUpdated), nameof(ReceivedSuction));
		}
	}

	class DialyzingFluidPreparation : Component
	{
		public readonly DialyzingFluidFlowSink Concentrate = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		[Provided]
		public void SetConcentrateFlowSuction(ref int outgoingSuction)
		{
			outgoingSuction = 1;
		}

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(ref int outgoingSuction, int incomingSuction)
		{
			outgoingSuction = incomingSuction;
		}

		[Provided]
		public void ReceivedConcentrate(DialyzingFluid incomingElement)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Concentrate.SetOutgoingSuction), nameof(SetConcentrateFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingSuction), nameof(SetMainFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingElement), nameof(SetMainFlow));
			Bind(nameof(Concentrate.ElementFromPredecessorWasUpdated), nameof(ReceivedConcentrate));
		}
	}

	class DialyzingUltraFiltrationPump : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(ref int outgoingSuction, int incomingSuction)
		{
			outgoingSuction = incomingSuction;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingSuction), nameof(SetMainFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingElement), nameof(SetMainFlow));
		}
	}

	class DialyzingFluidDrain : Component
	{
		public readonly DialyzingFluidFlowSink DrainFlow = new DialyzingFluidFlowSink();

		[Provided]
		public void SetMainFlowSuction(ref int outgoingSuction)
		{
			outgoingSuction = 1;
		}

		[Provided]
		public void ReceivedDialyzingFluid(DialyzingFluid incomingElement)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DrainFlow.SetOutgoingSuction), nameof(SetMainFlowSuction));
			Bind(nameof(DrainFlow.ElementFromPredecessorWasUpdated), nameof(ReceivedDialyzingFluid));
		}
	}

	class DialyzingFluidSafetyBypass : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment MainFlow = new DialyzingFluidFlowInToOutSegment();
		public readonly DialyzingFluidFlowSource DrainFlow = new DialyzingFluidFlowSource();

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(ref int outgoingSuction, int incomingSuction)
		{
			outgoingSuction = incomingSuction;
		}

		[Provided]
		public void SetDrainFlow(DialyzingFluid outgoing)
		{
			outgoing.ContaminatedByBlood=false;
			outgoing.Quantity = 0;
		}

		[Provided]
		public void ReceivedSuction(int incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingSuction), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingElement), nameof(SetMainFlow));
			Bind(nameof(DrainFlow.SetOutgoingElement), nameof(SetDrainFlow));
			Bind(nameof(DrainFlow.SuctionFromSuccessorWasUpdated), nameof(ReceivedSuction));
		}

	}


	// Two identical chambers

	// Each chamber has
	//   * Two sides separated by diaphragm.
	//     One half to the dialyzer (fresh dialyzing fluid) and the other half from the dialyzer (used dialyzing fluid)
	//   * inlet valve and outlet valve
	// - Produce Dialysing Fluid: Fresh Dialysing Fluid to store in passive chamber / used dialysate from passive chamber to drain
	// - Use Dialysing Fluid.Stored in active chamber -> dialysator -> drain or store used in active chamber
	class BalanceChamber : Component
	{
		public readonly DialyzingFluidFlowSink ProducedDialysingFluid = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowSink UsedDialysingFluid = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowSource StoredProducedDialysingFluid = new DialyzingFluidFlowSource();
		public readonly DialyzingFluidFlowSource StoredUsedDialysingFluid = new DialyzingFluidFlowSource();


		enum State
		{
			UseChamber1ForDialyzer,
			UseChamber2ForDialyzer
		}

		class SubChamber
		{
			private ValveState ValveToDialyser;
			private ValveState ValveToDrain;
			private ValveState ValveFromDialyzingFluidPreparation;
			private ValveState ValveFromDialyzer;

			private int FreshDialysingFluid;
			private int UsedDialysingFluid;

		}

		[Provided]
		public void SetProducedDialysingFluidSuction(ref int outgoingSuction)
		{
			outgoingSuction = 1;
		}
		[Provided]
		public void SetUsedDialysingFluidSuction(ref int outgoingSuction)
		{
			outgoingSuction = 1;
		}
		[Provided]
		public void SetStoredProducedDialysingFluid(DialyzingFluid outgoing)
		{
			outgoing.ContaminatedByBlood = false;
		}
		[Provided]
		public void SetUsedDialysingFluid(DialyzingFluid outgoing)
		{
			outgoing.ContaminatedByBlood = false;
		}


		[Provided]
		public void ReceivedSuctionOnStoredProducedDialyzingFluid(int incomingSuction)
		{
		}

		[Provided]
		public void ReceivedProducedDialyzingFluid(DialyzingFluid incomingElement)
		{
		}

		[Provided]
		public void ReceivedSuctionOnStoredUsedDialyzingFluid(int incomingSuction)
		{
		}

		[Provided]
		public void ReceivedUsedDialyzingFluid(DialyzingFluid incomingElement)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(ProducedDialysingFluid.SetOutgoingSuction), nameof(SetProducedDialysingFluidSuction));
			Bind(nameof(UsedDialysingFluid.SetOutgoingSuction), nameof(SetUsedDialysingFluidSuction));
			Bind(nameof(StoredProducedDialysingFluid.SetOutgoingElement), nameof(SetStoredProducedDialysingFluid));
			Bind(nameof(StoredUsedDialysingFluid.SetOutgoingElement), nameof(SetUsedDialysingFluid));
			Bind(nameof(ProducedDialysingFluid.ElementFromPredecessorWasUpdated), nameof(ReceivedProducedDialyzingFluid));
			Bind(nameof(UsedDialysingFluid.ElementFromPredecessorWasUpdated), nameof(ReceivedUsedDialyzingFluid));
			Bind(nameof(StoredProducedDialysingFluid.SuctionFromSuccessorWasUpdated), nameof(ReceivedSuctionOnStoredProducedDialyzingFluid));
			Bind(nameof(StoredUsedDialysingFluid.SuctionFromSuccessorWasUpdated), nameof(ReceivedSuctionOnStoredUsedDialyzingFluid));
		}
	}


	// Also called dialyzing fluid delivery system
	class DialyzingFluidDeliverySystem : Component
	{
		public readonly DialyzingFluidFlowUniqueOutgoingStub FromDialyzer = new DialyzingFluidFlowUniqueOutgoingStub();
		public readonly DialyzingFluidFlowUniqueIncomingStub ToDialyzer = new DialyzingFluidFlowUniqueIncomingStub();

		public readonly DialyzingFluidWaterSupply DialyzingFluidWaterSupply = new DialyzingFluidWaterSupply();
		public readonly DialyzingFluidWaterPreparation DialyzingFluidWaterPreparation = new DialyzingFluidWaterPreparation();
		public readonly DialyzingFluidConcentrateSupply DialyzingFluidConcentrateSupply = new DialyzingFluidConcentrateSupply();
		public readonly DialyzingFluidPreparation DialyzingFluidPreparation = new DialyzingFluidPreparation();
		public readonly BalanceChamber BalanceChamber = new BalanceChamber();
		public readonly DialyzingFluidDrain DialyzingFluidDrain = new DialyzingFluidDrain();
		public readonly DialyzingFluidSafetyBypass DialyzingFluidSafetyBypass = new DialyzingFluidSafetyBypass();

		public readonly DialyzingUltraFiltrationPump DialyzingUltraFiltrationPump = new DialyzingUltraFiltrationPump();
		
		public void AddFlows(DialyzingFluidFlowCombinator flowCombinator)
		{
			// The order of the connections matters
			flowCombinator.Connect(DialyzingFluidWaterSupply.MainFlow.Outgoing,
				DialyzingFluidWaterPreparation.MainFlow.Incoming);
			flowCombinator.Connect(DialyzingFluidConcentrateSupply.Concentrate.Outgoing,
				DialyzingFluidPreparation.Concentrate.Incoming);
			flowCombinator.Connect(DialyzingFluidWaterPreparation.MainFlow.Outgoing,
				DialyzingFluidPreparation.DialyzingFluidFlow.Incoming);
			flowCombinator.Connect(DialyzingFluidPreparation.DialyzingFluidFlow.Outgoing,
				BalanceChamber.ProducedDialysingFluid.Incoming);
			flowCombinator.Connect(BalanceChamber.StoredProducedDialysingFluid.Outgoing,
				DialyzingFluidSafetyBypass.MainFlow.Incoming);
			flowCombinator.Connect(DialyzingFluidSafetyBypass.MainFlow.Outgoing,
				ToDialyzer.Incoming);
			flowCombinator.Connect(
				FromDialyzer.Outgoing,
				new PortFlowIn<DialyzingFluid>[] {
					DialyzingUltraFiltrationPump.DialyzingFluidFlow.Incoming,
					BalanceChamber.UsedDialysingFluid.Incoming
				});
			flowCombinator.Connect(
				new PortFlowOut<DialyzingFluid>[] {
					DialyzingFluidSafetyBypass.DrainFlow.Outgoing,
					BalanceChamber.StoredUsedDialysingFluid.Outgoing,
					DialyzingUltraFiltrationPump.DialyzingFluidFlow.Outgoing
				},
				DialyzingFluidDrain.DrainFlow.Incoming);
		}

	}
}
