using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	public class BloodUnit
	{
	}

	public interface IBloodFlowIn : Utilities.IFlowIn<BloodUnit>
	{
	}

	public interface IBloodFlowOut : Utilities.IFlowOut<BloodUnit>
	{
	}

	class BloodFlowConnection : Utilities.FlowConnection<BloodUnit>
	{
	}

	class DirectBloodFlow : Utilities.DirectFlow<BloodUnit>, IBloodFlowIn, IBloodFlowOut
	{
	}
}