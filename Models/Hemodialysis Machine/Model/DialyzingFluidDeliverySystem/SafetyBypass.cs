namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

	public class SafetyBypass : Component
	{
		public readonly DialyzingFluidFlowInToOut MainFlow = new DialyzingFluidFlowInToOut();
		public readonly DialyzingFluidFlowSource DrainFlow = new DialyzingFluidFlowSource();

		public readonly DialyzingFluid ToDrainValue = new DialyzingFluid();

		public bool BypassEnabled = false;

		[Provided]
		public virtual void SetMainFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
		{
			if (BypassEnabled || fromPredecessor.Temperature != QualitativeTemperature.BodyHeat)
			{
				ToDrainValue.CopyValuesFrom(fromPredecessor);
				toSuccessor.Quantity = 0;
				toSuccessor.ContaminatedByBlood = false;
				toSuccessor.Temperature = QualitativeTemperature.TooCold;
				toSuccessor.WasUsed = false;
				toSuccessor.KindOfDialysate = KindOfDialysate.Water;
			}
			else
			{
				toSuccessor.CopyValuesFrom(fromPredecessor);
			}
		}

		[Provided]
		public void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
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
		public class SafetyBypassFaultEffect : SafetyBypass
		{
			[Provided]
			public override void SetMainFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
			{
				toSuccessor.CopyValuesFrom(fromPredecessor);
			}
		}
	}
}