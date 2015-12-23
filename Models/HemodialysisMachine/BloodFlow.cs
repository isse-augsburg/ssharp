using System;

namespace HemodialysisMachine
{
	using System.ComponentModel;

	public struct Blood
	{
		public int Quantity;
	}


	public class BloodFlowSegment : Utilities.FlowSegment<Blood>
	{
		public BloodFlowSegment(Func<Blood, Blood> flowLambdaFunc)
			: base(flowLambdaFunc)
		{
		}
	}

	public class BloodFlowSource : Utilities.FlowSource<Blood>
	{
		public BloodFlowSource(Func<Blood> sourceLambdaFunc)
			: base(sourceLambdaFunc)
		{
		}
	}

	public class BloodFlowSink : Utilities.FlowSink<Blood>
	{
	}

	public class BloodFlowComposite : Utilities.FlowComposite<Blood>
	{
	}

	public class BloodFlowDirect : Utilities.FlowDirect<Blood>
	{
	}

	public class BloodFlowCombinator : Utilities.FlowCombinator<Blood>
	{
	}
}
