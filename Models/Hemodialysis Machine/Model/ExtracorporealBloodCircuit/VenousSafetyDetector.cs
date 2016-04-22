namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;

	public class VenousSafetyDetector : Component
	{
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		public bool DetectedGasOrContaminatedBlood = false;

		[Provided]
		public virtual void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
			if (fromPredecessor.GasFree == false || fromPredecessor.ChemicalCompositionOk != true)
			{
				DetectedGasOrContaminatedBlood = true;
			}
			else
			{
				DetectedGasOrContaminatedBlood = false;
			}
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

		public readonly Fault SafetyDetectorDefect = new TransientFault();

		[FaultEffect(Fault = nameof(SafetyDetectorDefect))]
		public class SafetyDetectorDefectEffect : VenousSafetyDetector
		{
			[Provided]
			public override void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
			{
				toSuccessor.CopyValuesFrom(fromPredecessor);
				DetectedGasOrContaminatedBlood = false;
			}
		}
	}
}