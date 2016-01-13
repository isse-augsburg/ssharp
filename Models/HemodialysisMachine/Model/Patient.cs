namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;
	
	class Patient : Component
	{
		// Patient is the source and the sink of blood
		[Hidden]
		public BloodFlowSource ArteryFlow;

		[Hidden]
		public BloodFlowSink VeinFlow;

		public Patient()
		{
			ArteryFlow = new BloodFlowSource();
			VeinFlow = new BloodFlowSink();
		}

		[Provided]
		public void SetArteryFlow(Blood outgoing)
		{
			outgoing.Quantity = 1;
		}

		[Provided]
		public void SetVeinFlowSuction(ref int outgoingSuction)
		{
			outgoingSuction = 1;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(ArteryFlow.SetOutgoingElement), nameof(SetArteryFlow));
			Bind(nameof(VeinFlow.SetOutgoingSuction), nameof(SetVeinFlowSuction));
		}
	}
}
