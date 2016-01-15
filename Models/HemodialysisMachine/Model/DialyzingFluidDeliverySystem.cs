using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;
	using Utilities;
	using Utilities.BidirectionalFlow;

	// More details on the balance chamber on the internet:
	// http://principlesofdialysis.weebly.com/uploads/5/6/1/3/5613613/2008ccmodule4.pdf
	// -> Chapter "Volumetric UF Control"

	class DialyzingFluidWaterSupply : Component
	{
		public readonly DialyzingFluidFlowSource MainFlow = new DialyzingFluidFlowSource();
		
		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing)
		{
			var incomingSuction = MainFlow.Outgoing.BackwardFromSuccessor;
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			outgoing.Quantity = incomingSuction.CustomSuctionValue;
			outgoing.ContaminatedByBlood = false;
			outgoing.Temperature = QualitativeTemperature.TooCold;
			outgoing.WasUsed = false;
			outgoing.KindOfDialysate = KindOfDialysate.Water;
		}
		
		[Provided]
		public void ReceivedSuction(Suction incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
			Bind(nameof(MainFlow.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuction));
		}
	}

	class DialyzingFluidWaterPreparation : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment MainFlow = new DialyzingFluidFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
			outgoing.Temperature = QualitativeTemperature.BodyHeat;
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	class DialyzingFluidConcentrateSupply : Component
	{
		public readonly DialyzingFluidFlowSource Concentrate = new DialyzingFluidFlowSource();

		public KindOfDialysate KindOfDialysate = KindOfDialysate.Bicarbonate;

		[Provided]
		public void SetConcentrateFlow(DialyzingFluid outgoing)
		{
			var incomingSuction = Concentrate.Outgoing.BackwardFromSuccessor;
			outgoing.Quantity = incomingSuction.CustomSuctionValue;
			outgoing.ContaminatedByBlood = false;
			outgoing.Temperature = QualitativeTemperature.TooCold;
			outgoing.WasUsed = false;
			outgoing.KindOfDialysate = KindOfDialysate;
		}

		[Provided]
		public void ReceivedSuction(Suction incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Concentrate.SetOutgoingForward), nameof(SetConcentrateFlow));
			Bind(nameof(Concentrate.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuction));
		}
	}

	class DialyzingFluidPreparation : Component
	{
		public readonly DialyzingFluidFlowSink Concentrate = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		public KindOfDialysate KindOfDialysate = KindOfDialysate.Water;

		[Provided]
		public void SetConcentrateFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.CustomSuctionValue = 1;
			outgoingSuction.SuctionType=SuctionType.CustomSuction;
		}

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
			outgoing.KindOfDialysate = KindOfDialysate;
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CustomSuctionValue=4; //Force the pump
			outgoingSuction.SuctionType=SuctionType.CustomSuction;

		}

		[Provided]
		public void ReceivedConcentrate(DialyzingFluid incomingElement)
		{
			KindOfDialysate = incomingElement.KindOfDialysate;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Concentrate.SetOutgoingBackward), nameof(SetConcentrateFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingForward), nameof(SetMainFlow));
			Bind(nameof(Concentrate.ForwardFromPredecessorWasUpdated), nameof(ReceivedConcentrate));
		}
	}

	class DialyzingUltraFiltrationPump : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		public int UltraFiltrationValue = 0;

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.SuctionType = SuctionType.CustomSuction;
			outgoingSuction.CustomSuctionValue = UltraFiltrationValue;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	class DialyzingFluidDrain : Component
	{
		public readonly DialyzingFluidFlowSink DrainFlow = new DialyzingFluidFlowSink();

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction;
		}

		[Provided]
		public void ReceivedDialyzingFluid(DialyzingFluid incomingElement)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DrainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(DrainFlow.ForwardFromPredecessorWasUpdated), nameof(ReceivedDialyzingFluid));
		}
	}

	class DialyzingFluidSafetyBypass : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment MainFlow = new DialyzingFluidFlowInToOutSegment();
		public readonly DialyzingFluidFlowSource DrainFlow = new DialyzingFluidFlowSource();

		public DialyzingFluid ToDrainValue = new DialyzingFluid();

		public bool BypassEnabled = false;

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			if (BypassEnabled || incoming.Temperature != QualitativeTemperature.BodyHeat)
			{
				outgoing.CopyValuesFrom(incoming);
			}
			else
			{
				outgoing.Quantity = 0;
				outgoing.ContaminatedByBlood = false;
				outgoing.Temperature = QualitativeTemperature.TooCold;
				outgoing.WasUsed = false;
				outgoing.KindOfDialysate = KindOfDialysate.Water;
			}
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		[Provided]
		public void SetDrainFlow(DialyzingFluid outgoing)
		{
			outgoing.CopyValuesFrom(ToDrainValue);
		}

		[Provided]
		public void ReceivedSuction(Suction incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
			Bind(nameof(DrainFlow.SetOutgoingForward), nameof(SetDrainFlow));
			Bind(nameof(DrainFlow.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuction));
		}

	}
	
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

		public enum ChamberForDialyzerEnum
		{
			UseChamber1ForDialyzer,
			UseChamber2ForDialyzer
		}

		public ChamberForDialyzerEnum ChamberForDialyzer = ChamberForDialyzerEnum.UseChamber1ForDialyzer;

		public class Chamber
		{
			/*public ValveState ValveToDialyser;
			public ValveState ValveToDrain;
			public ValveState ValveFromDialyzingFluidPreparation;
			public ValveState ValveFromDialyzer;*/
			
			public DialyzingFluid StoredProducedDialysingFluid = new DialyzingFluid();
			public DialyzingFluid StoredUsedProducedDialysingFluid = new DialyzingFluid();

		}

		public Chamber Chamber1 = new Chamber();
		public Chamber Chamber2 = new Chamber();

		public BalanceChamber()
		{
			// Assume we have a rinsed Balance Chamber.
			// Chamber 1 is full of fresh DialysingFluid
			Chamber1.StoredProducedDialysingFluid.Quantity = 12;
			Chamber1.StoredProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber1.StoredProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber1.StoredProducedDialysingFluid.WasUsed = false;
			Chamber1.StoredProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;

			Chamber1.StoredUsedProducedDialysingFluid.Quantity = 0;
			Chamber1.StoredUsedProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber1.StoredUsedProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber1.StoredUsedProducedDialysingFluid.WasUsed = true;
			Chamber1.StoredUsedProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;

			Chamber2.StoredProducedDialysingFluid.Quantity = 0;
			Chamber2.StoredProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber2.StoredProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber2.StoredProducedDialysingFluid.WasUsed = false;
			Chamber2.StoredProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;

			Chamber2.StoredUsedProducedDialysingFluid.Quantity = 12;
			Chamber2.StoredUsedProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber2.StoredUsedProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber2.StoredUsedProducedDialysingFluid.WasUsed = true;
			Chamber2.StoredUsedProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;
		}

		[Provided]
		public void MakeSuctionOnSource(Suction outgoingSuction)
		{
			outgoingSuction.SuctionType=SuctionType.SourceDependentSuction; // The suction depends on the pump before
			outgoingSuction.CustomSuctionValue = 0;
		}

		[Provided]
		public void MakeSuctionOnDrain(Suction outgoingSuction)
		{
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction; // The suction depends on the membrane
			outgoingSuction.CustomSuctionValue = 0;
		}

		[Provided]
		public void PushDialisateToDialysator(DialyzingFluid outgoing)
		{
			var quantityOfIncomingUsedDialysate = UsedDialysingFluid.Incoming.ForwardFromPredecessor.Quantity;
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredProducedDialysingFluid.Quantity >= quantityOfIncomingUsedDialysate)
				{
					outgoing.CopyValuesFrom(Chamber1.StoredProducedDialysingFluid);
					outgoing.Quantity = quantityOfIncomingUsedDialysate;
					Chamber1.StoredProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredProducedDialysingFluid.Quantity >= quantityOfIncomingUsedDialysate)
				{
					outgoing.CopyValuesFrom(Chamber2.StoredProducedDialysingFluid);
					outgoing.Quantity = quantityOfIncomingUsedDialysate;
					Chamber2.StoredProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
		}

		[Provided]
		public void PushDialysateToDrain(DialyzingFluid outgoing)
		{
			var quantityOfFreshDialysate = ProducedDialysingFluid.Incoming.ForwardFromPredecessor.Quantity;
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredUsedProducedDialysingFluid.Quantity >= quantityOfFreshDialysate)
				{
					outgoing.CopyValuesFrom(Chamber1.StoredUsedProducedDialysingFluid);
					outgoing.Quantity = quantityOfFreshDialysate;
					Chamber1.StoredUsedProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredUsedProducedDialysingFluid.Quantity >= quantityOfFreshDialysate)
				{
					outgoing.CopyValuesFrom(Chamber2.StoredUsedProducedDialysingFluid);
					outgoing.Quantity = quantityOfFreshDialysate;
					Chamber2.StoredUsedProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
		}


		[Provided]
		public void ReceivedSuctionOnStoredProducedDialyzingFluid(Suction incomingSuction)
		{
		}

		[Provided]
		public void ReceivedProducedDialyzingFluid(DialyzingFluid incomingElement)
		{
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredProducedDialysingFluid.Quantity <= 20)
				{
					Chamber1.StoredProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredProducedDialysingFluid.Quantity <= 20)
				{
					Chamber2.StoredProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
		}

		[Provided]
		public void ReceivedSuctionOnStoredUsedDialyzingFluid(Suction incomingSuction)
		{
		}

		[Provided]
		public void ReceivedUsedDialyzingFluid(DialyzingFluid incomingElement)
		{
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredUsedProducedDialysingFluid.Quantity <= 20)
				{
					Chamber1.StoredUsedProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredUsedProducedDialysingFluid.Quantity <= 20)
				{
					Chamber2.StoredUsedProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
		}

		protected override void CreateBindings()
		{
			Bind(nameof(ProducedDialysingFluid.SetOutgoingBackward), nameof(MakeSuctionOnSource));
			Bind(nameof(UsedDialysingFluid.SetOutgoingBackward), nameof(MakeSuctionOnDrain));
			Bind(nameof(StoredProducedDialysingFluid.SetOutgoingForward), nameof(PushDialisateToDialysator));
			Bind(nameof(StoredUsedDialysingFluid.SetOutgoingForward), nameof(PushDialysateToDrain));
			Bind(nameof(ProducedDialysingFluid.ForwardFromPredecessorWasUpdated), nameof(ReceivedProducedDialyzingFluid));
			Bind(nameof(UsedDialysingFluid.ForwardFromPredecessorWasUpdated), nameof(ReceivedUsedDialyzingFluid));
			Bind(nameof(StoredProducedDialysingFluid.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuctionOnStoredProducedDialyzingFluid));
			Bind(nameof(StoredUsedDialysingFluid.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuctionOnStoredUsedDialyzingFluid));
		}

		public override void Update()
		{
			if (ChamberForDialyzer==ChamberForDialyzerEnum.UseChamber1ForDialyzer && Chamber1.StoredProducedDialysingFluid.Quantity == 4)
			{
				ChamberForDialyzer = ChamberForDialyzerEnum.UseChamber2ForDialyzer;
			}
			else if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber2ForDialyzer && Chamber2.StoredProducedDialysingFluid.Quantity == 4)
			{
				ChamberForDialyzer = ChamberForDialyzerEnum.UseChamber1ForDialyzer;
			}
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
				new PortFlowIn<DialyzingFluid, Suction>[] {
					DialyzingUltraFiltrationPump.DialyzingFluidFlow.Incoming,
					BalanceChamber.UsedDialysingFluid.Incoming
				});
			flowCombinator.Connect(
				new PortFlowOut<DialyzingFluid, Suction>[] {
					DialyzingFluidSafetyBypass.DrainFlow.Outgoing,
					BalanceChamber.StoredUsedDialysingFluid.Outgoing,
					DialyzingUltraFiltrationPump.DialyzingFluidFlow.Outgoing
				},
				DialyzingFluidDrain.DrainFlow.Incoming);
		}

	}
}
