using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	using System.Data;

	public class BloodFlowConnection
	{

		public static void ConnectInWithIn(IBloodFlowIn @from, IBloodFlowIn to)
		{
			
		}

		public static void ConnectOutWithIn(IBloodFlowOut @from, IBloodFlowIn to)
		{
			// The FlowUnit the from-component returns is the FlowUnit the to-component receives.
			to.FlowUnitBefore = from.FlowUnitAfterwards;
		}

		public static void ConnectOutWithOut(IBloodFlowOut @from, IBloodFlowOut to)
		{
			// The FlowUnit the from-component returns is the FlowUnit the to-component receives.
			to.FlowUnitAfterwards = from.FlowUnitAfterwards;
		}
	}

	public abstract class DirectBloodFlow : IBloodFlowIn, IBloodFlowOut
	{
		public Func<BloodUnit> FlowUnitBefore { get; set; }

		public Func<BloodUnit> FlowUnitAfterwards { get; set; }

		protected DirectBloodFlow()
		{
			FlowUnitAfterwards = FlowUnitAfterwardsStandard;
		}

		private BloodUnit FlowUnitAfterwardsStandard()
		{
			var incomingValue = FlowUnitBefore();
			return incomingValue;  //Forward InRead to InRead without changing anything
		}
	}
}
