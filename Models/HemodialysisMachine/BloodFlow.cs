using System;

namespace HemodialysisMachine
{
	using System.ComponentModel;

	public struct Blood
	{
		public int Quantity;
	}


	class BloodFlowSegment : Utilities.FlowSegment<Blood>
	{
		public BloodFlowSegment(Func<Blood, Blood> flowLambdaFunc)
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

	class BloodFlowDirect : Utilities.FlowDirect<Blood>
	{
	}

	class BloodPortConnector : Utilities.PortConnector<Blood>
	{
	}

	class BloodFlowConnector : Utilities.FlowConnector<Blood>
	{
		public static BloodFlowConnector Instance = new BloodFlowConnector();
	}
}
