using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model
{
	using Modeling;
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
			var incomingSuction = MainFlow.Outgoing.Backward;
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			outgoing.Quantity = incomingSuction.CustomSuctionValue;
			outgoing.ContaminatedByBlood = false;
			outgoing.Temperature = QualitativeTemperature.TooCold;
			outgoing.WasUsed = false;
			outgoing.KindOfDialysate = KindOfDialysate.Water;
		}
		
		protected override void CreateBindings()
		{
			MainFlow.SendForward=SetMainFlow;
		}
	}

	public class DialyzingFluidWaterPreparation : Component
	{
		public readonly DialyzingFluidFlowInToOut MainFlow = new DialyzingFluidFlowInToOut();

		public virtual bool WaterHeaterEnabled()
		{
			return true;
		}

		[Provided]
		public virtual void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
			if (WaterHeaterEnabled())
				outgoing.Temperature = QualitativeTemperature.BodyHeat;
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}

		public readonly Fault WaterHeaterDefect = new TransientFault();

		[FaultEffect(Fault = nameof(WaterHeaterDefect))]
		public class WaterHeaterDefectEffect : DialyzingFluidWaterPreparation
		{
			public override bool WaterHeaterEnabled()
			{
				return false;
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
			var incomingSuction = Concentrate.Outgoing.Backward;
			outgoing.Quantity = incomingSuction.CustomSuctionValue;
			outgoing.ContaminatedByBlood = false;
			outgoing.Temperature = QualitativeTemperature.TooCold;
			outgoing.WasUsed = false;
			outgoing.KindOfDialysate = KindOfDialysate;
		}
		
		protected override void CreateBindings()
		{
			Concentrate.SendForward=SetConcentrateFlow;
		}
	}

	public class DialyzingFluidPreparation : Component
	{
		public readonly DialyzingFluidFlowSink Concentrate = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowInToOut DialyzingFluidFlow = new DialyzingFluidFlowInToOut();

		public KindOfDialysate KindOfDialysate = KindOfDialysate.Water;

		[Range(0, 8, OverflowBehavior.Error)]
		public int PumpSpeed = 0;

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
			Concentrate.SendBackward=SetConcentrateFlowSuction;
			DialyzingFluidFlow.UpdateBackward=SetMainFlowSuction;
			DialyzingFluidFlow.UpdateForward=SetMainFlow;
			Concentrate.ReceivedForward=ReceivedConcentrate;
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
		public readonly DialyzingFluidFlowInToOut DialyzingFluidFlow = new DialyzingFluidFlowInToOut();

		[Range(0, 8, OverflowBehavior.Error)]
		public int UltraFiltrationPumpSpeed = 0;

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
			DialyzingFluidFlow.UpdateBackward=SetMainFlowSuction;
			DialyzingFluidFlow.UpdateForward=SetMainFlow;
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
		
		protected override void CreateBindings()
		{
			DrainFlow.SendBackward=SetMainFlowSuction;
		}
	}

	public class DialyzingFluidSafetyBypass : Component
	{
		public readonly DialyzingFluidFlowInToOut MainFlow = new DialyzingFluidFlowInToOut();
		public readonly DialyzingFluidFlowSource DrainFlow = new DialyzingFluidFlowSource();

		public readonly DialyzingFluid ToDrainValue = new DialyzingFluid();

		public bool BypassEnabled = false;

		[Provided]
		public virtual void SetMainFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			if (BypassEnabled || incoming.Temperature != QualitativeTemperature.BodyHeat)
			{
				ToDrainValue.CopyValuesFrom(incoming);
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
		
		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
			DrainFlow.SendForward=SetDrainFlow;
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
		public readonly DialyzingFluidFlowDelegate ProducedDialysingFluid = new DialyzingFluidFlowDelegate(); //Incoming
		public readonly DialyzingFluidFlowDelegate UsedDialysingFluid = new DialyzingFluidFlowDelegate(); //Incoming
		public readonly DialyzingFluidFlowDelegate StoredProducedDialysingFluid = new DialyzingFluidFlowDelegate(); //Outgoing
		public readonly DialyzingFluidFlowDelegate StoredUsedDialysingFluid = new DialyzingFluidFlowDelegate();//Outgoing

		public readonly DialyzingFluidFlowInToOut ForwardProducedFlowSegment = new DialyzingFluidFlowInToOut();
		public readonly DialyzingFluidFlowInToOut ForwardUsedFlowSegment = new DialyzingFluidFlowInToOut();

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
			ForwardProducedFlowSegment.UpdateForward=ForwardProducedFlow;
			ForwardProducedFlowSegment.UpdateBackward=ForwardProducedFlowSuction;
			ForwardUsedFlowSegment.UpdateForward=ForwardUsedFlow;
			ForwardUsedFlowSegment.UpdateBackward=ForwardUsedFlowSuction;
		}

		public void AddFlows(DialyzingFluidFlowCombinator flowCombinator)
		{//TODO: Check
			flowCombinator.ConnectOutWithIn(ProducedDialysingFluid, ForwardProducedFlowSegment);
			flowCombinator.ConnectOutWithIn(UsedDialysingFluid, ForwardUsedFlowSegment);
			flowCombinator.ConnectOutWithIn(ForwardProducedFlowSegment,StoredProducedDialysingFluid);
			flowCombinator.ConnectOutWithIn(ForwardUsedFlowSegment, StoredUsedDialysingFluid);
		}

		public override void Update()
		{
		}
	}

	public class PumpToBalanceChamber : Component
	{
		public readonly DialyzingFluidFlowInToOut MainFlow = new DialyzingFluidFlowInToOut();

		[Range(0, 8, OverflowBehavior.Error)]
		public int PumpSpeed = 0;

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
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
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
		public readonly DialyzingFluidFlowDelegate FromDialyzer = new DialyzingFluidFlowDelegate();
		public readonly DialyzingFluidFlowDelegate ToDialyzer = new DialyzingFluidFlowDelegate();

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
			flowCombinator.ConnectOutWithIn(DialyzingFluidWaterSupply.MainFlow,
				DialyzingFluidWaterPreparation.MainFlow);
			flowCombinator.ConnectOutWithIn(DialyzingFluidConcentrateSupply.Concentrate,
				DialyzingFluidPreparation.Concentrate);
			flowCombinator.ConnectOutWithIn(DialyzingFluidWaterPreparation.MainFlow,
				DialyzingFluidPreparation.DialyzingFluidFlow);
			flowCombinator.ConnectOutWithIn(DialyzingFluidPreparation.DialyzingFluidFlow,
				BalanceChamber.ProducedDialysingFluid);
			flowCombinator.ConnectOutWithIn(BalanceChamber.StoredProducedDialysingFluid,
				DialyzingFluidSafetyBypass.MainFlow);
			flowCombinator.ConnectOutWithIn(DialyzingFluidSafetyBypass.MainFlow,
				ToDialyzer);
			flowCombinator.ConnectOutWithIns(
				FromDialyzer,
				new IFlowComponentUniqueIncoming<DialyzingFluid, Suction>[] {
					DialyzingUltraFiltrationPump.DialyzingFluidFlow,
					PumpToBalanceChamber.MainFlow
				});
			flowCombinator.ConnectOutWithIn(PumpToBalanceChamber.MainFlow,
				BalanceChamber.UsedDialysingFluid);
			flowCombinator.ConnectOutsWithIn(
				new IFlowComponentUniqueOutgoing<DialyzingFluid, Suction>[] {
					DialyzingFluidSafetyBypass.DrainFlow,
					BalanceChamber.StoredUsedDialysingFluid,
					DialyzingUltraFiltrationPump.DialyzingFluidFlow
				},
				DialyzingFluidDrain.DrainFlow);

			BalanceChamber.AddFlows(flowCombinator);
		}

	}
}
