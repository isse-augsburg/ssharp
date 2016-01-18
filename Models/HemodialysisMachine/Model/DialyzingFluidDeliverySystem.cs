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

	public class DialyzingFluidWaterSupply : Component
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

	public class DialyzingFluidWaterPreparation : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment MainFlow = new DialyzingFluidFlowInToOutSegment();

		[Provided]
		public virtual void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
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

		public readonly Fault WaterHeaterDefect = new TransientFault();

		[FaultEffect(Fault = nameof(WaterHeaterDefect))]
		public class WaterHeaterDefectEffect : DialyzingFluidWaterPreparation
		{
			[Provided]
			public override void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
			{
				outgoing.CopyValuesFrom(incoming);
			}
		}
	}

	public class DialyzingFluidConcentrateSupply : Component
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

	public class DialyzingFluidPreparation : Component
	{
		public readonly DialyzingFluidFlowSink Concentrate = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		public KindOfDialysate KindOfDialysate = KindOfDialysate.Water;

		public int PumpSpeed = 4;

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
		public virtual void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CustomSuctionValue= PumpSpeed; //Force the pump
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

		public readonly Fault DialyzingFluidPreparationPumpDefect = new TransientFault();

		[FaultEffect(Fault = nameof(DialyzingFluidPreparationPumpDefect))]
		public class DialyzingFluidPreparationPumpDefectEffect : DialyzingFluidPreparation
		{
			[Provided]
			public override void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
			{
				outgoingSuction.CustomSuctionValue = 0;
				outgoingSuction.SuctionType = SuctionType.CustomSuction;
			}
		}
	}

	public class DialyzingUltraFiltrationPump : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		public int UltraFiltrationPumpSpeed = 1;

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.SuctionType = SuctionType.CustomSuction;
			outgoingSuction.CustomSuctionValue = UltraFiltrationPumpSpeed;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	public class DialyzingFluidDrain : Component
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

	public class DialyzingFluidSafetyBypass : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment MainFlow = new DialyzingFluidFlowInToOutSegment();
		public readonly DialyzingFluidFlowSource DrainFlow = new DialyzingFluidFlowSource();

		public DialyzingFluid ToDrainValue = new DialyzingFluid();

		public bool BypassEnabled = false;

		[Provided]
		public virtual void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			if (BypassEnabled || incoming.Temperature != QualitativeTemperature.BodyHeat)
			{
				outgoing.Quantity = 0;
				outgoing.ContaminatedByBlood = false;
				outgoing.Temperature = QualitativeTemperature.TooCold;
				outgoing.WasUsed = false;
				outgoing.KindOfDialysate = KindOfDialysate.Water;
			}
			else
			{
				outgoing.CopyValuesFrom(incoming);
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


		public readonly Fault SafetyBypassFault = new TransientFault();

		[FaultEffect(Fault = nameof(SafetyBypassFault))]
		public class SafetyBypassFaultEffect : DialyzingFluidSafetyBypass
		{
			[Provided]
			public override void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
			{
				outgoing.CopyValuesFrom(incoming);
			}
		}
	}

	public class SimplifiedBalanceChamber : Component
	{
		public readonly DialyzingFluidFlowUniqueIncomingStub ProducedDialysingFluid = new DialyzingFluidFlowUniqueIncomingStub();
		public readonly DialyzingFluidFlowUniqueIncomingStub UsedDialysingFluid = new DialyzingFluidFlowUniqueIncomingStub();
		public readonly DialyzingFluidFlowUniqueOutgoingStub StoredProducedDialysingFluid = new DialyzingFluidFlowUniqueOutgoingStub();
		public readonly DialyzingFluidFlowUniqueOutgoingStub StoredUsedDialysingFluid = new DialyzingFluidFlowUniqueOutgoingStub();
		
		public readonly DialyzingFluidFlowInToOutSegment ForwardProducedFlowSegment = new DialyzingFluidFlowInToOutSegment();
		public readonly DialyzingFluidFlowInToOutSegment ForwardUsedFlowSegment = new DialyzingFluidFlowInToOutSegment();

		public SimplifiedBalanceChamber()
		{
		}

		[Provided]
		public void ForwardProducedFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void ForwardProducedFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		[Provided]
		public void ForwardUsedFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void ForwardUsedFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}


		protected override void CreateBindings()
		{
			Bind(nameof(ForwardProducedFlowSegment.SetOutgoingForward), nameof(ForwardProducedFlow));
			Bind(nameof(ForwardProducedFlowSegment.SetOutgoingBackward), nameof(ForwardProducedFlowSuction));
			Bind(nameof(ForwardUsedFlowSegment.SetOutgoingForward), nameof(ForwardUsedFlow));
			Bind(nameof(ForwardUsedFlowSegment.SetOutgoingBackward), nameof(ForwardUsedFlowSuction));
		}

		public void AddFlows(DialyzingFluidFlowCombinator flowCombinator)
		{
			flowCombinator.Replace(ProducedDialysingFluid.Incoming, ForwardProducedFlowSegment.Incoming);
			flowCombinator.Replace(UsedDialysingFluid.Incoming, ForwardUsedFlowSegment.Incoming);
			flowCombinator.Replace(StoredProducedDialysingFluid.Outgoing, ForwardProducedFlowSegment.Outgoing);
			flowCombinator.Replace(StoredUsedDialysingFluid.Outgoing, ForwardUsedFlowSegment.Outgoing);
		}

		public override void Update()
		{
		}
	}

	public class PumpToBalanceChamber : Component
	{
		public readonly DialyzingFluidFlowInToOutSegment MainFlow = new DialyzingFluidFlowInToOutSegment();

		public int PumpSpeed = 4;

		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public virtual void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CustomSuctionValue = PumpSpeed; //Force the pump
			outgoingSuction.SuctionType = SuctionType.CustomSuction;
		}
		

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}

		public readonly Fault PumpToBalanceChamberDefect = new TransientFault();

		[FaultEffect(Fault = nameof(PumpToBalanceChamberDefect))]
		public class PumpToBalanceChamberDefectEffect : PumpToBalanceChamber
		{
			[Provided]
			public override void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
			{
				outgoingSuction.CustomSuctionValue = 0;
				outgoingSuction.SuctionType = SuctionType.CustomSuction;
			}
		}
	}


	// Also called dialyzing fluid delivery system
	public class DialyzingFluidDeliverySystem : Component
	{
		public readonly DialyzingFluidFlowUniqueOutgoingStub FromDialyzer = new DialyzingFluidFlowUniqueOutgoingStub();
		public readonly DialyzingFluidFlowUniqueIncomingStub ToDialyzer = new DialyzingFluidFlowUniqueIncomingStub();

		public readonly DialyzingFluidWaterSupply DialyzingFluidWaterSupply = new DialyzingFluidWaterSupply();
		public readonly DialyzingFluidWaterPreparation DialyzingFluidWaterPreparation = new DialyzingFluidWaterPreparation();
		public readonly DialyzingFluidConcentrateSupply DialyzingFluidConcentrateSupply = new DialyzingFluidConcentrateSupply();
		public readonly DialyzingFluidPreparation DialyzingFluidPreparation = new DialyzingFluidPreparation();
		public readonly SimplifiedBalanceChamber BalanceChamber = new SimplifiedBalanceChamber();
		public readonly PumpToBalanceChamber PumpToBalanceChamber = new PumpToBalanceChamber();
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
					PumpToBalanceChamber.MainFlow.Incoming
				});
			flowCombinator.Connect(PumpToBalanceChamber.MainFlow.Outgoing,
				BalanceChamber.UsedDialysingFluid.Incoming);
			flowCombinator.Connect(
				new PortFlowOut<DialyzingFluid, Suction>[] {
					DialyzingFluidSafetyBypass.DrainFlow.Outgoing,
					BalanceChamber.StoredUsedDialysingFluid.Outgoing,
					DialyzingUltraFiltrationPump.DialyzingFluidFlow.Outgoing
				},
				DialyzingFluidDrain.DrainFlow.Incoming);

			BalanceChamber.AddFlows(flowCombinator);
		}

	}
}
