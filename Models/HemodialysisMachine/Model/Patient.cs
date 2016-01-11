namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;

	class Patient : Component
	{
		// Patient is the source and the sink of blood
		public BloodFlowSource OutArtery;
		public BloodFlowSink Vein;
	}
}
