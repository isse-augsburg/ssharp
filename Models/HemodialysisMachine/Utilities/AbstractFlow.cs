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
	//    - FlowCombinator.UpdateFlows() calls every UpdateSuctionToPredecessor and UpdateElementToSuccessor
	//      in the correct order
	//    - First, every UpdateSuctionToPredecessor is executed (from Sink to Source)
	//    - Then, every UpdateElementToSuccessor is executed (from Source to Sink)
	//    - Must be acyclic
	
	public delegate void SetElementDelegate<TElement>(TElement pushedElement);
	public delegate void SetSuctionDelegate(int suction);
	public delegate void UpdateSuctionDelegate();
	public delegate void UpdateElementDelegate();
	//public delegate void ResetCycleDelegate();


	public class PortFlowIn<TElement> : Component where TElement : class
	{
		[Hidden]
		public PortFlowOut<TElement> ConnectedPredecessor;

		[Hidden] // Only save "To"-Values
		public TElement ElementFromPredecessor;

		public int SuctionToPredecessor;
		public UpdateSuctionDelegate UpdateSuctionToPredecessor { get; set; } // This is executed to calculate what the predecessor value should be (make changes). To update the predecessor, this.SetPredecessorSuction() must be called in this method.
		//public ResetCycleDelegate ResetValuesOfCurrentCycle;
	}

	public class PortFlowOut<TElement> : Component where TElement : class
	{
		[Hidden]
		public PortFlowIn<TElement> ConnectedSuccessor;

		[Hidden] // Only save "To"-Values
		public TElement ElementToSuccessor;

		public int SuctionFromSuccessor;
		public UpdateElementDelegate UpdateElementToSuccessor { get; set; } // This is executed to calculate what the successor value should be (make changes). To update the successor, this.SetSuccessorElement() must be called in this method.
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
				flowIn.UpdateSuctionToPredecessor();
				flowIn.ConnectedPredecessor.SuctionFromSuccessor = flowIn.SuctionToPredecessor;
			}
			foreach (var flowOut in UpdateElementOrder)
			{
				flowOut.UpdateElementToSuccessor();
				flowOut.ConnectedSuccessor.ElementFromPredecessor = flowOut.ElementToSuccessor;
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
			if (from.ConnectedSuccessor != null || to.ConnectedPredecessor!=null)
				throw new Exception("is already connected");
			from.ConnectedSuccessor = to;
			to.ConnectedPredecessor = from;
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
			if (toReplace.ConnectedPredecessor != null)
			{
				replaceBy.ConnectedPredecessor = toReplace.ConnectedPredecessor;
			}
			toReplace.ConnectedPredecessor.ConnectedSuccessor = replaceBy;

			for (var i = 0; i<UpdateSuctionOrder.Count();i++)
			{
				if (UpdateSuctionOrder[i].Equals(toReplace))
					UpdateSuctionOrder[i] = replaceBy;
			}
		}

		public void Replace(PortFlowOut<TElement> toReplace, PortFlowOut<TElement> replaceBy)
		{
			if (toReplace.ConnectedSuccessor != null)
			{
				replaceBy.ConnectedSuccessor = toReplace.ConnectedSuccessor;
			}
			toReplace.ConnectedSuccessor.ConnectedPredecessor = replaceBy;

			for (var i = 0; i < UpdateElementOrder.Count(); i++)
			{
				if (UpdateElementOrder[i].Equals(toReplace))
					UpdateElementOrder[i] = replaceBy;
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
		
		private Func<TElement, TElement> FlowLambdaFunc { get; set; }
		
		public FlowInToOutSegment(Func<TElement, TElement> flowLambdaFunc)
		{
			FlowLambdaFunc = flowLambdaFunc;
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionToPredecessor = UpdateSuctionToPredecessor };
			Outgoing = new PortFlowOut<TElement>() {UpdateElementToSuccessor = UpdateElementToSuccessor };
		}

		public void UpdateSuctionToPredecessor()
		{
			Incoming.SuctionToPredecessor = Outgoing.SuctionFromSuccessor;
		}

		public void UpdateElementToSuccessor()
		{
			Outgoing.ElementToSuccessor = FlowLambdaFunc(Incoming.ElementFromPredecessor);
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

		public FlowSource(Func<TElement> sourceLambdaFunc)
		{
			SourceLambdaFunc = sourceLambdaFunc;
			Outgoing = new PortFlowOut<TElement>() {UpdateElementToSuccessor = UpdateElementToSuccessor };
		}
		
		public void UpdateElementToSuccessor()
		{
			Outgoing.ElementToSuccessor = SourceLambdaFunc();
		}
	}

	public class FlowSink<TElement> : Component, IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		public PortFlowIn<TElement> Incoming { get; set; }

		private Func<int> SinkLambdaFunc { get; }

		public FlowSink(Func<int> sinkLambdaFunc)
		{
			SinkLambdaFunc = sinkLambdaFunc;
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionToPredecessor = UpdateSuctionToPredecessor };
		}

		public FlowSink()
			: this(()=> 1 ) //default is a sink that has a suction of 1 (to let at least anything flow)
		{
		}
		
		public void UpdateSuctionToPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SuctionToPredecessor = SinkLambdaFunc();
		}
	}

	public class FlowComposite<TElement> : IFlowComponentUniqueOutgoing<TElement>,  IFlowComponentUniqueIncoming<TElement> where TElement : class
	{
		// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
		public PortFlowIn<TElement> Incoming { get; set; } //This element is accessed from the outside
		public PortFlowOut<TElement> Outgoing { get; set; } //This element is accessed from the outside

		public FlowSink<TElement> InternalSink { get; set; } // This element is accessed from the inside
		public FlowSource<TElement> InternalSource { get; set; } //This element is accessed from the inside
		
		public FlowComposite()
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionToPredecessor = UpdateSuctionToPredecessor };
			Outgoing = new PortFlowOut<TElement>() {UpdateElementToSuccessor = UpdateElementToSuccessor };
			InternalSink = new FlowSink<TElement>(() => Outgoing.SuctionFromSuccessor);
			InternalSource = new FlowSource<TElement>( () => Incoming.ElementFromPredecessor );
		}
		
		public void UpdateSuctionToPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.SuctionToPredecessor = InternalSource.Outgoing.SuctionFromSuccessor;
		}
		
		public void UpdateElementToSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.ElementToSuccessor = InternalSink.Incoming.ElementFromPredecessor;
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
			Incoming = new PortFlowIn<TElement>() {UpdateSuctionToPredecessor = UpdateSuctionOfPredecessor};
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
			Incoming.SuctionToPredecessor = MergeAny(suctionsOfCurrentCycle);
			Incoming.ConnectedPredecessor.SuctionFromSuccessor = Incoming.SuctionToPredecessor;
		}

		public void UpdateElementsOfSuccessors()
		{
			// TODO: Update with a dynamic Function
			SplitEqual(Incoming.ElementFromPredecessor, ElementsOfCurrentCycle);
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
			Outgoing = new PortFlowOut<TElement>() {UpdateElementToSuccessor = UpdateElementOfSuccessor};
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
			SplitEqual(Outgoing.SuctionFromSuccessor, SuctionsOfCurrentCycle);
		}

		public void UpdateElementOfSuccessor()
		{
			// TODO: Update with a dynamic Function
			var elementsOfCurrentCycle = VirtualIncomings.Select(virtualIncoming => virtualIncoming.Incoming.ElementFromPredecessor).ToArray();
			Outgoing.ElementToSuccessor = MergeAny(elementsOfCurrentCycle);
			Outgoing.ConnectedSuccessor.ElementFromPredecessor = Outgoing.ElementToSuccessor;
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
