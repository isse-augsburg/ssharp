namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;

	class Patient : Component
	{
		// Patient is the source and the sink of blood
		[Hidden]
		public BloodFlowSource OutArtery;

		[Hidden]
		public BloodFlowSink Vein;

		public Patient()
		{
			OutArtery = new BloodFlowSource((value) => { });
			Vein = new BloodFlowSink();
		}
	}
}
