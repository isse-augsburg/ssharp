using System;

namespace HemodialysisMachine.Utilities
{
	using System.Linq;


	// Flow
	//    - Every flow is orchestrated by a Flow class.
	//    - Flows consist of FlowComponents (FlowComposites, FlowSegments, FlowSources, and FlowSinks)
	//    - Refreshes every Cycle
	//    - Every FlowComponent might have outgoing ports (IFlowOut) or incoming ports (IFlowIn)
	//    - First, every UpdateSuction is executed (from Sink to Source)
	//    - Then, every UpdateElement is executed (from Source to Sink)
	//    - Must be acyclic
	// Port
	//    - Consists of two parts (Suction and PushElement)
	//    - Every time the _active_ part should call its part
	//    - For Suction the active part is the respective latter component of the flow
	//    - For PushElement the active part is the respective former component of the flow
	// FlowScheduler
	//    - TODO: could be created by FlowConnector
	// Example:
	//   We have
	//      - a Source source
	//      - a DirectFlow directFlow
	//      - a Sink sink
	//   They are connected
	//      - directFlow.ElementBefore = source.ElementAfterwards (ConnectOutWithIn)
	//      - sink.ElementBefore = directFlow.ElementAfterwards (ConnectOutWithIn)
	//      - directFlow.ElementAfterwards = directFlow.ElementBefore (Behavior of DirectFlow)
	//   Scenario
	//		- source.Update () is called
	//			- source.SourceLambdaFunc is called
	//			- source.ElementOfCurrentCycle is set
	//		- directFlow.Update () is called
	//			- directFlow.ElementBefore(requestedSuction) is called
	//			- source.ElementAfterwards() is called and requestedSuction for the next step is saved in source
	//			- directFlow.FlowLambdaFunc() is called
	//			- directFlow.ElementOfCurrentCycle is set
	//		- sink.Update () is called
	//			- sink.ElementBefore(requestedSuction) is called
	//			- directFlow.ElementAfterwards() is called and requestedSuction for the next step is saved in directFlow
	//			//- sink.SinkLambdaFunc is called
	//			- sink.ElementOfCurrentCycle is set

	// - The direction of the flow _never_ changes. Only the suction
	// - TElement contains the quantity of the element 
	// - ElementBefore(uint suction) contains a suction parameter which valid for the _next_ step
	//   (only pressure difference). A source may have an own output pressure. These values are added.
	// -  Higher pressure of the surrounding leads to suction effect. This suction is propagated to the source
	//    and in the next step this has an effect.
	// - Problem: The requestedSuction needs several ticks to propagate from the sink to the source

	// Comments and further ideas:
	//   - Modelica solves our problems by equations.


	public delegate void SetElementDelegate<TElement>(TElement pushedElement);
	public delegate void SetSuctionDelegate(int suction);



	public class PortFlowIn<TElement> where TElement : struct
	{
		public SetElementDelegate<TElement> SetElement { get; set; }
		public SetSuctionDelegate SetPredecessorSuction { get; set; }
	}

	public class PortFlowOut<TElement> where TElement : struct
	{
		public SetElementDelegate<TElement> SetSuccessorElement { get; set; }
		public SetSuctionDelegate SetSuction { get; set; }
	}

	public interface IFlowIn<TElement> where TElement : struct
	{
		PortFlowIn<TElement> Incoming { get; set; }
	}

	public interface IFlowOut<TElement> where TElement : struct
	{
		PortFlowOut<TElement> Outgoing { get; set; }
	}


	public class PortConnector<TElement> where TElement : struct
	{
		public static void Connect(PortFlowIn<TElement> fromOuter, PortFlowIn<TElement> toInner)
		{
			// ConnectInWithIn
			// Mainly used for CompositeFlows
			// When fromOuter.SetElement is called, then toInner.SetElement should be called.
			// When toInner.SetPredecessorSuction is called, then fromOuter.SetPredecessorSuction should be called.
			if (toInner.SetPredecessorSuction != null)
				throw new Exception("toInner is already connected");
			fromOuter.SetElement = element =>  toInner.SetElement(element);
			toInner.SetPredecessorSuction = suction => fromOuter.SetPredecessorSuction(suction);
		}

