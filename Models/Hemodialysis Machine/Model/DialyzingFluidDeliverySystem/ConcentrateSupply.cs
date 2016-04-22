namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

	public class ConcentrateSupply : Component
	{
		public readonly DialyzingFluidFlowSource Concentrate = new DialyzingFluidFlowSource();

		public KindOfDialysate KindOfDialysate = KindOfDialysate.Bicarbonate;

		[Provided]
		public void SetConcentrateFlow(DialyzingFluid outgoing)
		{
			var incomingSuction = Concentrate.Outgoing.Backward;
			outgoing.Quantity = incomingSuction.CustomSuctionValue;
			outgoing.ContaminatedByBlood = false;
			outgoing.Temperature = QualitativeTemperature.TooCold;
			outgoing.WasUsed = false;
			outgoing.KindOfDialysate = KindOfDialysate;
		}
		
		protected override void CreateBindings()
		{
			Concentrate.SendForward=SetConcentrateFlow;
		}
	}
}