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
			outgoing.UnfiltratedBloodUnits = 1;
		}

		[Provided]
		public void SetVeinFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.Value = 1;
		}


		[Provided]
		public void VeinReceivedBlood(Blood incomingElement)
		{
			//System.Console.WriteLine("Patient received Blood");
		}

		[Provided]
		public void ArteryReceivedSuction(Suction incomingSuction)
		{
			//System.Console.WriteLine("Received Suction: " + incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(ArteryFlow.SetOutgoingForward), nameof(SetArteryFlow));
			Bind(nameof(VeinFlow.SetOutgoingBackward), nameof(SetVeinFlowSuction));
			Bind(nameof(ArteryFlow.BackwardFromSuccessorWasUpdated), nameof(ArteryReceivedSuction));
			Bind(nameof(VeinFlow.ForwardFromPredecessorWasUpdated), nameof(VeinReceivedBlood));
		}
	}
}
