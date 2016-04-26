// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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