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

namespace HemodialysisMachine.Utilities.BidirectionalFlow
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

	public interface IElement<TElement>
		where TElement : class, IElement<TElement>, new()
	{
		void CopyValuesFrom(TElement @from);
	}

	
	public class PortFlowIn<TForward, TBackward> : Component
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		[Hidden]
		public PortFlowOut<TForward, TBackward> ConnectedPredecessor;
		
		public readonly TForward ForwardFromPredecessor;

		public readonly TBackward BackwardToPredecessor;

		[Required]
		public extern void UpdateBackwardToPredecessor(); // This is executed to calculate what the predecessor value should be (make changes). To update the predecessor, this.SetPredecessorSuction() must be called in this method.

		[Required]
		public extern void ForwardFromPredecessorWasUpdated();

		//public ResetCycleDelegate ResetValuesOfCurrentCycle;

		public PortFlowIn()
		{
			BackwardToPredecessor = new TBackward();
			ForwardFromPredecessor = new TForward();
		}
	}

	public class PortFlowOut<TForward, TBackward> : Component
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		[Hidden]
		public PortFlowIn<TForward, TBackward> ConnectedSuccessor;
		
		public readonly TForward ForwardToSuccessor;

		public readonly TBackward BackwardFromSuccessor;

		[Required]
		public extern void UpdateForwardToSuccessor(); // This is executed to calculate what the successor value should be (make changes). To update the successor, this.SetSuccessorElement() must be called in this method.
													   //public ResetCycleDelegate ResetValuesOfCurrentCycle;

		[Required]
		public extern void BackwardFromSuccessorWasUpdated();

		public PortFlowOut()
		{
			BackwardFromSuccessor = new TBackward();
			ForwardToSuccessor = new TForward();
		}
	}

	public interface IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		PortFlowIn<TForward, TBackward> Incoming { get; }
		//void AddSubFlows(FlowCombinator<TForward> _flowCombinator);
	}

	public interface IFlowComponentUniqueOutgoing<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		PortFlowOut<TForward, TBackward> Outgoing { get; }
	}

	public abstract class FlowCombinator<TForward, TBackward> : Component
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		[Hidden(HideElements = true)]
		private readonly List<PortFlowIn<TForward, TBackward>> UpdateBackwardOrder;

		[Hidden(HideElements = true)]
		private readonly List<PortFlowOut<TForward, TBackward>> UpdateForwardOrder;

		[Hidden(HideElements = true)]
		private readonly List<IComponent> VirtualFlowComponents;

		// TODO: For generic non-tree like acyclic flows a topological sort is necessary (So, every Update gets executed only once)

		public override void Update()
		{
			foreach (var flowIn in UpdateBackwardOrder)
			{
				flowIn.UpdateBackwardToPredecessor();
				flowIn.ConnectedPredecessor.BackwardFromSuccessor.CopyValuesFrom(flowIn.BackwardToPredecessor);
				flowIn.ConnectedPredecessor.BackwardFromSuccessorWasUpdated();
			}
			foreach (var flowOut in UpdateForwardOrder)
			{
				flowOut.UpdateForwardToSuccessor();
				flowOut.ConnectedSuccessor.ForwardFromPredecessor.CopyValuesFrom(flowOut.ForwardToSuccessor);
				flowOut.ConnectedSuccessor.ForwardFromPredecessorWasUpdated();
			}
		}

		protected FlowCombinator()
		{
			UpdateBackwardOrder=new List<PortFlowIn<TForward, TBackward>>();
			UpdateForwardOrder=new List<PortFlowOut<TForward, TBackward>>();
			VirtualFlowComponents=new List<IComponent>();
		}
		
		public void Connect(PortFlowOut<TForward, TBackward> @from, PortFlowIn<TForward, TBackward> to)
		{
			// Flow goes from [@from]-->[to]
			// Suction goes from [to]-->[@from]
			// When from.SetSuccessorElement is called, then to.SeTForward should be called.
			// When to.SetPredecessorSuction is called, then from.SetSuction should be called.
			if (from.ConnectedSuccessor != null || to.ConnectedPredecessor!=null)
				throw new Exception("is already connected");
			from.ConnectedSuccessor = to;
			to.ConnectedPredecessor = from;
			// Add elements to update lists			
			UpdateForwardOrder.Add(from); //from is the active part
			UpdateBackwardOrder.Insert(0, to); //to is the active part
		}

		public abstract FlowVirtualMerger<TForward, TBackward> CreateFlowVirtualMerger(int elementNos);

		// Convenience (split/merge)
		public void Connect(PortFlowOut<TForward, TBackward>[] fromOuts, PortFlowIn<TForward, TBackward> to)
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
				var flowVirtualMerger = CreateFlowVirtualMerger(elementNos);
				VirtualFlowComponents.Add(flowVirtualMerger);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(fromOuts[i], flowVirtualMerger.VirtualIncomings[i].Incoming);
				}
				Connect(flowVirtualMerger.Outgoing, to);
			}
		}

		public abstract FlowVirtualSplitter<TForward, TBackward> CreateFlowVirtualSplitter(int elementNos);

		public void Connect(PortFlowOut<TForward, TBackward> @from, params PortFlowIn<TForward, TBackward>[] to)
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
				var flowVirtualSplitter = CreateFlowVirtualSplitter(elementNos);
				VirtualFlowComponents.Add(flowVirtualSplitter);
				Connect(@from, flowVirtualSplitter.Incoming);
				for (int i = 0; i < elementNos; i++)
				{
					Connect(flowVirtualSplitter.VirtualOutgoings[i].Outgoing, to[i]);
				}
			}
		}

		public void Replace(PortFlowIn<TForward, TBackward> toReplace, PortFlowIn<TForward, TBackward> replaceBy)
		{
			if (toReplace.ConnectedPredecessor != null)
			{
				replaceBy.ConnectedPredecessor = toReplace.ConnectedPredecessor;
			}
			toReplace.ConnectedPredecessor.ConnectedSuccessor = replaceBy;

			for (var i = 0; i<UpdateBackwardOrder.Count();i++)
			{
				if (UpdateBackwardOrder[i].Equals(toReplace))
					UpdateBackwardOrder[i] = replaceBy;
			}
		}

		public void Replace(PortFlowOut<TForward, TBackward> toReplace, PortFlowOut<TForward, TBackward> replaceBy)
		{
			if (toReplace.ConnectedSuccessor != null)
			{
				replaceBy.ConnectedSuccessor = toReplace.ConnectedSuccessor;
			}
			toReplace.ConnectedSuccessor.ConnectedPredecessor = replaceBy;

			for (var i = 0; i < UpdateForwardOrder.Count(); i++)
			{
				if (UpdateForwardOrder[i].Equals(toReplace))
					UpdateForwardOrder[i] = replaceBy;
			}
		}


		// Convenience (connections on elements defined here)

		public void ConnectOutWithIn(IFlowComponentUniqueOutgoing<TForward, TBackward> @from, IFlowComponentUniqueIncoming<TForward, TBackward> to)
		{
			Connect(from.Outgoing, to.Incoming);
		}

		public void ConnectOutWithIns(IFlowComponentUniqueOutgoing<TForward, TBackward> @from, params IFlowComponentUniqueIncoming<TForward, TBackward>[] tos)
		{
			var collectedPorts = tos.Select(to => to.Incoming).ToArray();
			Connect(from.Outgoing, collectedPorts);
		}

		public void ConnectOutsWithIn(IFlowComponentUniqueOutgoing<TForward, TBackward>[] fromOuts, IFlowComponentUniqueIncoming<TForward, TBackward> to)
		{
			var collectedPorts = fromOuts.Select(from => from.Outgoing).ToArray();
			Connect(collectedPorts, to.Incoming);
		}

		public void ConnectInWithIn(FlowComposite<TForward, TBackward> @from, IFlowComponentUniqueIncoming<TForward, TBackward> to)
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			// Connect Source with Inner1
			Connect(@from.InternalSource.Outgoing, to.Incoming);
		}

		public void ConnectOutWithOut(IFlowComponentUniqueOutgoing<TForward, TBackward> @from, FlowComposite<TForward, TBackward> to)
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			// Connect Inner1 with Sink
			Connect(@from.Outgoing, to.InternalSink.Incoming);
		}
		
		public void ConnectInWithIns(FlowComposite<TForward, TBackward> @from, params IFlowComponentUniqueIncoming<TForward, TBackward>[] tos)
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			var collectedPorts = tos.Select(to => to.Incoming).ToArray();
			Connect(from.InternalSource.Outgoing, collectedPorts);
		}

		public void ConnectOutsWithOut(IFlowComponentUniqueOutgoing<TForward, TBackward>[] fromOuts, FlowComposite<TForward, TBackward> to)
		{
			// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
			var collectedPorts = fromOuts.Select(from => from.Outgoing).ToArray();
			Connect(collectedPorts, to.InternalSink.Incoming);
		}

		// Standard Behaviors
		[Provided]
		private void StandardBehaviorIgnoreReceivedBackward(TBackward incomingBackward)
		{
		}
		[Provided]
		private void StandardBehaviorIgnoreSetOutgoingBackward(TBackward outgoingBackward)
		{
		}
		[Provided]
		private void StandardBehaviorIgnoreSetOutgoingBackward(TBackward outgoingBackward, TBackward incomingBackward)
		{
			outgoingBackward.CopyValuesFrom(incomingBackward);
		}

		[Provided]
		private void StandardBehaviorIgnoreReceivedForward(TForward incomingForward)
		{
		}
		[Provided]
		private void StandardBehaviorIgnoreSetOutgoingForward(TForward outgoingForward)
		{
		}
		[Provided]
		private void StandardBehaviorIgnoreSetOutgoingForward(TForward outgoingForward, TForward incomingForward)
		{
			outgoingForward.CopyValuesFrom(incomingForward);
		}

		public void SetStandardBehaviorForElement(params IFlowComponent<TForward, TBackward>[] components)
		{
			foreach (var component in components)
			{
				if (component is FlowSource<TForward, TBackward>)
				{
					var sourceComponent = (FlowSource<TForward, TBackward>)component;
					Bind(nameof(sourceComponent.SetOutgoingForward), nameof(StandardBehaviorIgnoreSetOutgoingForward));
				}
				if (component is FlowInToOutSegment<TForward, TBackward>)
				{
					var directComponent = (FlowInToOutSegment<TForward, TBackward>)component;
					Bind(nameof(directComponent.SetOutgoingForward), nameof(StandardBehaviorIgnoreSetOutgoingForward));
				}
				if (component is FlowSink<TForward, TBackward>)
				{
					var sinkComponent = (FlowSink<TForward, TBackward>)component;
					Bind(nameof(sinkComponent.ForwardFromPredecessorWasUpdated), nameof(StandardBehaviorIgnoreReceivedForward));
				}
			}
		}

		public void SetStandardBehaviorForBackward(params IFlowComponent<TForward, TBackward>[] components)
		{
			foreach (var component in components)
			{
				if (component is FlowSource<TForward, TBackward>)
				{
					var sourceComponent = (FlowSource<TForward, TBackward>)component;
					Bind(nameof(sourceComponent.BackwardFromSuccessorWasUpdated), nameof(StandardBehaviorIgnoreReceivedBackward));
				}
				if (component is FlowInToOutSegment<TForward, TBackward>)
				{
					var directComponent = (FlowInToOutSegment<TForward, TBackward>)component;
					Bind(nameof(directComponent.SetOutgoingBackward), nameof(StandardBehaviorIgnoreSetOutgoingBackward));
				}
				if (component is FlowSink<TForward, TBackward>)
				{
					var sinkComponent = (FlowSink<TForward, TBackward>)component;
					Bind(nameof(sinkComponent.SetOutgoingBackward), nameof(StandardBehaviorIgnoreSetOutgoingBackward));
				}
			}
		}

		public void SetStandardBehavior(params IFlowComponent<TForward, TBackward>[] components)
		{
			SetStandardBehaviorForElement(components);
			SetStandardBehaviorForBackward(components);
		}
	}

	public interface IFlowComponent<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
	}

	public class FlowInToOutSegment<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueOutgoing<TForward, TBackward>, IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		public PortFlowIn<TForward, TBackward> Incoming { get; } = new PortFlowIn<TForward, TBackward>();
		public PortFlowOut<TForward, TBackward> Outgoing { get; } = new PortFlowOut<TForward, TBackward>();

		public FlowInToOutSegment()
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Incoming.UpdateBackwardToPredecessor), nameof(UpdateBackwardToPredecessor));
			Bind(nameof(Outgoing.UpdateForwardToSuccessor), nameof(UpdateForwardToSuccessor));
			Bind(nameof(Incoming.ForwardFromPredecessorWasUpdated), nameof(ForwardFromPredecessorWasUpdated));
			Bind(nameof(Outgoing.BackwardFromSuccessorWasUpdated), nameof(BackwardFromSuccessorWasUpdated));
		}

		[Required]
		public extern void SetOutgoingBackward(TBackward outgoingBackward, TBackward incomingBackward);

		[Required]
		public extern void SetOutgoingForward(TForward outgoingForward,TForward incomingForward);

		public void UpdateBackwardToPredecessor()
		{
			SetOutgoingBackward(Incoming.BackwardToPredecessor, Outgoing.BackwardFromSuccessor);
		}

		public void UpdateForwardToSuccessor()
		{
			SetOutgoingForward(Outgoing.ForwardToSuccessor,Incoming.ForwardFromPredecessor);
		}

		public void BackwardFromSuccessorWasUpdated()
		{
		}

		public void ForwardFromPredecessorWasUpdated()
		{
		}
	}

	// TODO: Idea Short Circuit Components:
	//    When flowConnector.Connect(stub.Outgoing,normalA.Incoming)
	//     and flowConnector.Connect(normalB.Outgoing,stub.Incoming)
	//    then flowConnector.Connect(normalB.Outgoing,normalA.Incoming)

	public class FlowSource<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueOutgoing<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		public PortFlowOut<TForward, TBackward> Outgoing { get; } = new PortFlowOut<TForward, TBackward>();

		public FlowSource()
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Outgoing.UpdateForwardToSuccessor), nameof(UpdateForwardToSuccessor));
			Bind(nameof(Outgoing.BackwardFromSuccessorWasUpdated), nameof(BackwardFromSuccessorWasUpdatedIntern));
		}

		[Required]
		public extern void SetOutgoingForward(TForward outgoingForward);

		public void UpdateForwardToSuccessor()
		{
			SetOutgoingForward(Outgoing.ForwardToSuccessor);
		}

		[Required]
		public extern void BackwardFromSuccessorWasUpdated(TBackward incomingBackward);

		private void BackwardFromSuccessorWasUpdatedIntern()
		{
			BackwardFromSuccessorWasUpdated(Outgoing.BackwardFromSuccessor);
		}
	}

	public class FlowSink<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		public PortFlowIn<TForward, TBackward> Incoming { get; set; }
		
		public FlowSink()
		{
			Incoming = new PortFlowIn<TForward, TBackward>();
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Incoming.UpdateBackwardToPredecessor), nameof(UpdateBackwardToPredecessor));
			Bind(nameof(Incoming.ForwardFromPredecessorWasUpdated), nameof(ForwardFromPredecessorWasUpdatedIntern));
		}

		[Required]
		public extern void SetOutgoingBackward(TBackward outgoingBackward);

		public void UpdateBackwardToPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			SetOutgoingBackward(Incoming.BackwardToPredecessor);
		}

		public extern void ForwardFromPredecessorWasUpdated(TForward incomingForward);

		private void ForwardFromPredecessorWasUpdatedIntern()
		{
			ForwardFromPredecessorWasUpdated(Incoming.ForwardFromPredecessor);
		}
	}

	public class FlowComposite<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueOutgoing<TForward, TBackward>, IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		// Outer1 --> Incoming --> Source --> Inner1 --> Sink --> Outgoing --> Outer2
		public PortFlowIn<TForward, TBackward> Incoming { get; } = new PortFlowIn<TForward, TBackward>(); //This element is accessed from the outside
		public PortFlowOut<TForward, TBackward> Outgoing { get; } = new PortFlowOut<TForward, TBackward>(); //This element is accessed from the outside

		public FlowSink<TForward, TBackward> InternalSink { get; } = new FlowSink<TForward, TBackward>(); // This element is accessed from the inside
		public FlowSource<TForward, TBackward> InternalSource { get; } = new FlowSource<TForward, TBackward>(); //This element is accessed from the inside

		public FlowComposite()
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Incoming.UpdateBackwardToPredecessor), nameof(UpdateSuctionToPredecessor));
			Bind(nameof(Outgoing.UpdateForwardToSuccessor), nameof(UpdateForwardToSuccessor));
			Bind(nameof(InternalSink.SetOutgoingBackward), nameof(SetOutgoingBackwardOfInternalSink));
			Bind(nameof(InternalSource.SetOutgoingForward), nameof(SetOutgoingForwardOfInternalSource));
			Bind(nameof(Incoming.ForwardFromPredecessorWasUpdated), nameof(ForwardFromPredecessorWasUpdated));
			Bind(nameof(Outgoing.BackwardFromSuccessorWasUpdated), nameof(BackwardFromSuccessorWasUpdated));
			Bind(nameof(InternalSource.BackwardFromSuccessorWasUpdated), nameof(BackwardFromSuccessorWasUpdatedInternal));
			Bind(nameof(InternalSink.ForwardFromPredecessorWasUpdated), nameof(ForwardFromPredecessorWasUpdatedInternal));
		}

		[Provided]
		public void SetOutgoingBackwardOfInternalSink(TBackward outgoingBackward)
		{
			outgoingBackward.CopyValuesFrom(Outgoing.BackwardFromSuccessor);
		}

		[Provided]
		public void SetOutgoingForwardOfInternalSource(TForward outgoingForward)
		{
			outgoingForward.CopyValuesFrom(Incoming.ForwardFromPredecessor);
		}

		public void UpdateSuctionToPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			Incoming.BackwardToPredecessor.CopyValuesFrom(InternalSource.Outgoing.BackwardFromSuccessor);
		}
		
		public void UpdateForwardToSuccessor()
		{
			// Actively Update the ElementOfCurrentCycle of the successor
			Outgoing.ForwardToSuccessor.CopyValuesFrom(InternalSink.Incoming.ForwardFromPredecessor);
		}

		public void BackwardFromSuccessorWasUpdated()
		{
		}

		public void ForwardFromPredecessorWasUpdated()
		{
		}

		[Provided]
		private void ForwardFromPredecessorWasUpdatedInternal(TForward incomingForward)
		{
		}

		[Provided]
		private void BackwardFromSuccessorWasUpdatedInternal(TBackward incomingBackward)
		{
		}
	}

	public class FlowVirtualSource<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueOutgoing<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		// Used only by FlowVirtualSplitter
		private int Index;
		public PortFlowOut<TForward, TBackward> Outgoing { get; } = new PortFlowOut<TForward, TBackward>();

		public FlowVirtualSource(int index)
		{
			Index = index;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Outgoing.UpdateForwardToSuccessor), nameof(UpdateForwardToSuccessor));
			Bind(nameof(Outgoing.BackwardFromSuccessorWasUpdated), nameof(BackwardFromSuccessorWasUpdated));
		}

		[Required]
		public extern void SetOutgoingForward(int index, TForward outgoingForward);

		public void UpdateForwardToSuccessor()
		{
			SetOutgoingForward(Index,Outgoing.ForwardToSuccessor);
		}

		public void BackwardFromSuccessorWasUpdated()
		{
		}
	}

	public abstract class FlowVirtualSplitter<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		private int Number { get; }
		public PortFlowIn<TForward, TBackward> Incoming { get; }

		public FlowVirtualSource<TForward, TBackward>[] VirtualOutgoings { get; }

		// Elements must be split
		public TForward[] ForwardsToSuccessors { get; }
		public TBackward[] BackwardsFromSuccessors { get; }

		public FlowVirtualSplitter(int number)
		{
			Number = number;
			Incoming = new PortFlowIn<TForward, TBackward>();
			VirtualOutgoings = new FlowVirtualSource<TForward, TBackward>[number];
			for (int i = 0; i < number; i++)
			{
				VirtualOutgoings[i] = new FlowVirtualSource<TForward, TBackward>(i);
			}
			ForwardsToSuccessors = VirtualOutgoings.Select(virtualOutgoing => virtualOutgoing.Outgoing.ForwardToSuccessor).ToArray(); //directly link to the elements in the ports
			BackwardsFromSuccessors = VirtualOutgoings.Select(virtualOutgoing => virtualOutgoing.Outgoing.BackwardFromSuccessor).ToArray(); //directly link to the elements in the ports
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Incoming.UpdateBackwardToPredecessor), nameof(UpdateBackwardToPredecessor));
			for (int i = 0; i < Number; i++)
			{
				var outgoing = VirtualOutgoings[i];
				Bind(nameof(outgoing.SetOutgoingForward), nameof(SetOutgoingForwardOfInternalSource));
			}
			Bind(nameof(Incoming.ForwardFromPredecessorWasUpdated), nameof(ElementFromPredecessorWasUpdated));
		}

		[Provided]
		public void SetOutgoingForwardOfInternalSource(int index, TForward outgoingForward)
		{
			UpdateForwardsToSuccessors(); // TODO: Execute only once per cycle
			outgoingForward.CopyValuesFrom(ForwardsToSuccessors[index]);
		}
		
		public abstract void SplitForwards(TForward source, TForward[] targets, TBackward[] dependingOn);
		
		public abstract void MergeBackwards(TBackward[] sources, TBackward target);
		

		public void StandardBehaviorSplitForwardsEqual(TForward source, TForward[] targets, TBackward[] dependingOn)
		{
			var number = targets.Length;
			for (int i = 0; i < number; i++)
			{
				targets[i].CopyValuesFrom(source);
			}
		}

		public void StandardBehaviorMergeBackwardsSelectFirst(TBackward[] sources, TBackward target)
		{
			target.CopyValuesFrom(sources[0]);
		}

		public void UpdateBackwardToPredecessor()
		{
			// TODO: Update with a dynamic Function
			MergeBackwards(BackwardsFromSuccessors, Incoming.BackwardToPredecessor);
		}

		public void UpdateForwardsToSuccessors()
		{
			// TODO: Update with a dynamic Function
			SplitForwards(Incoming.ForwardFromPredecessor, ForwardsToSuccessors, BackwardsFromSuccessors);
		}

		public void ElementFromPredecessorWasUpdated()
		{
		}
	}
	

	public class FlowVirtualSink<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		private int Index;
		// Used only by FlowVirtualMerger
		public PortFlowIn<TForward, TBackward> Incoming { get; }

		public FlowVirtualSink(int index)
		{
			Index = index;
			Incoming = new PortFlowIn<TForward, TBackward>();
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Incoming.UpdateBackwardToPredecessor), nameof(UpdateBackwardToPredecessor));
			Bind(nameof(Incoming.ForwardFromPredecessorWasUpdated), nameof(ForwardFromPredecessorWasUpdated));
		}

		[Required]
		public extern void SetOutgoingBackward(int index, TBackward outgoingBackward);

		public void UpdateBackwardToPredecessor()
		{
			// Actively Update the SuctionOfCurrentCycle of the predecessor
			SetOutgoingBackward(Index,Incoming.BackwardToPredecessor);
		}

		public void ForwardFromPredecessorWasUpdated()
		{
		}
	}
	
	public abstract class FlowVirtualMerger<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueOutgoing<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		private int Number { get; }
		public FlowVirtualSink<TForward, TBackward>[] VirtualIncomings { get; }
		public PortFlowOut<TForward, TBackward> Outgoing { get; }

		// Suctions must be split
		public TBackward[] BackwardsToPredecessors { get; }
		public TForward[] ForwardsFromPredecessors { get; }

		public FlowVirtualMerger(int number)
		{
			Number = number;
			VirtualIncomings = new FlowVirtualSink<TForward, TBackward>[number];
			Outgoing = new PortFlowOut<TForward, TBackward>();
			for (int i = 0; i < number; i++)
			{
				VirtualIncomings[i] = new FlowVirtualSink<TForward, TBackward>(i);
			}
			BackwardsToPredecessors = VirtualIncomings.Select(virtualIncoming => virtualIncoming.Incoming.BackwardToPredecessor).ToArray(); //directly link to the elements in the ports
			ForwardsFromPredecessors = VirtualIncomings.Select(virtualIncoming => virtualIncoming.Incoming.ForwardFromPredecessor).ToArray(); //directly link to the elements in the ports
		}

		protected override void CreateBindings()
		{
			Bind(nameof(Outgoing.UpdateForwardToSuccessor), nameof(UpdateForwardToSuccessor));
			for (int i = 0; i < Number; i++)
			{
				var incoming = VirtualIncomings[i];
				Bind(nameof(incoming.SetOutgoingBackward), nameof(SetOutgoingBackwardOfInternalSink));
			}
			Bind(nameof(Outgoing.BackwardFromSuccessorWasUpdated), nameof(BackwardsFromSuccessorWasUpdated));
		}

		[Provided]
		public void SetOutgoingBackwardOfInternalSink(int index, TBackward outgoingBackward)
		{
			UpdateBackwardsToPredecessors(); // TODO: Execute only once per cycle
			outgoingBackward.CopyValuesFrom(BackwardsToPredecessors[index]);
		}
		
		public abstract void SplitBackwards(TBackward source, TBackward[] targets);
		
		public abstract void MergeForwards(TForward[] sources, TForward target, TBackward dependingOn);
		
		public void StandardBehaviorSplitBackwardsEqual(TBackward source, TBackward[] targets)
		{
			var number = targets.Length;
			for (int i = 0; i < number; i++)
			{
				targets[i].CopyValuesFrom(source);
			}
		}

		public void StandardBehaviorMergeForwardsSelectFirst(TForward[] sources, TForward target, TBackward dependingOn)
		{
			target.CopyValuesFrom(sources[0]);
		}


		public void UpdateBackwardsToPredecessors()
		{
			// TODO: Update with a dynamic Function
			SplitBackwards(Outgoing.BackwardFromSuccessor, BackwardsToPredecessors);
		}

		public void UpdateForwardToSuccessor()
		{
			// TODO: Update with a dynamic Function
			MergeForwards(ForwardsFromPredecessors, Outgoing.ForwardToSuccessor, Outgoing.BackwardFromSuccessor);
		}

		public void BackwardsFromSuccessorWasUpdated()
		{
		}
	}

	public class FlowUniqueOutgoingStub<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueOutgoing<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		//   FlowUniqueOutgoingStub:
		//    When flowConnector.Connect(stub.Outgoing,normalA.Incoming)
		//     and flowConnector.Replace(stub.Outgoing,normalB.Outgoing)
		//    then flowConnector.Connect(normalB.Outgoing,normalA.Incoming)
		public PortFlowOut<TForward, TBackward> Outgoing { get; }
		
		public FlowUniqueOutgoingStub()
		{
			Outgoing = new PortFlowOut<TForward, TBackward>();
		}
	}

	public class FlowUniqueIncomingStub<TForward, TBackward> : Component, IFlowComponent<TForward, TBackward>, IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IElement<TForward>, new()
		where TBackward : class, IElement<TBackward>, new()
	{
		//   FlowUniqueIncomingStub:
		//    When flowConnector.Connect(normalA.Outgoing,stub.Incoming)
		//     and flowConnector.Replace(stub.Incoming,normalB.Incoming)
		//    then flowConnector.Connect(normalA.Outgoing,normalB.Incoming)
		public PortFlowIn<TForward, TBackward> Incoming { get; }
		
		public FlowUniqueIncomingStub()
		{
			Incoming = new PortFlowIn<TForward, TBackward>();
		}
	}
}
