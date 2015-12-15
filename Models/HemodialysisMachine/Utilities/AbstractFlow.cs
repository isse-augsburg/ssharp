using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Utilities
{
	public interface IFlowIn<TUnit>
	{
		Func<TUnit> FlowUnitBefore { get; set; }
	}

	public interface IFlowOut<TUnit>
	{
		Func<TUnit> FlowUnitAfterwards { get; set; }
	}

	public class FlowConnection<TUnit>
	{

		public static void ConnectInWithIn(IFlowIn<TUnit> @from, IFlowIn<TUnit> to)
		{

		}

		public static void ConnectOutWithIn(IFlowOut<TUnit> @from, IFlowIn<TUnit> to)
		{
			// The FlowUnit the from-component returns is the FlowUnit the to-component receives.
			to.FlowUnitBefore = from.FlowUnitAfterwards;
		}

		public static void ConnectOutWithOut(IFlowOut<TUnit> @from, IFlowOut<TUnit> to)
		{
			// The FlowUnit the from-component returns is the FlowUnit the to-component receives.
			to.FlowUnitAfterwards = from.FlowUnitAfterwards;
		}
	}

	public abstract class DirectFlow<TUnit> : IFlowIn<TUnit>, IFlowOut<TUnit>
	{
		public Func<TUnit> FlowUnitBefore { get; set; }

		public Func<TUnit> FlowUnitAfterwards { get; set; }

		protected DirectFlow()
		{
			FlowUnitAfterwards = FlowUnitAfterwardsStandard;
		}

		private TUnit FlowUnitAfterwardsStandard()
		{
			var incomingValue = FlowUnitBefore();
			return incomingValue;  //Forward InRead to InRead without changing anything
		}
	}
}