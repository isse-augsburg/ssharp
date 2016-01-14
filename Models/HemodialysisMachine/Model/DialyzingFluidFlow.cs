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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using Utilities.BidirectionalFlow;
	
	// Also called dialysate or dialyzate
	public class DialyzingFluid : IElement<DialyzingFluid>
	{
		public int Quantity;
		public KindOfDialysate KindOfDialysate;
		public bool ContaminatedByBlood;
		public QualitativeTemperature Temperature;

		public void CopyValuesFrom(DialyzingFluid @from)
		{
			Quantity = from.Quantity;
			KindOfDialysate = from.KindOfDialysate;
			ContaminatedByBlood = from.ContaminatedByBlood;
			Temperature = from.Temperature;
		}
	}


	class DialyzingFluidFlowInToOutSegment : FlowInToOutSegment<DialyzingFluid,Suction>
	{
	}

	class DialyzingFluidFlowSource : FlowSource<DialyzingFluid, Suction>
	{
	}

	class DialyzingFluidFlowSink : FlowSink<DialyzingFluid, Suction>
	{
	}

	class DialyzingFluidFlowComposite : FlowComposite<DialyzingFluid, Suction>
	{
	}


	class DialyzingFluidFlowVirtualSplitter : FlowVirtualSplitter<DialyzingFluid, Suction>, IIntFlowComponent
	{
		public DialyzingFluidFlowVirtualSplitter(int number)
			: base(number)
		{
		}

		public override void SplitForwards(DialyzingFluid source, DialyzingFluid[] targets)
		{
			StandardBehaviorSplitForwardsEqual(source, targets);
		}

		public override void MergeBackwards(Suction[] sources, Suction target)
		{
			StandardBehaviorMergeBackwardsSelectFirst(sources, target);
		}
	}

	class DialyzingFluidFlowVirtualMerger : FlowVirtualMerger<DialyzingFluid, Suction>, IIntFlowComponent
	{
		public DialyzingFluidFlowVirtualMerger(int number)
			: base(number)
		{
		}

		public override void SplitBackwards(Suction source, Suction[] targets)
		{
			StandardBehaviorSplitBackwardsEqual(source, targets);
		}

		public override void MergeForwards(DialyzingFluid[] sources, DialyzingFluid target)
		{
			StandardBehaviorMergeForwardsSelectFirst(sources, target);
		}
	}

	class DialyzingFluidFlowCombinator : FlowCombinator<DialyzingFluid, Suction>, IIntFlowComponent
	{
		public override FlowVirtualMerger<DialyzingFluid, Suction> CreateFlowVirtualMerger(int elementNos)
		{
			return new DialyzingFluidFlowVirtualMerger(elementNos);
		}

		public override FlowVirtualSplitter<DialyzingFluid, Suction> CreateFlowVirtualSplitter(int elementNos)
		{
			return new DialyzingFluidFlowVirtualSplitter(elementNos);
		}
	}

	class DialyzingFluidFlowUniqueOutgoingStub : FlowUniqueOutgoingStub<DialyzingFluid, Suction>
	{
	}

	class DialyzingFluidFlowUniqueIncomingStub : FlowUniqueIncomingStub<DialyzingFluid, Suction>
	{
	}
}
