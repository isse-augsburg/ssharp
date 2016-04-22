namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

	public class WaterPreparation : Component
	{
		public readonly DialyzingFluidFlowInToOut MainFlow = new DialyzingFluidFlowInToOut();

		public virtual bool WaterHeaterEnabled()
		{
			return true;
		}

		[Provided]
		public virtual void SetMainFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
			if (WaterHeaterEnabled())
				toSuccessor.Temperature = QualitativeTemperature.BodyHeat;
		}

		[Provided]
		public void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}

		public readonly Fault WaterHeaterDefect = new TransientFault();

		[FaultEffect(Fault = nameof(WaterHeaterDefect))]
		public class WaterHeaterDefectEffect : WaterPreparation
		{
			public override bool WaterHeaterEnabled()
			{
				return false;
			}
		}
	}
}