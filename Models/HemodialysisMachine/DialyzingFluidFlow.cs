using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	using Utilities;

	// Also called dialysate or dialyzate
	public class DialyzingFluid
	{
	}
	public interface IDialyzingFluidFlowIn : Utilities.IFlowIn<DialyzingFluid>
	{
	}

	public interface IDialyzingFluidFlowOut : Utilities.IFlowOut<DialyzingFluid>
	{
	}

	class DialyzingFluidFlowConnector : Utilities.FlowConnector<DialyzingFluid>
	{
		public static DialyzingFluidFlowConnector Connector = new DialyzingFluidFlowConnector();

		protected override DialyzingFluid Merger(DialyzingFluid[] sources)
		{
			return FlowConnectors.MergeAny(sources);
		}

		protected override DialyzingFluid[] Splitter(DialyzingFluid source, int splits)
		{
			return FlowConnectors.SplitEqual<DialyzingFluid>(source, splits);
		}
	}

	class DialyzingFluidFlow : Utilities.Flow<DialyzingFluid>, IDialyzingFluidFlowIn, IDialyzingFluidFlowOut
	{
		public DialyzingFluidFlow(Func<DialyzingFluid, DialyzingFluid> flowLambdaFunc)
			: base(flowLambdaFunc)
		{
		}
	}

	class DialyzingFluidFlowSource : Utilities.FlowSource<DialyzingFluid>, IDialyzingFluidFlowOut
	{
		public DialyzingFluidFlowSource(Func<DialyzingFluid> sourceLambdaFunc)
			: base(sourceLambdaFunc)
		{
		}
	}

	class DialyzingFluidFlowSink : Utilities.FlowSink<DialyzingFluid>, IDialyzingFluidFlowIn
	{
	}


	class CompositeDialyzingFluidFlow : Utilities.CompositeFlow<DialyzingFluid>, IDialyzingFluidFlowIn, IDialyzingFluidFlowOut
	{
	}

	class DirectDialyzingFluidFlow : Utilities.DirectFlow<DialyzingFluid>, IDialyzingFluidFlowIn, IDialyzingFluidFlowOut
	{
	}
}
