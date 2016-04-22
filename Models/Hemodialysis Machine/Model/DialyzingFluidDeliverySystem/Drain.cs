namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

	public class Drain : Component
	{
		public readonly DialyzingFluidFlowSink DrainFlow = new DialyzingFluidFlowSink();

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction;
		}
		
		protected override void CreateBindings()
		{
			DrainFlow.SendBackward=SetMainFlowSuction;
		}
	}
}