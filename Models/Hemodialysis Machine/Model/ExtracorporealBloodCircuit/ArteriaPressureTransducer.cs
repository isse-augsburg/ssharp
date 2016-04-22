namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;

	public class ArteriaPressureTransducer : Component
	{
		public readonly BloodFlowSink SenseFlow = new BloodFlowSink();
		
		public QualitativePressure SensedPressure = QualitativePressure.NoPressure;

		[Provided]
		public void SetSenseFlowSuction(Suction toPredecessor)
		{
			toPredecessor.CustomSuctionValue = 0;
			toPredecessor.SuctionType = SuctionType.CustomSuction;
		}

		[Provided]
		public void ReceivedBlood(Blood incomingElement)
		{
			SensedPressure = incomingElement.Pressure;
		}

		protected override void CreateBindings()
		{
			SenseFlow.SendBackward=SetSenseFlowSuction;
			SenseFlow.ReceivedForward=ReceivedBlood;
		}
	}
}