using System;

namespace HemodialysisMachine.Utilities
{
	using System.Linq;

	// - The updates must be called in topological sorted order starting at the source.
	// - Source elements use their Lambda-Function to generate the outgoing value
	// - Flow elements get their incomingValue through a connection (which they retrieve by calling FlowUnitBefore)
	// - Flow elements set their outgoingValue by setting FlowUnitAfterwards using their lambda function.
	// - Sink elements get their incomingValue through a connection (which they retrieve by calling FlowUnitBefore)
	// Example:
	//   We have
	//      - a Source source
	//      - a DirectFlow directFlow
	//      - a Sink sink
	//   They are connected
	//      - directFlow.FlowUnitBefore = source.FlowUnitAfterwards (ConnectOutWithIn)
	//      - sink.FlowUnitBefore = directFlow.FlowUnitAfterwards (ConnectOutWithIn)
	//      - directFlow.FlowUnitAfterwards = directFlow.FlowUnitBefore (Behavior of DirectFlow)
	//   Scenario
	//		- source.Update () is called
	//			- source.SourceLambdaFunc is called
	//			- source.ValueOfCurrentCylce is set
	//		- directFlow.Update () is called
	//			- directFlow.FlowUnitBefore() is called
	//			- source.FlowUnitAfterwards() is called
	//			- directFlow.FlowLambdaFunc() is called
	//			- directFlow.ValueOfCurrentCylce is set
	//		- sink.Update () is called
	//			- sink.FlowUnitBefore() is called
	//			- sink.SinkLambdaFunc is called
	//			- sink.ValueOfCurrentCylce is set


	// Comment: Modelica solves our problems by equations.

	public interface IFlowIn<TUnit>
	{
		Func<TUnit> FlowUnitBefore { get; set; }
	}

	public interface IFlowOut<TUnit>
	{
		Func<TUnit> FlowUnitAfterwards { get; set; }
	}

	public class FlowConnectors
	{
		static TUnit[] SplitEqual<TUnit>(TUnit source, int splitsTotal)
		{
			return System.Linq.Enumerable.Repeat<TUnit>(source, splitsTotal).ToArray();
		}

		static TUnit MergeAny<TUnit>(TUnit[] sources)
		{
			return sources[0];
		}
	}

	public abstract class FlowConnector<TUnit>
	{
		protected abstract TUnit Merger(TUnit[] sources);

		protected abstract TUnit[] Splitter(TUnit source,int splits);

		public void ConnectInWithIn(IFlowIn<TUnit> @from, IFlowIn<TUnit> to)
		{
			if (to!=null)
				throw new Exception("to is already connected");
			to.FlowUnitBefore = from.FlowUnitBefore;
		}

		public void ConnectOutWithIn(IFlowOut<TUnit> @from, IFlowIn<TUnit> to)
		{
			// The FlowUnit the from-component returns is the FlowUnit the to-component receives.
			if (to != null)
				throw new Exception("to is already connected");
			to.FlowUnitBefore = from.FlowUnitAfterwards;
		}

		public void ConnectOutWithIn(IFlowOut<TUnit>[] fromOuts, IFlowIn<TUnit> to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			var fromOutValues = fromOuts.Select(fromOut => fromOut.FlowUnitAfterwards());
			to.FlowUnitBefore = () => Merger(fromOutValues.ToArray());
		}

		public void ConnectOutWithIn(IFlowOut<TUnit> @from, params IFlowIn<TUnit>[] to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			var fromOutValue = @from.FlowUnitAfterwards();
			var toInValues = Splitter(fromOutValue, to.Length);
			for (int i = 0; i < to.Length; i++)
			{
				to[i].FlowUnitBefore = () => toInValues[i];
			}
		}

		public void ConnectOutWithOut(IFlowOut<TUnit> @from, IFlowOut<TUnit> to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			to.FlowUnitAfterwards = from.FlowUnitAfterwards;
		}
	}

	public class Flow<TUnit> : IFlowIn<TUnit>, IFlowOut<TUnit>
	{
		public Func<TUnit> FlowUnitBefore { get; set; }
		public Func<TUnit> FlowUnitAfterwards { get; set; }

		private Func<TUnit,TUnit> FlowLambdaFunc { get; }
		protected TUnit ValueOfCurrentCylce { get; private set; }

		public Flow(Func<TUnit, TUnit> flowLambdaFunc)
		{
			FlowLambdaFunc = flowLambdaFunc;
			FlowUnitAfterwards = () => ValueOfCurrentCylce;
		}

		public void Update()
		{
			var incomingValue = FlowUnitBefore();
			ValueOfCurrentCylce = FlowLambdaFunc(incomingValue);
		}
	}

	public class FlowSource<TUnit> : IFlowOut<TUnit>
	{
		public Func<TUnit> FlowUnitAfterwards { get; set; }

		private Func<TUnit> SourceLambdaFunc { get; }
		protected TUnit ValueOfCurrentCylce { get; private set; }

		public FlowSource(Func<TUnit> sourceLambdaFunc)
		{
			SourceLambdaFunc = sourceLambdaFunc;
			FlowUnitAfterwards = () => ValueOfCurrentCylce;
		}

		public void Update()
		{
			ValueOfCurrentCylce = SourceLambdaFunc();
		}
	}

	public class FlowSink<TUnit> : IFlowIn<TUnit>
	{
		private Action<TUnit> SinkLambdaFunc { get; }

		public Func<TUnit> FlowUnitBefore { get; set; }
		protected TUnit ValueOfCurrentCylce { get; private set; }

		public FlowSink(Action<TUnit> sinkLambdaFunc)
		{
			SinkLambdaFunc = sinkLambdaFunc;
		}

		public void Update()
		{
			var incomingValue = FlowUnitBefore();
			SinkLambdaFunc(incomingValue);
			ValueOfCurrentCylce = incomingValue;
		}
	}

	public class DirectFlow<TUnit> : Flow<TUnit>, IFlowIn<TUnit>, IFlowOut<TUnit>
	{
		public DirectFlow()
			: base( inValue => inValue)
		{
		}
	}
}