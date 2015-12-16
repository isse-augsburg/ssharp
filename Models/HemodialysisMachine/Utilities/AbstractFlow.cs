using System;

namespace HemodialysisMachine.Utilities
{
	using System.Linq;

	// - The updates must be called in topological sorted order starting at the source.
	// - Incoming Values of an element must be injected by setting FlowElementBefore
	// - Source elements use their Lambda-Function to generate the outgoing value and setting ValueOfCurrentCylce
	// - Flow elements get their incomingValue through a connection (which they retrieve by calling FlowElementBefore)
	// - Flow elements set their outgoingValue by setting ValueOfCurrentCylce using their lambda function.
	// - Sink elements get their incomingValue through a connection (which they retrieve by calling FlowElementBefore)
	// Example:
	//   We have
	//      - a Source source
	//      - a DirectFlow directFlow
	//      - a Sink sink
	//   They are connected
	//      - directFlow.FlowElementBefore = source.FlowElementAfterwards (ConnectOutWithIn)
	//      - sink.FlowElementBefore = directFlow.FlowElementAfterwards (ConnectOutWithIn)
	//      - directFlow.FlowElementAfterwards = directFlow.FlowElementBefore (Behavior of DirectFlow)
	//   Scenario
	//		- source.Update () is called
	//			- source.SourceLambdaFunc is called
	//			- source.ValueOfCurrentCylce is set
	//		- directFlow.Update () is called
	//			- directFlow.FlowElementBefore() is called
	//			- source.FlowElementAfterwards() is called
	//			- directFlow.FlowLambdaFunc() is called
	//			- directFlow.ValueOfCurrentCylce is set
	//		- sink.Update () is called
	//			- sink.FlowElementBefore() is called
	//			//- sink.SinkLambdaFunc is called
	//			- sink.ValueOfCurrentCylce is set


	// Comment: Modelica solves our problems by equations.

	public interface IFlowIn<TElement>
	{
		Func<TElement> FlowElementBefore { get; set; }
	}

	public interface IFlowOut<TElement>
	{
		TElement FlowElementAfterwards();
	}

	public static class FlowConnectors
	{
		public static TElement[] SplitEqual<TElement>(TElement source, int splitsTotal)
		{
			return System.Linq.Enumerable.Repeat<TElement>(source, splitsTotal).ToArray();
		}

		public static TElement MergeAny<TElement>(TElement[] sources)
		{
			return sources[0];
		}
	}

	public abstract class FlowConnector<TElement>
	{
		protected abstract TElement Merger(TElement[] sources);

		protected abstract TElement[] Splitter(TElement source,int splits);

		public void ConnectInWithIn(CompositeFlow<TElement> @from, IFlowIn<TElement> to)
		{
			// Only works for CompositeFlows
			if (to!=null)
				throw new Exception("to is already connected");
			to.FlowElementBefore = from.FlowElementBefore;
		}


		public void ConnectInWithIn(CompositeFlow<TElement> @from, params IFlowIn<TElement>[] to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			var fromInValue = @from.FlowElementBefore();
			var fromInValues = Splitter(fromInValue, to.Length);
			for (int i = 0; i < to.Length; i++)
			{
				to[i].FlowElementBefore = () => fromInValues[i];
			}
		}

		public void ConnectOutWithIn(IFlowOut<TElement> @from, IFlowIn<TElement> to)
		{
			// The FlowElement the from-component returns is the FlowElement the to-component receives.
			if (to != null)
				throw new Exception("to is already connected");
			to.FlowElementBefore = from.FlowElementAfterwards;
		}

		public void ConnectOutWithIn(IFlowOut<TElement>[] fromOuts, IFlowIn<TElement> to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			var fromOutValues = fromOuts.Select(fromOut => fromOut.FlowElementAfterwards());
			to.FlowElementBefore = () => Merger(fromOutValues.ToArray());
		}

		public void ConnectOutWithIn(IFlowOut<TElement> @from, params IFlowIn<TElement>[] to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			var fromOutValue = @from.FlowElementAfterwards();
			var toInValues = Splitter(fromOutValue, to.Length);
			for (int i = 0; i < to.Length; i++)
			{
				to[i].FlowElementBefore = () => toInValues[i];
			}
		}

		public void ConnectOutWithOut(IFlowOut<TElement> @from, CompositeFlow<TElement> to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			to.LastOutputOfComposition = from.FlowElementAfterwards;
		}

		public void ConnectOutWithOut(IFlowOut<TElement>[] fromOuts, CompositeFlow<TElement> to)
		{
			if (to != null)
				throw new Exception("to is already connected");
			var fromOutValues = fromOuts.Select(fromOut => fromOut.FlowElementAfterwards());
			to.LastOutputOfComposition = () => Merger(fromOutValues.ToArray());
		}
	}

	public class Flow<TElement> : IFlowIn<TElement>, IFlowOut<TElement>
	{
		public Func<TElement> FlowElementBefore { get; set; }

		private Func<TElement,TElement> FlowLambdaFunc { get; }
		protected TElement ValueOfCurrentCylce { get; private set; }

		public TElement FlowElementAfterwards()
		{
			return ValueOfCurrentCylce;
		}

		public Flow(Func<TElement, TElement> flowLambdaFunc)
		{
			FlowLambdaFunc = flowLambdaFunc;
		}

		public void Update()
		{
			var incomingValue = FlowElementBefore();
			ValueOfCurrentCylce = FlowLambdaFunc(incomingValue);
		}
	}

	public class FlowSource<TElement> : IFlowOut<TElement>
	{
		private Func<TElement> SourceLambdaFunc { get; }
		protected TElement ValueOfCurrentCylce { get; private set; }
		
		public TElement FlowElementAfterwards()
		{
			return ValueOfCurrentCylce;
		}

		public FlowSource(Func<TElement> sourceLambdaFunc)
		{
			SourceLambdaFunc = sourceLambdaFunc;
		}

		public void Update()
		{
			ValueOfCurrentCylce = SourceLambdaFunc();
		}
	}

	public class FlowSink<TElement> : IFlowIn<TElement>
	{
		public Func<TElement> FlowElementBefore { get; set; }
		
		protected TElement ValueOfCurrentCylce { get; private set; }
		
		public void Update()
		{
			var incomingValue = FlowElementBefore();
			ValueOfCurrentCylce = incomingValue;
		}
	}

	public class CompositeFlow<TElement> : IFlowIn<TElement>, IFlowOut<TElement>
	{
		// flow declared explicitly by modeler using connections

		public Func<TElement> FlowElementBefore { get; set; }
		public Func<TElement> LastOutputOfComposition { get; set; }

		public TElement ValueOfCurrentCylce { get; set; }
		
		public TElement FlowElementAfterwards()
		{
			return ValueOfCurrentCylce;
		}

		public void Update()
		{
			ValueOfCurrentCylce = LastOutputOfComposition();
		}
	}

	public class DirectFlow<TElement> : Flow<TElement>, IFlowIn<TElement>, IFlowOut<TElement>
	{
		public DirectFlow()
			: base( inValue => inValue)
		{
		}
	}
}