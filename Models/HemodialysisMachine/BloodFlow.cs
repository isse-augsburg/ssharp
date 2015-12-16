using System;

namespace HemodialysisMachine
{
	using Utilities;

	public class Blood
	{
		public int quantity;
	}

	public interface IBloodFlowIn : Utilities.IFlowIn<Blood>
	{
	}

	public interface IBloodFlowOut : Utilities.IFlowOut<Blood>
	{
	}

	class BloodFlowConnector : Utilities.FlowConnector<Blood>
	{
		public static BloodFlowConnector Connector = new BloodFlowConnector();

		protected override Blood Merger(Blood[] sources)
		{
			return FlowConnectors.MergeAny(sources);
		}

		protected override Blood[] Splitter(Blood source, int splits)
		{
			return FlowConnectors.SplitEqual<Blood>(source, splits);
		}
	}

	class BloodFlow : Utilities.Flow<Blood>, IBloodFlowIn, IBloodFlowOut
	{
		public BloodFlow(Func<Blood, Blood> flowLambdaFunc)
			: base(flowLambdaFunc)
		{
		}
	}

	class BloodFlowSource : Utilities.FlowSource<Blood>, IBloodFlowOut
	{
		public BloodFlowSource(Func<Blood> sourceLambdaFunc)
			: base(sourceLambdaFunc)
		{
		}
	}

	class BloodFlowSink : Utilities.FlowSink<Blood>, IBloodFlowIn
	{
	}

	class CompositeBloodFlow : Utilities.CompositeFlow<Blood>, IBloodFlowIn, IBloodFlowOut
	{
	}

	class DirectBloodFlow : Utilities.DirectFlow<Blood>, IBloodFlowIn, IBloodFlowOut
	{
	}
}