		public static void Connect(PortFlowOut<TElement> @from, PortFlowIn<TElement> to)
		{
			// ConnectOutWithIn
			// Flow goes from [@from]-->[to]
			// Suction goes from [to]-->[@from]
			// When from.SetSuccessorElement is called, then to.SetElement should be called.
			// When to.SetPredecessorSuction is called, then from.SetSuction should be called.
			if (from.SetSuccessorElement != null)
				throw new Exception("is already connected");
			from.SetSuccessorElement = element => to.SetElement(element);
			to.SetPredecessorSuction = suction => from.SetSuction(suction);
		}

		public static void Connect(PortFlowOut<TElement> fromInner, PortFlowOut<TElement> toOuter)
		{
			// ConnectOutWithOut
			// Mainly used for CompositeFlows
			// When fromInner.SetSuccessorElement is called, then toOuter.SetSuccessorElement should be called.
			// When toOuter.SetSuction is called, then fromInner.SetSuction should be called.
			if (toOuter.SetSuccessorElement != null)
				throw new Exception("toOuter is already connected");
			fromInner.SetSuccessorElement = element => toOuter.SetSuccessorElement(element);
			toOuter.SetSuction = suction => fromInner.SetSuction(suction);
		}
	}
	
	public abstract class FlowConnector<TElement> where TElement : struct
	{
		public void ConnectInWithIn(FlowComposite<TElement> @from, IFlowIn<TElement> to)
		{
			PortConnector<TElement>.Connect(@from.Incoming,to.Incoming);
		}

		public void ConnectOutWithIn(IFlowOut<TElement> @from, IFlowIn<TElement> to)
		{
			PortConnector<TElement>.Connect(@from.Outgoing, to.Incoming);
		}

		public void ConnectOutWithOut(IFlowOut<TElement> @from, FlowComposite<TElement> to)
		{
			PortConnector<TElement>.Connect(@from.Outgoing, to.Outgoing);
		}
	}

	public class FlowSegment<TElement> : IFlowIn<TElement>, IFlowOut<TElement> where TElement : struct
	{
		public PortFlowIn<TElement> Incoming { get; set; }
		public PortFlowOut<TElement> Outgoing { get; set; }

		public TElement ElementOfCurrentCycle { get; set; }
		public int SuctionOfCurrentCycle { get; set; }
		private Func<TElement, TElement> FlowLambdaFunc { get; set; }
		
		public FlowSegment(Func<TElement, TElement> flowLambdaFunc)
		{
			FlowLambdaFunc = flowLambdaFunc;
			Incoming = new PortFlowIn<TElement>();
			Incoming.SetElement = value => ElementOfCurrentCycle = value;
			Outgoing = new PortFlowOut<TElement>();
			Outgoing.SetSuction = value => SuctionOfCurrentCycle = value;
		}

		public void UpdateSuction()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(SuctionOfCurrentCycle);
		}

