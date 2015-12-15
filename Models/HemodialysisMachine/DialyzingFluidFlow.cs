using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	// Also called dialysate or dialyzate
	public class DialyzingFluidUnit
	{
	}

	public interface IDialyzingFluidFlowIn : Utilities.IFlowIn<DialyzingFluidUnit>
	{
	}

	public interface IDialyzingFluidFlowOut : Utilities.IFlowOut<DialyzingFluidUnit>
	{
	}

	class DialyzingFluidFlowConnection : Utilities.FlowConnection<DialyzingFluidUnit>
	{
	}

	class DirectDialyzingFluidFlow : Utilities.DirectFlow<DialyzingFluidUnit>, IDialyzingFluidFlowIn, IDialyzingFluidFlowOut
	{
	}
}
