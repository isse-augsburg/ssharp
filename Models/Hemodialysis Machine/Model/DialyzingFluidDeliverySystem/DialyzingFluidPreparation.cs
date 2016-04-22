namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

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
		public void SetMainFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
			toSuccessor.KindOfDialysate = KindOfDialysate;
		}

		[Provided]
		public virtual void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CustomSuctionValue= PumpSpeed; //Force the pump
			toPredecessor.SuctionType=SuctionType.CustomSuction;
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
			public override void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
			{
				toPredecessor.CustomSuctionValue = 0;
				toPredecessor.SuctionType = SuctionType.CustomSuction;
			}
		}
	}
}