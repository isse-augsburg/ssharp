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
			Quantity = from.Quantity;
		}

		public Blood()
		{
			
		}
	}


	class BloodFlowInToOutSegment : Utilities.FlowInToOutSegment<Blood>
	{
	}

	class BloodFlowSource : Utilities.FlowSource<Blood>
	{
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
