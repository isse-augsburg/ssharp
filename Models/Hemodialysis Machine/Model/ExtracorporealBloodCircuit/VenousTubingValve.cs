namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;

	public class VenousTubingValve : Component
	{
		// HACK: To be able to react in time we delay the BloodFlow
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		public ValveState ValveState = ValveState.Open;

		public readonly BufferedBlood DelayedBlood = new BufferedBlood();

		[Provided]
		public void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			if (ValveState == ValveState.Open)
			{
				toSuccessor.CopyValuesFrom(DelayedBlood);
			}
			else
			{
				toSuccessor.HasHeparin = true;
				toSuccessor.Water = 0;
				toSuccessor.SmallWasteProducts = 0;
				toSuccessor.BigWasteProducts = 0;
				toSuccessor.ChemicalCompositionOk = true;
				toSuccessor.GasFree = true;
				toSuccessor.Pressure = QualitativePressure.NoPressure;
				toSuccessor.Temperature = QualitativeTemperature.BodyHeat;
			}
			DelayedBlood.CopyValuesFrom(fromPredecessor);
		}

		[Provided]
		public void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		public virtual void CloseValve()
		{
			ValveState = ValveState.Closed;
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}

		public readonly Fault ValveDoesNotClose = new TransientFault();

		[FaultEffect(Fault = nameof(ValveDoesNotClose))]
		public class ValveDoesNotCloseEffect : VenousTubingValve
		{
			public override void CloseValve()
			{
			}
		}
	}
}