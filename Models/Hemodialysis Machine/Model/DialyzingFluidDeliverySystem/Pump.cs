namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

	public class Pump : Component
	{
		public readonly DialyzingFluidFlowInToOut MainFlow;
		
		public Pump()
		{
			MainFlow = new DialyzingFluidFlowInToOut();
			MainFlow.UpdateBackward = SetMainFlowSuction;
			MainFlow.UpdateForward = SetMainFlow;
		}

		[Range(0, 8, OverflowBehavior.Error)]
		public int PumpSpeed = 0;

		[Provided]
		public void SetMainFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
		}

		[Provided]
		public virtual void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.SuctionType = SuctionType.CustomSuction;
			toPredecessor.CustomSuctionValue = PumpSpeed;
		}
		
		public readonly Fault PumpDefect = new TransientFault();

		[FaultEffect(Fault = nameof(PumpDefect))]
		public class PumpDefectEffect : Pump
		{
			[Provided]
			public override void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
			{
				toPredecessor.SuctionType = SuctionType.CustomSuction;
				toPredecessor.CustomSuctionValue = 0;
			}
		}

	}
}