		public void UpdateElement()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			ElementOfCurrentCycle = FlowLambdaFunc(ElementOfCurrentCycle);
			Outgoing.SetSuccessorElement(ElementOfCurrentCycle);
		}
	}

	public class FlowSource<TElement> : IFlowOut<TElement>  where TElement : struct
	{
		public PortFlowOut<TElement> Outgoing { get; set; }

		private Func<TElement> SourceLambdaFunc { get; }
		public TElement ElementOfCurrentCycle { get; set; }
		public int SuctionOfCurrentCycle { get; set; }

		public FlowSource(Func<TElement> sourceLambdaFunc)
		{
			SourceLambdaFunc = sourceLambdaFunc;
			Outgoing = new PortFlowOut<TElement>();
			Outgoing.SetSuction = value => SuctionOfCurrentCycle = value;
		}
		
		public void UpdateElement()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			ElementOfCurrentCycle = SourceLambdaFunc();
			Outgoing.SetSuccessorElement(ElementOfCurrentCycle);
		}
	}

	public class FlowSink<TElement> : IFlowIn<TElement> where TElement : struct
	{
		public PortFlowIn<TElement> Incoming { get; set; }

		public int SuctionOfCurrentCycle { get; set; }

		public FlowSink(int initialSuction)
		{
			SuctionOfCurrentCycle = initialSuction;
			Incoming = new PortFlowIn<TElement>();
			Incoming.SetElement = value => ElementOfCurrentCycle = value;
		}

		public FlowSink()
			: this(1) //default is a sink that has a suction of 1 (to let at least anything flow)
		{
		}

		protected TElement ElementOfCurrentCycle { get; private set; }
		
		public void UpdateSuction()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(SuctionOfCurrentCycle);
		}
	}

	public class FlowComposite<TElement> : IFlowIn<TElement>, IFlowOut<TElement> where TElement : struct
	{
		public PortFlowIn<TElement> Incoming { get; set; }
		public PortFlowOut<TElement> Outgoing { get; set; }

		//public int SuctionOfCurrentCycle { get; set; } TODO: Create proxy port, if value is of interest
		//public TElement ElementOfCurrentCycle { get; set; } TODO: Create proxy port, if value is of interest

		public FlowComposite()
		{
			Incoming = new PortFlowIn<TElement>();
			Outgoing = new PortFlowOut<TElement>();
		}
	}
	
	public class FlowVirtualSplitter<TElement> : IFlowIn<TElement> where TElement : struct
	{
		private int Number { get; }
		public PortFlowIn<TElement> Incoming { get; set; }
		public PortFlowOut<TElement>[] Outgoings { get; set; }

		// Elements must be split
		public TElement ElementOfCurrentCycle { get; set; }
		public TElement[] ElementOfCurrentCycles { get; set; }

		// Suctions must be merged
		public int[] SuctionOfCurrentCycles { get; set; }
		public int SuctionOfCurrentCycle { get; set; }

		public FlowVirtualSplitter(int number)
		{
			Number = number;
			Incoming.SetElement = element => ElementOfCurrentCycle = element;
			Incoming = new PortFlowIn<TElement>();
			Outgoings = new PortFlowOut<TElement>[number];
			ElementOfCurrentCycles = new TElement[number];
			for (int i = 0; i < number; i++)
			{
				Outgoings[i] = new PortFlowOut<TElement>();
				Outgoings[i].SetSuction = (value) => SuctionOfCurrentCycles[i] = value;
			}
		}

		public void UpdateSuction()
		{
			// TODO: Update SuctionOfCurrentCycle
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(SuctionOfCurrentCycle);
		}

		public void UpdateElement()
		{
			// TODO: Update ElementOfCurrentCycles[i]
			// Actively Update the ElementOfCurrentCycle of the successors
			for (int i = 0; i < Number; i++)
			{
				Outgoings[i].SetSuccessorElement(ElementOfCurrentCycles[i]);
			}
		}
	}

	public class FlowVirtualMerger<TElement> : IFlowOut<TElement> where TElement : struct
	{
		private int Number { get; }
		public PortFlowIn<TElement>[] Incomings { get; set; }
		public PortFlowOut<TElement> Outgoing { get; set; }

		// Elements must be merged
		public TElement[] ElementOfCurrentCycles { get; set; }
		public TElement ElementOfCurrentCycle { get; set; }

		// Suctions must be split
		public int SuctionOfCurrentCycle { get; set; }
		public int[] SuctionOfCurrentCycles { get; set; }

		public FlowVirtualMerger(int number)
		{
			Number = number;
			Outgoing.SetSuction = suction => SuctionOfCurrentCycle = suction;
			Incomings = new PortFlowIn<TElement>[number];
			Outgoing = new PortFlowOut<TElement>();
			SuctionOfCurrentCycles = new int[number];
			for (int i = 0; i < number; i++)
			{
				Incomings[i] = new PortFlowIn<TElement>();
				Incomings[i].SetElement = (element) => ElementOfCurrentCycles[i] = element;
			}
		}

		public void UpdateSuction()
		{
			// TODO: Update SuctionOfCurrentCycles[i]
			// Actively Update the SuctionOfCurrentCycle of the predecessors
			for (int i = 0; i < Number; i++)
			{
				Incomings[i].SetPredecessorSuction(SuctionOfCurrentCycles[i]);
			}
		}

		public void UpdateElement()
		{
			// TODO: Update ElementOfCurrentCycle
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.SetSuccessorElement(ElementOfCurrentCycle);
		}
	}

	public static class FlowConnectors
	{
		public static TElement[] SplitEqual<TElement>(TElement source, int splitsTotal) where TElement : struct
		{
			return System.Linq.Enumerable.Repeat<TElement>(source, splitsTotal).ToArray();
		}

		public static TElement MergeAny<TElement>(TElement[] sources) where TElement : struct
		{
			return sources[0];
		}
	}

	public class FlowDirect<TElement> : FlowSegment<TElement> where TElement : struct
	{
		public FlowDirect()
			: base( inValue => inValue)
		{
		}
	}
}