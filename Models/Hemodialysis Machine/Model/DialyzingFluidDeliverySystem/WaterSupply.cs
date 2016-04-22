namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

	public class WaterSupply : Component
	{
		public readonly DialyzingFluidFlowSource MainFlow = new DialyzingFluidFlowSource();
		
		[Provided]
		public void SetMainFlow(DialyzingFluid outgoing)
		{
			var incomingSuction = MainFlow.Outgoing.Backward;
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			outgoing.Quantity = incomingSuction.CustomSuctionValue;
			outgoing.ContaminatedByBlood = false;
			outgoing.Temperature = QualitativeTemperature.TooCold;
			outgoing.WasUsed = false;
			outgoing.KindOfDialysate = KindOfDialysate.Water;
		}
		
		protected override void CreateBindings()
		{
			MainFlow.SendForward=SetMainFlow;
		}
	}
}