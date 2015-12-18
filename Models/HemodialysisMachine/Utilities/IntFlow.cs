using System;

namespace HemodialysisMachine.Utilities
{
	class IntFlowSegment : Utilities.FlowSegment<int>
	{
		public IntFlowSegment(Func<int, int> flowLambdaFunc)
			: base(flowLambdaFunc)
		{
		}
	}

	class IntFlowSource : Utilities.FlowSource<int>
	{
		public IntFlowSource(Func<int> sourceLambdaFunc)
			: base(sourceLambdaFunc)
		{
		}
	}

	class IntFlowSink : Utilities.FlowSink<int>
	{
	}

	class IntFlowComposite : Utilities.FlowComposite<int>
	{
	}

	class IntFlowDirect : Utilities.FlowDirect<int>
	{
	}
	class IntFlowCombinator : Utilities.FlowCombinator<int>
	{
	}
}