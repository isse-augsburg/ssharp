// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace HemodialysisMachine.Utilities
{
	using System.Collections.Generic;
	using System.Linq;

	// TODO: Update
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
	public delegate void UpdateSuctionDelegate();
	public delegate void UpdateElementDelegate();


	public class PortFlowIn<TElement> where TElement : struct
	{
		public SetElementDelegate<TElement> SetElement { get; set; }
		public SetSuctionDelegate SetPredecessorSuction { get; set; } //this is the connected predecessor
		public UpdateSuctionDelegate UpdateSuctionOfPredecessor { get; set; } // This is executed to calculate what the predecessor value should be (make changes). To update the predecessor, this.SetPredecessorSuction() must be called in this method.
	}

	public class PortFlowOut<TElement> where TElement : struct
	{
		public SetElementDelegate<TElement> SetSuccessorElement { get; set; }
		public SetSuctionDelegate SetSuction { get; set; } //this is the connected successor
		public UpdateElementDelegate UpdateElementOfSuccessor { get; set; } // This is executed to calculate what the successor value should be (make changes). To update the successor, this.SetSuccessorElement() must be called in this method.
	}

	public interface IFlowComponentUniqueIncoming<TElement> where TElement : struct
	{
		PortFlowIn<TElement> Incoming { get; set; }
	}

	public interface IFlowComponentUniqueOutgoing<TElement> where TElement : struct
	{
		PortFlowOut<TElement> Outgoing { get; set; }
	}

	public abstract class FlowCombinator<TElement> where TElement : struct
	{
		private static void ConnectLambdas(PortFlowOut<TElement> @from, PortFlowIn<TElement> to)
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

		private List<PortFlowIn<TElement>> UpdateSuctionOrder;
		private List<PortFlowOut<TElement>> UpdateElementOrder;

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
			UpdateSuctionOrder=new List<PortFlowIn<TElement>>();
			UpdateElementOrder = new List<PortFlowOut<TElement>>();
		}
		
		public void Connect(PortFlowOut<TElement> @from, PortFlowIn<TElement> to)
		{
			ConnectLambdas(from, to);
			UpdateElementOrder.Add(from); //from is the active part
			UpdateSuctionOrder.Insert(0, to); //to is the active part
		}


		// Convenience (split/merge)
		public void Connect(PortFlowOut<TElement>[] fromOuts, PortFlowIn<TElement> to)
		{
			// fromOuts[] --> Merger --> to
			var elementNos = fromOuts.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{
				Connect(fromOuts[0], to);
			}
			else
			{
				// create virtual merging component.
				var flowVirtualMerger = new FlowVirtualMerger<TElement>(elementNos);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(fromOuts[i], flowVirtualMerger.Incomings[i]);
					UpdateElementOrder.Add(fromOuts[i]); //Every source is updated first
				}
				Connect(flowVirtualMerger.Outgoing, to);
			}
		}

		public void Connect(PortFlowOut<TElement> @from, params PortFlowIn<TElement>[] to)
		{
			// from --> Splitter --> to[]
			var elementNos = to.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{
				Connect(@from, to[0]);
			}
			else
			{
				// create virtual splitting component.
				var flowVirtualSplitter = new FlowVirtualSplitter<TElement>(elementNos);
				UpdateSuctionOrder.Insert(0, flowVirtualSplitter.Incoming);
				Connect(@from, flowVirtualSplitter.Incoming);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(flowVirtualSplitter.Outgoings[i], to[i]);
				}
			}
		}


		// Convenience (connections on elements defined here)

		public void ConnectOutWithIn(IFlowComponentUniqueOutgoing<TElement> @from, IFlowComponentUniqueIncoming<TElement> to)
		{
			Connect(from.Outgoing, to.Incoming);
		}

		public void ConnectOutWithIns(IFlowComponentUniqueOutgoing<TElement> @from, params IFlowComponentUniqueIncoming<TElement>[] tos)
		{
			var collectedPorts = tos.Select(to => to.Incoming).ToArray();
			Connect(from.Outgoing, collectedPorts);
		}

		public void ConnectOutsWithIn(IFlowComponentUniqueOutgoing<TElement>[] fromOuts, IFlowComponentUniqueIncoming<TElement> to)
		{
			var collectedPorts = fromOuts.Select(from => from.Outgoing).ToArray();
			Connect(collectedPorts, to.Incoming);
		}

		public void ConnectInWithIn(FlowComposite<TElement> @from, IFlowComponentUniqueIncoming<TElement> to)
		{
			// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2
			// Connect IncomingProxy with Inner1
			Connect(@from.IncomingProxy, to.Incoming);
		}

		public void ConnectOutWithOut(IFlowComponentUniqueOutgoing<TElement> @from, FlowComposite<TElement> to)
		{
			// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2
			// Connect Inner1 with OutgoingProxy
			Connect(@from.Outgoing, to.OutgoingProxy);
		}
		
		public void ConnectInWithIns(FlowComposite<TElement> @from, params IFlowComponentUniqueIncoming<TElement>[] tos)
		{
			// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2
			var collectedPorts = tos.Select(to => to.Incoming).ToArray();
			Connect(from.IncomingProxy, collectedPorts);
		}

		public void ConnectOutsWithOut(IFlowComponentUniqueOutgoing<TElement>[] fromOuts, FlowComposite<TElement> to)
		{
			// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2
			var collectedPorts = fromOuts.Select(from => from.Outgoing).ToArray();
			Connect(collectedPorts, to.OutgoingProxy);
		}
	}

	public class FlowInToOutSegment<TElement> : IFlowComponentUniqueOutgoing<TElement>, IFlowComponentUniqueIncoming<TElement> where TElement : struct
	{
		public PortFlowIn<TElement> Incoming { get; set; }
		public PortFlowOut<TElement> Outgoing { get; set; }

		public TElement ElementInOfCurrentCycle { get; set; }
		public TElement ElementOutOfCurrentCycle { get; set; }
		public int SuctionInOfCurrentCycle { get; set; }
		public int SuctionOutOfCurrentCycle { get; set; }
		private Func<TElement, TElement> FlowLambdaFunc { get; set; }
		
		public FlowInToOutSegment(Func<TElement, TElement> flowLambdaFunc)
		{
			FlowLambdaFunc = flowLambdaFunc;
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
			Incoming.SetElement = value => ElementInOfCurrentCycle = value;
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
			Outgoing.SetSuction = value => SuctionInOfCurrentCycle = value;
		}

		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			SuctionOutOfCurrentCycle = SuctionInOfCurrentCycle;
			Incoming.SetPredecessorSuction(SuctionOutOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			ElementOutOfCurrentCycle = FlowLambdaFunc(ElementInOfCurrentCycle);
			Outgoing.SetSuccessorElement(ElementOutOfCurrentCycle);
		}
	}

	public class FlowSource<TElement> : IFlowComponentUniqueOutgoing<TElement> where TElement : struct
	{
		public PortFlowOut<TElement> Outgoing { get; set; }

		private Func<TElement> SourceLambdaFunc { get; }
		public TElement ElementOutOfCurrentCycle { get; set; } //=Element Out
		public int SuctionInOfCurrentCycle { get; set; } //=Suction In

		public FlowSource(Func<TElement> sourceLambdaFunc)
		{
			SourceLambdaFunc = sourceLambdaFunc;
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
			Outgoing.SetSuction = value => SuctionInOfCurrentCycle = value;
		}
		
		public void UpdateElementOfSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			ElementOutOfCurrentCycle = SourceLambdaFunc();
			Outgoing.SetSuccessorElement(ElementOutOfCurrentCycle);
		}
	}

	public class FlowSink<TElement> : IFlowComponentUniqueIncoming<TElement> where TElement : struct
	{
		public PortFlowIn<TElement> Incoming { get; set; }

		public int SuctionOutOfCurrentCycle { get; set; }

		public FlowSink(int initialSuction)
		{
			SuctionOutOfCurrentCycle = initialSuction;
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
			Incoming.SetElement = value => ElementInOfCurrentCycle = value;
		}

		public FlowSink()
			: this(1) //default is a sink that has a suction of 1 (to let at least anything flow)
		{
		}

		public TElement ElementInOfCurrentCycle { get; private set; }
		
		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(SuctionOutOfCurrentCycle);
		}
	}

	public class FlowComposite<TElement> : IFlowComponentUniqueOutgoing<TElement>,  IFlowComponentUniqueIncoming<TElement> where TElement : struct
	{
		// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2
		public PortFlowIn<TElement> Incoming { get; set; } //This element is accessed from the outside
		public PortFlowOut<TElement> Outgoing { get; set; } //This element is accessed from the outside

		public PortFlowOut<TElement> IncomingProxy { get; set; } // This element is accessed from the inside
		public PortFlowIn<TElement> OutgoingProxy { get; set; } //This element is accessed from the inside


		public TElement ElementInOfCurrentCycle { get; set; } // Value pushed by the predecessor
		public TElement ElementOutOfCurrentCycle { get; set; } // Value pushed to the successor
		public int SuctionInOfCurrentCycle { get; set; } // Value pushed by the successor
		public int SuctionOutOfCurrentCycle { get; set; } // Value pushed to the predecessor

		
		public FlowComposite()
		{
			// Outer1 --> Incoming --> IncomingProxy --> Inner1 --> OutgoingProxy --> Outgoing --> Outer2

			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
			OutgoingProxy = new PortFlowIn<TElement>();
			OutgoingProxy.UpdateSuctionOfPredecessor = () => OutgoingProxy.SetPredecessorSuction(SuctionInOfCurrentCycle);
			IncomingProxy = new PortFlowOut<TElement>();
			IncomingProxy.UpdateElementOfSuccessor = () => IncomingProxy.SetSuccessorElement(ElementInOfCurrentCycle);

			// Ports set external
			//    Incoming.SetPredecessorSuction should be set by ConnectOutWithIn(Outer1,Composite) == Connect(Outer1,Incoming)
			//    Outgoing.SetSuccessorElement should be set by ConnectOutWithIn(Composite,Outer2) == Connect (Outgoing,Outer2)
			//    IncomingProxy.SetSuccessorElement should be set by ConnectInWithIn(Composite,Inner1) == Connect (IncomingProxy,Inner1)
			//    OutgoingProxy.SetPredecessorSuction should be set by ConnectOutWithOut(Inner1,Composite) == Connect (Inner1,OutgoingProxy)

			Incoming.SetElement = element =>
			{
				ElementInOfCurrentCycle = element;
				
			};
			Outgoing.SetSuction = suction =>
			{
				SuctionInOfCurrentCycle = suction;
			};
			IncomingProxy.SetSuction = suction => { SuctionOutOfCurrentCycle = suction; };
			OutgoingProxy.SetElement = element => { ElementOutOfCurrentCycle = element; };
		}
		
		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(SuctionOutOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.SetSuccessorElement(ElementOutOfCurrentCycle);
		}
	}
	
	public class FlowVirtualSplitter<TElement> : IFlowComponentUniqueIncoming<TElement> where TElement : struct
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
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
			Outgoings = new PortFlowOut<TElement>[number];
			ElementsOfCurrentCycle = new TElement[number];
			Incoming.SetElement = element => { ElementOfCurrentCycle = element;
												 UpdateElementsOfSuccessors();
			};
			SuctionsOfCurrentCycle = new int[number];
			for (int i = 0; i < number; i++)
			{
				Outgoings[i] = new PortFlowOut<TElement>();
				var index = i; // must be added for the closure explicitly. The outer i changes.
				Outgoings[i].UpdateElementOfSuccessor = () => Outgoings[index].SetSuccessorElement(ElementsOfCurrentCycle[index]);
				Outgoings[i].SetSuction = value => SuctionsOfCurrentCycle[index] = value;
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
			// Set values in ElementsOfCurrentCycle.
			// When Outgoings[i].UpdateElementOfSuccessor is called, these values are used.
		}
	}


	// Note: Merger.SetSuction (called by to) automatically calls every fromOuts[].SetPredecessorSuction().
	//       Thus, at this point no entry in UpdateSuctionOrder necessary.
	public class FlowVirtualMerger<TElement> : IFlowComponentUniqueOutgoing<TElement> where TElement : struct
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
			ElementsOfCurrentCycle = new TElement[number];
			Incomings = new PortFlowIn<TElement>[number];
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
			Outgoing.SetSuction = suction => { SuctionOfCurrentCycle = suction;
												 UpdateSuctionsOfPredecessors();
			};
			SuctionsOfCurrentCycle = new int[number];
			for (int i = 0; i < number; i++)
			{
				Incomings[i] = new PortFlowIn<TElement>();
				var index = i; // must be added for the closure explicitly. The outer i changes.
				Incomings[i].UpdateSuctionOfPredecessor = () => Incomings[index].SetPredecessorSuction(SuctionsOfCurrentCycle[index]);
				Incomings[i].SetElement = (element) => ElementsOfCurrentCycle[index] = element;
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
			// Set values in SuctionsOfCurrentCycle.
			// When Incomings[i].UpdateSuctionOfPredecessor is called, these values are used.
			SplitEqual(SuctionOfCurrentCycle, SuctionsOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
		{
			// TODO: Update with a dynamic Function
			ElementOfCurrentCycle = MergeAny(ElementsOfCurrentCycle);
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.SetSuccessorElement(ElementOfCurrentCycle);
		}
	}
}
 