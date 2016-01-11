using System;

namespace HemodialysisMachine.Model
{
	using System.ComponentModel;
	using Utilities;

	struct Blood
	{
		public int Quantity;
	}


	class BloodFlowInToOutSegment : Utilities.FlowInToOutSegment<Blood>
	{
		public BloodFlowInToOutSegment(Func<Blood, Blood> flowLambdaFunc)
			: base(flowLambdaFunc)
		{
		}
	}

	class BloodFlowSource : Utilities.FlowSource<Blood>
	{
		public BloodFlowSource(Func<Blood> sourceLambdaFunc)
			: base(sourceLambdaFunc)
		{
		}
	}

	class BloodFlowSink : Utilities.FlowSink<Blood>
	{
	}

	class BloodFlowComposite : Utilities.FlowComposite<Blood>
	{
	}

	class BloodFlowCombinator : Utilities.FlowCombinator<Blood>
	{
	}

	class BloodFlowUniqueOutgoingStub : FlowUniqueOutgoingStub<Blood>
	{
	}

	class BloodFlowUniqueIncomingStub : FlowUniqueIncomingStub<Blood>
	{
	}
}
