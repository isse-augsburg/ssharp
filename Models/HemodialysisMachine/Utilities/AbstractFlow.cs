using System;

namespace HemodialysisMachine.Utilities
{
	using System.Collections.Generic;

	// Flow
	//    - Flows consist of FlowComponents (FlowComposites, FlowSegments, FlowSources, and FlowSinks)
	//    - Every FlowComponent might have outgoing ports (PortFlowOut) or incoming ports (PortFlowIn)
	//    - TElement contains the quantity of the element 
	//    - Higher pressure of the surrounding leads to suction effect. This suction is propagated to the source
	//    - The direction of the flow _never_ changes. Only the suction and the elements
	// PortFlowOut and PortFlowIn 
	//    - Consists of two parts (Suction and Element)
	//    - Every time the _active_ part should call its part
	//    - For Suction the active part is the respective latter component of the flow
	//    - For PushElement the active part is the respective former component of the flow
	// FlowCombinator
	//    - Note: The FlowComponents must be added in correct order starting from the source to the sink
	//    - FlowCombinator.UpdateFlows() calls every UpdateSuctionOfPredecessor and UpdateElementOfSuccessor
	//      in the correct order
	//    - First, every UpdateSuctionOfPredecessor is executed (from Sink to Source)
	//    - Then, every UpdateElementOfSuccessor is executed (from Source to Sink)
	//    - Must be acyclic
	// Example:
	//   We have
	//      - a FlowSource source
	//             Outgoing:PortFlowOut
	//             Outgoing.SetSuction is lambda function with local variables in closure
	//             UpdateElementOfSuccessor()
	//      - a FlowDirect direct
	//             Incoming:PortFlowIn
	//             Outgoing:PortFlowOut
	//             Incoming.SetElement is lambda function with local variables in closure
	//             Outgoing.SetSuction is lambda function with local variables in closure
	//             UpdateSuctionOfPredecessor()
	//             UpdateElementOfSuccessor()
	//      - a FlowSink sink
	//             Incoming:PortFlowIn
	//             Incoming.SetElement is lambda function with local variables in closure
	//             UpdateSuctionOfPredecessor()
	//   They are connected by FlowCombinator flowCombinator
	//      - Connected by FlowCombinator.ConnectOutWithIn(source,direct)
	//             source.SetSuccessorElement = element => direct.SetElement(element);
	//             direct.SetPredecessorSuction = suction => source.SetSuction(suction);
	//      - Connected by FlowCombinator.ConnectOutWithIn(direct,sink)
	//             direct.SetSuccessorElement = element => sink.SetElement(element);
	//             sink.SetPredecessorSuction = suction => direct.SetSuction(suction);
	//   Scenario
	//      - flowCombinator.UpdateFlows() is called
	//          - sink.UpdateSuctionOfPredecessor() is called
	//              - sink.SetPredecessorSuction(x1)
	//              - direct.SetSuction(x1)
	//              - direct.SuctionOfCurrentCycle = x1
	//          - direct.UpdateSuctionOfPredecessor() is called
	//              - direct.SuctionOfCurrentCycle = x2 (might depend on x1)
	//              - direct.SetPredecessorSuction (x2)
	//              - source.SetSuction(x2)
	//              - source.SuctionOfCurrentCycle(x2)
	//          - source.UpdateElementOfSuccessor() is called
	//              - source.ElementOfCurrentCycle = source.SourceLambdaFunc()    (we call the value for short y1)
	//              - source.SetSuccessorElement (y1)
	//              - direct.SetElement(y1)
	//              - direct.ElementOfCurrentCycle = y1
	//          - direct.UpdateElementOfSuccessor() is called
	//              - direct.ElementOfCurrentCycle = direct.FlowLambdaFunc(y1)    (we call the value for short y2)
	//              - direct.SetSuccessorElement (y2)
	//              - sink.SetElement (y2)
	//              - sink.ElementOfCurrentCycle = y2

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
		void UpdateSuctionOfPredecessor();
	}

	public interface IFlowOut<TElement> where TElement : struct
	{
		PortFlowOut<TElement> Outgoing { get; set; }
		void UpdateElementOfSuccessor();
	}
	
	public abstract class FlowCombinator<TElement> where TElement : struct
	{
		private static void Connect(PortFlowOut<TElement> @from, PortFlowIn<TElement> to)
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

		private List<IFlowIn<TElement>> UpdateSuctionOrder;
		private List<IFlowOut<TElement>> UpdateElementOrder;

		// TODO: For generic non-tree like acyclic flows a topological sort is necessary (So, every Update gets executed only once)

		public void UpdateFlows()
		{
			foreach (var flowIn in UpdateSuctionOrder)
			{
				flowIn.UpdateSuctionOfPredecessor();
			}
			foreach (var flowOut in UpdateElementOrder)
			{
				flowOut.UpdateElementOfSuccessor();
			}
		}

		protected FlowCombinator()
		{
			UpdateSuctionOrder=new List<IFlowIn<TElement>>();
			UpdateElementOrder = new List<IFlowOut<TElement>>();
		}

		public void ConnectOutWithIn(IFlowOut<TElement> @from, IFlowIn<TElement> to)
		{
			Connect(@from.Outgoing, to.Incoming);
			UpdateElementOrder.Add(from); //from is the active part
			UpdateSuctionOrder.Insert(0, to); //to is the active part
		}

		public void ConnectOutWithIn(IFlowOut<TElement>[] fromOuts, IFlowIn<TElement> to)
		{
			// fromOuts[] --> Merger --> to
			// Note: Merger.SetSuction (called by to) automatically calls every fromOuts[].SetPredecessorSuction().
			//       Thus, at this point no entry in UpdateSuctionOrder necessary.
			var elementNos = fromOuts.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{
				Connect(fromOuts[0].Outgoing, to.Incoming);
				UpdateElementOrder.Add(fromOuts[0]);
			}
			else
			{
				// create virtual merging component.
				var flowVirtualMerger = new FlowVirtualMerger<TElement>(elementNos);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(fromOuts[i].Outgoing, flowVirtualMerger.Incomings[i]);
					UpdateElementOrder.Add(fromOuts[i]); //Every source is updated first
				}
				Connect(flowVirtualMerger.Outgoing, to.Incoming);
				UpdateElementOrder.Add(flowVirtualMerger); //Element of FlowMerger is updated after each source
			}
			UpdateSuctionOrder.Insert(0, to); //to is the active part
		}

		public void ConnectOutWithIn(IFlowOut<TElement> @from, params IFlowIn<TElement>[] to)
		{
			// from --> Splitter --> to[]
			// Note: Splitter.SetElement (called by from) automatically calls every to[].SetSuccessorElement().
			//       Thus, at this point no entry in UpdateElementOrder necessary.
			UpdateElementOrder.Add(from);
			var elementNos = to.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{
				Connect(@from.Outgoing, to[0].Incoming);
				UpdateSuctionOrder.Insert(0, to[0]);
			}
			else
			{
				// create virtual splitting component.
				var flowVirtualSplitter = new FlowVirtualSplitter<TElement>(elementNos);
				Connect(@from.Outgoing, flowVirtualSplitter.Incoming);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(flowVirtualSplitter.Outgoings[i], to[i].Incoming);
					UpdateSuctionOrder.Insert(0, to[i]);
				}
				UpdateSuctionOrder.Insert(0, flowVirtualSplitter);
			}
		}

		// Special cases with FlowComposite
		// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2

		public void ConnectInWithIn(FlowComposite<TElement> @from, IFlowIn<TElement> to)
		{
			// Connect IncomingProxy with Inner1
			Connect(@from.IncomingProxy, to.Incoming);
			UpdateSuctionOrder.Insert(0, to);
		}

		public void ConnectOutWithOut(IFlowOut<TElement> @from, FlowComposite<TElement> to)
		{
			// Connect Inner1 with OutgoingProxy
			Connect(@from.Outgoing, to.OutgoingProxy);
			UpdateElementOrder.Add(from);
		}

		public void ConnectInWithIn(FlowComposite<TElement> @from, params IFlowIn<TElement>[] to)
		{
			// Connect IncomingProxy with Inner[]
			var elementNos = to.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{
				Connect(@from.IncomingProxy, to[0].Incoming);
				UpdateSuctionOrder.Insert(0, to[0]);
			}
			else
			{
				// create virtual splitting component.
				var flowVirtualSplitter = new FlowVirtualSplitter<TElement>(elementNos);
				Connect(@from.Outgoing, flowVirtualSplitter.Incoming);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(flowVirtualSplitter.Outgoings[i], to[i].Incoming);
					UpdateSuctionOrder.Insert(0, to[i]);
				}
				UpdateSuctionOrder.Insert(0, flowVirtualSplitter);
			}
		}

		public void ConnectOutWithOut(IFlowOut<TElement>[] fromOuts, FlowComposite<TElement> to)
		{
			// Connect Inner[] with OutgoingProxy
			var elementNos = fromOuts.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{
				Connect(fromOuts[0].Outgoing, to.OutgoingProxy);
				UpdateElementOrder.Add(fromOuts[0]); //Every source is updated first
			}
			else
			{
				// create virtual merging component.
				var flowVirtualMerger = new FlowVirtualMerger<TElement>(elementNos);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(fromOuts[i].Outgoing, flowVirtualMerger.Incomings[i]);
					UpdateElementOrder.Add(fromOuts[i]); //Every source is updated first
				}
				Connect(flowVirtualMerger.Outgoing, to.Incoming);
				UpdateElementOrder.Add(flowVirtualMerger); //Element of FlowMerger is updated after each source
			}
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

		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(SuctionOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
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
		
		public void UpdateElementOfSuccessor()
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

		public TElement ElementOfCurrentCycle { get; private set; }
		
		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(SuctionOfCurrentCycle);
		}
	}

	public class FlowComposite<TElement> : IFlowIn<TElement>, IFlowOut<TElement> where TElement : struct
	{
		// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2
		public PortFlowIn<TElement> Incoming { get; set; } //This element is accessed from the outside
		public PortFlowOut<TElement> Outgoing { get; set; } //This element is accessed from the outside

		public PortFlowOut<TElement> IncomingProxy { get; set; } // This element is accessed from the inside
		public PortFlowIn<TElement> OutgoingProxy { get; set; } //This element is accessed from the inside


		//public TElement IncomingElementOfCurrentCycle { get; set; } // Value pushed by the predecessor
		public TElement OutgoingElementOfCurrentCycle { get; set; } // Value pushed to the successor
		//public int IncomingSuctionOfCurrentCycle { get; set; } // Value pushed by the successor
		public int OutgoingSuctionOfCurrentCycle { get; set; } // Value pushed to the predecessor
		
		public FlowComposite()
		{
			// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2

			Incoming = new PortFlowIn<TElement>();
			Outgoing = new PortFlowOut<TElement>();
			OutgoingProxy = new PortFlowIn<TElement>();
			IncomingProxy = new PortFlowOut<TElement>();

			// Ports set external
			//    Incoming.SetPredecessorSuction should be set by ConnectOutWithIn(Outer1,Composite) == Connect(Outer1,Incoming)
			//    Outgoing.SetSuccessorElement should be set by ConnectOutWithIn(Composite,Outer2) == Connect (Outgoing,Outer2)
			//    IncomingProxy.SetSuccessorElement should be set by ConnectInWithIn(Composite,Inner1) == Connect (IncomingProxy,Inner1)
			//    OutgoingProxy.SetPredecessorSuction should be set by ConnectOutWithOut(Inner1,Composite) == Connect (Inner1,OutgoingProxy)

			Incoming.SetElement = element => { IncomingProxy.SetSuccessorElement(element); };
			Outgoing.SetSuction = suction => { OutgoingProxy.SetPredecessorSuction(suction); };
			IncomingProxy.SetSuction = suction => { OutgoingSuctionOfCurrentCycle = suction; };
			OutgoingProxy.SetElement = element => { OutgoingElementOfCurrentCycle = element; };
		}
		
		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(OutgoingSuctionOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.SetSuccessorElement(OutgoingElementOfCurrentCycle);
		}
	}
	
	public class FlowVirtualSplitter<TElement> : IFlowIn<TElement> where TElement : struct
	{
		private int Number { get; }
		public PortFlowIn<TElement> Incoming { get; set; }
		public PortFlowOut<TElement>[] Outgoings { get; set; }

		// Elements must be split
		public TElement ElementOfCurrentCycle { get; set; }
		public TElement[] ElementsOfCurrentCycle { get; set; }

		// Suctions must be merged
		public int[] SuctionsOfCurrentCycle { get; set; }
		public int SuctionOfCurrentCycle { get; set; }

		public FlowVirtualSplitter(int number)
		{
			Number = number;
			Incoming.SetElement = element => { ElementOfCurrentCycle = element;
												 UpdateElementsOfSuccessors();
			};
			SuctionsOfCurrentCycle = new int[number];
			Incoming = new PortFlowIn<TElement>();
			Outgoings = new PortFlowOut<TElement>[number];
			ElementsOfCurrentCycle = new TElement[number];
			for (int i = 0; i < number; i++)
			{
				Outgoings[i] = new PortFlowOut<TElement>();
				Outgoings[i].SetSuction = (value) => SuctionsOfCurrentCycle[i] = value;
			}
		}

		public static void SplitEqual(TElement source, TElement[] targets)
		{
			var number = targets.Length;
			for (int i=0; i < number; i++)
			{
				targets[i] = source;
			}
		}
		public static int MergeAny(int[] sources)
		{
			return sources[0];
		}

		public void UpdateSuctionOfPredecessor()
		{
			// TODO: Update with a dynamic Function
			SuctionOfCurrentCycle=MergeAny(SuctionsOfCurrentCycle);
			Incoming.SetPredecessorSuction(SuctionOfCurrentCycle);
		}

		public void UpdateElementsOfSuccessors()
		{
			// TODO: Update with a dynamic Function
			SplitEqual(ElementOfCurrentCycle, ElementsOfCurrentCycle);
			// Actively Update the ElementOfCurrentCycle of the successors
			for (int i = 0; i < Number; i++)
			{
				Outgoings[i].SetSuccessorElement(ElementsOfCurrentCycle[i]);
			}
		}
	}

	public class FlowVirtualMerger<TElement> : IFlowOut<TElement> where TElement : struct
	{
		private int Number { get; }
		public PortFlowIn<TElement>[] Incomings { get; set; }
		public PortFlowOut<TElement> Outgoing { get; set; }

		// Elements must be merged
		public TElement[] ElementsOfCurrentCycle { get; set; }
		public TElement ElementOfCurrentCycle { get; set; }

		// Suctions must be split
		public int SuctionOfCurrentCycle { get; set; }
		public int[] SuctionsOfCurrentCycle { get; set; }

		public FlowVirtualMerger(int number)
		{
			Number = number;
			Outgoing.SetSuction = suction => { SuctionOfCurrentCycle = suction;
												 UpdateSuctionsOfPredecessors();
			};
			ElementsOfCurrentCycle = new TElement[number];
			Incomings = new PortFlowIn<TElement>[number];
			Outgoing = new PortFlowOut<TElement>();
			SuctionsOfCurrentCycle = new int[number];
			for (int i = 0; i < number; i++)
			{
				Incomings[i] = new PortFlowIn<TElement>();
				Incomings[i].SetElement = (element) => ElementsOfCurrentCycle[i] = element;
			}
		}
		
		public static void SplitEqual(int source, int[] targets)
		{
			var number = targets.Length;
			for (int i = 0; i < number; i++)
			{
				targets[i] = source;
			}
		}

		public static TElement MergeAny(TElement[] sources)
		{
			return sources[0];
		}

		public void UpdateSuctionsOfPredecessors()
		{
			// TODO: Update with a dynamic Function
			// Actively Update the SuctionOfCurrentCycle of the predecessors
			SplitEqual(SuctionOfCurrentCycle, SuctionsOfCurrentCycle);
			for (int i = 0; i < Number; i++)
			{
				Incomings[i].SetPredecessorSuction(SuctionsOfCurrentCycle[i]);
			}
		}

		public void UpdateElementOfSuccessor()
		{
			// TODO: Update with a dynamic Function
			ElementOfCurrentCycle = MergeAny(ElementsOfCurrentCycle);
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.SetSuccessorElement(ElementOfCurrentCycle);
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