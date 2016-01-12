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
	using SafetySharp.Modeling;

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

	// TODO: Inout zu pipe umbenennen. Werte in Ports speichern. Werte in verbundenen ports sollen auf gleiche Stelle referenzieren. 
	// TODO: SetElement: Alternatively: We could save the In-Value.

	public delegate void SetElementDelegate<TElement>(TElement pushedElement);
	public delegate void SetSuctionDelegate(int suction);
	public delegate void UpdateSuctionDelegate();
	public delegate void UpdateElementDelegate();
	public delegate void ResetCycleDelegate();


	public class PortFlowIn<TElement> : Component where TElement : class
	{
		//[Hidden]
		//private PortFlowOut<TElement> ConnectedPredecessor;

		public TElement ElementFromPredecessor;
		public SetSuctionDelegate SetPredecessorSuction { get; set; } //this is the connected predecessor
		public UpdateSuctionDelegate UpdateSuctionOfPredecessor { get; set; } // This is executed to calculate what the predecessor value should be (make changes). To update the predecessor, this.SetPredecessorSuction() must be called in this method.
		//public ResetCycleDelegate ResetValuesOfCurrentCycle;
	}

	public class PortFlowOut<TElement> : Component where TElement : class
	{
		//[Hidden]
		//private PortFlowIn<TElement> ConnectedSuccessor;

		public SetElementDelegate<TElement> SetSuccessorElement { get; set; } //this is the connected successor
		public int SuctionFromSuccessor;
		public UpdateElementDelegate UpdateElementOfSuccessor { get; set; } // This is executed to calculate what the successor value should be (make changes). To update the successor, this.SetSuccessorElement() must be called in this method.
		//public ResetCycleDelegate ResetValuesOfCurrentCycle;
	}

	public interface IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		PortFlowIn<TElement> Incoming { get; set; }
		//void AddSubFlows(FlowCombinator<TElement> _flowCombinator);
	}

	public interface IFlowComponentUniqueOutgoing<TElement> where TElement : class
	{
		PortFlowOut<TElement> Outgoing { get; set; }
	}

	public abstract class FlowCombinator<TElement> : Component where TElement : class
	{
		[Hidden]
		private List<PortFlowIn<TElement>> UpdateSuctionOrder;

		[Hidden]
		private List<PortFlowOut<TElement>> UpdateElementOrder;

		// TODO: For generic non-tree like acyclic flows a topological sort is necessary (So, every Update gets executed only once)

		public override void Update()
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
			UpdateElementOrder=new List<PortFlowOut<TElement>>();
		}
		
		public void Connect(PortFlowOut<TElement> @from, PortFlowIn<TElement> to)
		{
			// Flow goes from [@from]-->[to]
			// Suction goes from [to]-->[@from]
			// When from.SetSuccessorElement is called, then to.SetElement should be called.
			// When to.SetPredecessorSuction is called, then from.SetSuction should be called.
			if (from.SetSuccessorElement != null)
				throw new Exception("is already connected");
			from.SetSuccessorElement = element => to.ElementFromPredecessor = element;
			to.SetPredecessorSuction = suction => from.SuctionFromSuccessor = suction;
			// Add elements to update lists			
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
					Connect(fromOuts[i], flowVirtualMerger.VirtualIncomings[i].Incoming);
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
				Connect(@from, flowVirtualSplitter.Incoming);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(flowVirtualSplitter.VirtualOutgoings[i].Outgoing, to[i]);
				}
			}
		}

		public void Replace(PortFlowIn<TElement> toReplace, PortFlowIn<TElement> replaceBy)
		{
			/*toReplace.SetElement = element => replaceBy.SetElement(element);
			toReplace.UpdateSuctionOfPredecessor = () => replaceBy.UpdateSuctionOfPredecessor();
			replaceBy.SetPredecessorSuction = toReplace.SetPredecessorSuction;
			var i = UpdateSuctionOrder.FindIndex(x => x.Equals(toReplace));
			UpdateSuctionOrder[i] = replaceBy;*/
		}

		public void Replace(PortFlowOut<TElement> toReplace, PortFlowOut<TElement> replaceBy)
		{
			/*toReplace.SetSuction= replaceBy.SetSuction;
			toReplace.UpdateElementOfSuccessor = replaceBy.UpdateElementOfSuccessor;
			replaceBy.SetSuccessorElement = toReplace.SetSuccessorElement;
			var i = UpdateElementOrder.FindIndex(x => x.Equals(toReplace));
			UpdateElementOrder[i] = replaceBy;*/
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
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			// Connect Source with Inner1
			Connect(@from.InternalSource.Outgoing, to.Incoming);
		}

		public void ConnectOutWithOut(IFlowComponentUniqueOutgoing<TElement> @from, FlowComposite<TElement> to)
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			// Connect Inner1 with Sink
			Connect(@from.Outgoing, to.InternalSink.Incoming);
		}
		
		public void ConnectInWithIns(FlowComposite<TElement> @from, params IFlowComponentUniqueIncoming<TElement>[] tos)
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			var collectedPorts = tos.Select(to => to.Incoming).ToArray();
			Connect(from.InternalSource.Outgoing, collectedPorts);
		}

		public void ConnectOutsWithOut(IFlowComponentUniqueOutgoing<TElement>[] fromOuts, FlowComposite<TElement> to)
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			var collectedPorts = fromOuts.Select(from => from.Outgoing).ToArray();
			Connect(collectedPorts, to.InternalSink.Incoming);
		}
	}

	public class FlowInToOutSegment<TElement> : IFlowComponentUniqueOutgoing<TElement>, IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		public PortFlowIn<TElement> Incoming { get; set; }
		public PortFlowOut<TElement> Outgoing { get; set; }
		
		public TElement ElementOutOfCurrentCycle { get; set; }
		public int SuctionOutOfCurrentCycle { get; set; }
		private Func<TElement, TElement> FlowLambdaFunc { get; set; }
		
		public FlowInToOutSegment(Func<TElement, TElement> flowLambdaFunc)
		{
			FlowLambdaFunc = flowLambdaFunc;
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
		}

		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			SuctionOutOfCurrentCycle = Outgoing.SuctionFromSuccessor;
			Incoming.SetPredecessorSuction(SuctionOutOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			ElementOutOfCurrentCycle = FlowLambdaFunc(Incoming.ElementFromPredecessor);
			Outgoing.SetSuccessorElement(ElementOutOfCurrentCycle);
		}
	}

	// TODO: Idea Short Circuit Components:
	//    When flowConnector.Connect(stub.Outgoing,normalA.Incoming)
	//     and flowConnector.Connect(normalB.Outgoing,stub.Incoming)
	//    then flowConnector.Connect(normalB.Outgoing,normalA.Incoming)

	public class FlowSource<TElement> : Component, IFlowComponentUniqueOutgoing<TElement> where TElement : class
	{
		public PortFlowOut<TElement> Outgoing { get; set; }

		private Func<TElement> SourceLambdaFunc { get; }
		public TElement ElementOutOfCurrentCycle { get; set; } //=Element Out

		public FlowSource(Func<TElement> sourceLambdaFunc)
		{
			SourceLambdaFunc = sourceLambdaFunc;
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
		}
		
		public void UpdateElementOfSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			ElementOutOfCurrentCycle = SourceLambdaFunc();
			Outgoing.SetSuccessorElement(ElementOutOfCurrentCycle);
		}
	}

	public class FlowSink<TElement> : Component, IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		public PortFlowIn<TElement> Incoming { get; set; }

		private Func<int> SinkLambdaFunc { get; }
		public int SuctionOutOfCurrentCycle { get; set; }

		public FlowSink(Func<int> sinkLambdaFunc)
		{
			SinkLambdaFunc = sinkLambdaFunc;
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
		}

		public FlowSink()
			: this(()=> 1 ) //default is a sink that has a suction of 1 (to let at least anything flow)
		{
		}
		
		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			SuctionOutOfCurrentCycle = SinkLambdaFunc();
			Incoming.SetPredecessorSuction(SuctionOutOfCurrentCycle);
		}
	}

	public class FlowComposite<TElement> : IFlowComponentUniqueOutgoing<TElement>,  IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
		public PortFlowIn<TElement> Incoming { get; set; } //This element is accessed from the outside
		public PortFlowOut<TElement> Outgoing { get; set; } //This element is accessed from the outside

		public FlowSink<TElement> InternalSink { get; set; } // This element is accessed from the inside
		public FlowSource<TElement> InternalSource { get; set; } //This element is accessed from the inside

		//public TElement ElementInOfCurrentCycle { get; set; } // Value pushed by the predecessor
		//public TElement ElementOutOfCurrentCycle { get; set; } // Value pushed to the successor
		//public int SuctionInOfCurrentCycle { get; set; } // Value pushed by the successor
		//public int SuctionOutOfCurrentCycle { get; set; } // Value pushed to the predecessor

		public FlowComposite()
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
			InternalSink = new FlowSink<TElement>(() => Outgoing.SuctionFromSuccessor);
			InternalSink.Incoming.UpdateSuctionOfPredecessor = () => InternalSink.Incoming.SetPredecessorSuction(Outgoing.SuctionFromSuccessor);
			InternalSource = new FlowSource<TElement>(() => Incoming.ElementFromPredecessor);
			InternalSource.Outgoing.UpdateElementOfSuccessor = () => InternalSource.Outgoing.SetSuccessorElement(Incoming.ElementFromPredecessor);
		}
		
		public void UpdateSuctionOfPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SetPredecessorSuction(InternalSource.Outgoing.SuctionFromSuccessor);
		}

		public void UpdateElementOfSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.SetSuccessorElement(InternalSink.Incoming.ElementFromPredecessor);
		}
	}
	
	public class FlowVirtualSplitter<TElement> : IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		private int Number { get; }
		public PortFlowIn<TElement> Incoming { get; set; }

		public FlowSource<TElement>[] VirtualOutgoings { get; set; }

		// Elements must be split
		public TElement[] ElementsOfCurrentCycle { get; set; }

		public FlowVirtualSplitter(int number)
		{
			Number = number;
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionOfPredecessor = UpdateSuctionOfPredecessor};
			VirtualOutgoings = new FlowSource<TElement>[number];
			ElementsOfCurrentCycle = new TElement[number];
			for (int i = 0; i < number; i++)
			{
				var index = i; // must be added for the closure explicitly. The outer i changes.
				VirtualOutgoings[i] = new FlowSource<TElement>( () => GetElementOfIndex(index) );
			}
		}

		private TElement GetElementOfIndex(int index)
		{
			UpdateElementsOfSuccessors(); // TODO: Execute only once per cycle
			return ElementsOfCurrentCycle[index];
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
			var suctionsOfCurrentCycle = VirtualOutgoings.Select(virtualOutgoing => virtualOutgoing.Outgoing.SuctionFromSuccessor).ToArray();
			var suctionOfCurrentCycle = MergeAny(suctionsOfCurrentCycle);
			Incoming.SetPredecessorSuction(suctionOfCurrentCycle);
		}

		public void UpdateElementsOfSuccessors()
		{
			// TODO: Update with a dynamic Function
			SplitEqual(Incoming.ElementFromPredecessor, ElementsOfCurrentCycle);
			// Set values in ElementsOfCurrentCycle.
			// When Outgoings[i].UpdateElementOfSuccessor is called, these values are used.
		}
	}


	// Note: Merger.SetSuction (called by to) automatically calls every fromOuts[].SetPredecessorSuction().
	//       Thus, at this point no entry in UpdateSuctionOrder necessary.
	public class FlowVirtualMerger<TElement> : IFlowComponentUniqueOutgoing<TElement> where TElement : class
	{
		private int Number { get; }
		public FlowSink<TElement>[] VirtualIncomings { get; set; }
		public PortFlowOut<TElement> Outgoing { get; set; }
		
		// Suctions must be split
		public int[] SuctionsOfCurrentCycle { get; set; }

		public FlowVirtualMerger(int number)
		{
			Number = number;
			VirtualIncomings = new FlowSink<TElement>[number];
			Outgoing = new PortFlowOut<TElement>() {UpdateElementOfSuccessor = UpdateElementOfSuccessor};
			SuctionsOfCurrentCycle = new int[number];
			for (int i = 0; i < number; i++)
			{
				var index = i; // must be added for the closure explicitly. The outer i changes.
				VirtualIncomings[i] = new FlowSink<TElement>(() => GetSuctionOfIndex(index));
			}
		}

		private int GetSuctionOfIndex(int index)
		{
			UpdateSuctionsOfPredecessors(); // TODO: Execute only once per cycle
			return SuctionsOfCurrentCycle[index];
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
			SplitEqual(Outgoing.SuctionFromSuccessor, SuctionsOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
		{
			// TODO: Update with a dynamic Function
			var elementsOfCurrentCycle = VirtualIncomings.Select(virtualIncoming => virtualIncoming.Incoming.ElementFromPredecessor).ToArray();
			var elementOfCurrentCycle = MergeAny(elementsOfCurrentCycle);
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.SetSuccessorElement(elementOfCurrentCycle);
		}
	}

	public class FlowUniqueOutgoingStub<TElement> : Component, IFlowComponentUniqueOutgoing<TElement> where TElement : class
	{
		//   FlowUniqueOutgoingStub:
		//    When flowConnector.Connect(stub.Outgoing,normalA.Incoming)
		//     and flowConnector.Replace(stub.Outgoing,normalB.Outgoing)
		//    then flowConnector.Connect(normalB.Outgoing,normalA.Incoming)
		public PortFlowOut<TElement> Outgoing { get; set; }
		
		public FlowUniqueOutgoingStub()
		{
			Outgoing = new PortFlowOut<TElement>();
		}
	}

	public class FlowUniqueIncomingStub<TElement> : Component, IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		//   FlowUniqueIncomingStub:
		//    When flowConnector.Connect(normalA.Outgoing,stub.Incoming)
		//     and flowConnector.Replace(stub.Incoming,normalB.Incoming)
		//    then flowConnector.Connect(normalA.Outgoing,normalB.Incoming)
		public PortFlowIn<TElement> Incoming { get; set; }
		
		public FlowUniqueIncomingStub()
		{
			Incoming = new PortFlowIn<TElement>();
		}
	}
}
