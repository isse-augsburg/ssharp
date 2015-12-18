using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	using Utilities;

	// Also called dialysate or dialyzate
	public struct DialyzingFluid
	{
		public int Quantity;
	}


	class DialyzingFluidFlowSegment : Utilities.FlowSegment<DialyzingFluid>
	{
		public DialyzingFluidFlowSegment(Func<DialyzingFluid, DialyzingFluid> flowLambdaFunc)
			: base(flowLambdaFunc)
		{
		}
	}

	class DialyzingFluidFlowSource : Utilities.FlowSource<DialyzingFluid>
	{
		public DialyzingFluidFlowSource(Func<DialyzingFluid> sourceLambdaFunc)
			: base(sourceLambdaFunc)
		{
		}
	}

	class DialyzingFluidFlowSink : Utilities.FlowSink<DialyzingFluid>
	{
	}

	class DialyzingFluidFlowComposite : Utilities.FlowComposite<DialyzingFluid>
	{
	}

	class DialyzingFluidFlowDirect : Utilities.FlowDirect<DialyzingFluid>
	{
	}
	class DialyzingFluidConnector : Utilities.PortConnector<DialyzingFluid>
	{
	}
}
