namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;

	public class ArterialBloodPump : Component
	{
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		[Range(0, 8, OverflowBehavior.Error)]
		public int SpeedOfMotor = 0;

		[Provided]
		public void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
		}

		[Provided]
		public virtual void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CustomSuctionValue = SpeedOfMotor; //Force suction set by motor
			toPredecessor.SuctionType=SuctionType.CustomSuction;
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}

		public readonly Fault BloodPumpDefect = new TransientFault();

		[FaultEffect(Fault = nameof(BloodPumpDefect))]
		public class BloodPumpDefectEffect : ArterialBloodPump
		{
			[Provided]
			public override void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
			{
				toPredecessor.CustomSuctionValue = 0; //Force suction set by motor
				toPredecessor.SuctionType = SuctionType.CustomSuction;
			}
		}
	}
}