namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;

	public class ArtierialChamber : Component
	{
		// Drip Chamber
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		[Provided]
		public void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
			toSuccessor.GasFree = true;
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
	}
}