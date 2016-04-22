namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;

	public class SimplifiedBalanceChamber : Component
	{
		public readonly DialyzingFluidFlowDelegate ProducedDialysingFluid = new DialyzingFluidFlowDelegate(); //Incoming
		public readonly DialyzingFluidFlowDelegate UsedDialysingFluid = new DialyzingFluidFlowDelegate(); //Incoming
		public readonly DialyzingFluidFlowDelegate StoredProducedDialysingFluid = new DialyzingFluidFlowDelegate(); //Outgoing
		public readonly DialyzingFluidFlowDelegate StoredUsedDialysingFluid = new DialyzingFluidFlowDelegate();//Outgoing

		public readonly DialyzingFluidFlowInToOut ForwardProducedFlowSegment = new DialyzingFluidFlowInToOut();
		public readonly DialyzingFluidFlowInToOut ForwardUsedFlowSegment = new DialyzingFluidFlowInToOut();

		public SimplifiedBalanceChamber()
		{
		}

		[Provided]
		public void ForwardProducedFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
		}

		[Provided]
		public void ForwardProducedFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		[Provided]
		public void ForwardUsedFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
		}

		[Provided]
		public void ForwardUsedFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}


		protected override void CreateBindings()
		{
			ForwardProducedFlowSegment.UpdateForward=ForwardProducedFlow;
			ForwardProducedFlowSegment.UpdateBackward=ForwardProducedFlowSuction;
			ForwardUsedFlowSegment.UpdateForward=ForwardUsedFlow;
			ForwardUsedFlowSegment.UpdateBackward=ForwardUsedFlowSuction;
		}

		public void AddFlows(DialyzingFluidFlowCombinator flowCombinator)
		{//TODO: Check
			flowCombinator.ConnectOutWithIn(ProducedDialysingFluid, ForwardProducedFlowSegment);
			flowCombinator.ConnectOutWithIn(UsedDialysingFluid, ForwardUsedFlowSegment);
			flowCombinator.ConnectOutWithIn(ForwardProducedFlowSegment,StoredProducedDialysingFluid);
			flowCombinator.ConnectOutWithIn(ForwardUsedFlowSegment, StoredUsedDialysingFluid);
		}

		public override void Update()
		{
		}
	}
}