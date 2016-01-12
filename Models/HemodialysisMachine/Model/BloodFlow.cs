using System;

namespace HemodialysisMachine.Model
{
	using System.ComponentModel;
	using Utilities;

	class Blood : IElement<Blood>
	{
		public int Quantity;

		public void CopyValuesFrom(Blood from)
		{
			throw new NotImplementedException();
		}

		public Blood()
		{
			
		}
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
		public BloodFlowSource(Action<Blood> sourceLambdaFunc)
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
