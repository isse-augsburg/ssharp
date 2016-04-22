namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;

	public class HeparinPump : Component
	{
		public readonly BloodFlowSource HeparinFlow = new BloodFlowSource();

		public readonly bool Enabled = true;

		[Provided]
		public void SetHeparinFlow(Blood outgoing)
		{
			outgoing.HasHeparin = true;
			outgoing.Water = 0;
			outgoing.SmallWasteProducts = 0;
			outgoing.BigWasteProducts = 0;
			outgoing.ChemicalCompositionOk = true;
			outgoing.GasFree = true;
			outgoing.Pressure = QualitativePressure.NoPressure;
			outgoing.Temperature = QualitativeTemperature.BodyHeat;
		}

		[Provided]
		public void ReceivedSuction(Suction fromSuccessor)
		{
		}

		protected override void CreateBindings()
		{
			HeparinFlow.SendForward=SetHeparinFlow;
			HeparinFlow.ReceivedBackward=ReceivedSuction;
		}
	}
}