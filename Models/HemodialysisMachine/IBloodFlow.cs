using System;

namespace HemodialysisMachine
{
	public interface IBloodFlowIn
	{
		Func<BloodUnit> FlowUnitBefore { get; set; }
	}

	public interface IBloodFlowOut
	{
		Func<BloodUnit> FlowUnitAfterwards { get; set; }
	}
}
