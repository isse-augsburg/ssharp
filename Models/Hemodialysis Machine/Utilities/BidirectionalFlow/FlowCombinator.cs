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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Utilities.BidirectionalFlow
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using SafetySharp.Modeling;
	using QuickGraph;
	using QuickGraph.Algorithms;

	public abstract class FlowCombinator<TForward, TBackward> : Component
		where TForward : struct
		where TBackward : struct
	{
		[Hidden(HideElements = true)]
		private IFlowAtomic<TForward, TBackward>[] _updateForwardOrder;

		[NonSerializable]
		private readonly BidirectionalGraph<IFlowAtomic<TForward, TBackward>, Edge<IFlowAtomic<TForward, TBackward>>> _graphOfComponents = new BidirectionalGraph<IFlowAtomic<TForward, TBackward>, Edge<IFlowAtomic<TForward, TBackward>>>();

		public abstract FlowMerger<TForward, TBackward> CreateFlowVirtualMerger(int elementNos);
		public abstract FlowSplitter<TForward, TBackward> CreateFlowVirtualSplitter(int elementNos);
		
		public void CommitFlow()
		{
			_updateForwardOrder = _graphOfComponents.TopologicalSort().ToArray();
		}

		public override void Update()
		{
			if (_updateForwardOrder==null)
				throw new Exception("Flow was not committed. Please execute CommitFlow() in the model initialization phase");
			for (var i = _updateForwardOrder.Length - 1; i >= 0; i--)
			{
				_updateForwardOrder[i].UpdateBackwardInternal();
			}
			for (var i = 0; i < _updateForwardOrder.Length; i++)
			{
				_updateForwardOrder[i].UpdateForwardInternal();
			}
		}
		
		private void AddAtomicConnection(IFlowComponent<TForward, TBackward> from, IFlowComponent<TForward, TBackward> to)
		{
			var fromAtomic = from as IFlowAtomic<TForward, TBackward>;
			if (fromAtomic == null)
			{
				var fromComposite = from as IFlowComposite<TForward, TBackward>;
				if (fromComposite == null)
					throw new Exception("BUG: from should be either IFlowAtomic or IFlowComposite");
				fromAtomic = fromComposite.FlowOut;
			}
			var toAtomic = to as IFlowAtomic<TForward, TBackward>;
			if (toAtomic == null)
			{
				var toComposite = to as IFlowComposite<TForward, TBackward>;
				if (toComposite == null)
					throw new Exception("BUG: to should be either IFlowAtomic or IFlowComposite");
				toAtomic = toComposite.FlowIn;
			}

			_graphOfComponents.AddVerticesAndEdge(new Edge<IFlowAtomic<TForward, TBackward>>(fromAtomic, toAtomic));
		}

		public void ConnectOutWithIn(IFlowComponentUniqueOutgoing<TForward, TBackward> @from, IFlowComponentUniqueIncoming<TForward, TBackward> to)
		{
			var newPort = new FlowPort<TForward, TBackward>();
			from.Outgoing= newPort;
			to.Incoming = newPort;

			AddAtomicConnection(from, to);
		}

		public void ConnectOutWithIn(FlowSplitter<TForward, TBackward> @from, int index, IFlowComponentUniqueIncoming<TForward, TBackward> to)
		{
			Connect(from.Outgoings[index], to.Incoming);
			AddAtomicConnection(from, to);
		}

		public void ConnectOutWithIn(IFlowComponentUniqueOutgoing<TForward, TBackward> @from, FlowMerger<TForward, TBackward> to, int index)
		{
			Connect(from.Outgoing, to.Incomings[index]);
			AddAtomicConnection(from, to);
		}

		public FlowSplitter<TForward, TBackward> ConnectOutWithIns(IFlowComponentUniqueOutgoing<TForward, TBackward> @from, IFlowComponentUniqueIncoming<TForward, TBackward>[] tos)
		{
			// from --> Splitter --> to[]
			var elementNos = tos.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{
				var newPort = new FlowPort<TForward, TBackward>();
				from.Outgoing = newPort;
				tos[0].Incoming = newPort;
				AddAtomicConnection(from, tos[0]);
				return null;
			}
			else
			{
				// create virtual splitting component.
				var flowVirtualSplitter = CreateFlowVirtualSplitter(elementNos);

				var newPortToSplitter = new FlowPort<TForward, TBackward>();
				@from.Outgoing = newPortToSplitter;
				flowVirtualSplitter.Incoming = newPortToSplitter;
				AddAtomicConnection(from, flowVirtualSplitter);
				for (int i = 0; i < elementNos; i++)
				{
					var newPortFromSplitter = new FlowPort<TForward, TBackward>();
					flowVirtualSplitter.Outgoings[i] = newPortFromSplitter;
					tos[i].Incoming = newPortFromSplitter;
					AddAtomicConnection(flowVirtualSplitter, tos[i]);
				}
				return flowVirtualSplitter;
			}
		}

		public FlowMerger<TForward, TBackward> ConnectOutsWithIn(IFlowComponentUniqueOutgoing<TForward, TBackward>[] fromOuts, IFlowComponentUniqueIncoming<TForward, TBackward> to)
		{
			// fromOuts[] --> Merger --> to
			var elementNos = fromOuts.Length;
			if (elementNos == 0)
			{
				throw new ArgumentException("need at least one source element");
			}
			else if (elementNos == 1)
			{

				var newPort = new FlowPort<TForward, TBackward>();
				fromOuts[0].Outgoing = newPort;
				to.Incoming = newPort;
				AddAtomicConnection(fromOuts[0], to);
				return null;
			}
			else
			{
				// create virtual merging component.
				var flowVirtualMerger = CreateFlowVirtualMerger(elementNos);
				for (int i = 0; i < elementNos; i++)
				{

					var newPortToMerger = new FlowPort<TForward, TBackward>();
					fromOuts[i].Outgoing = newPortToMerger;
					flowVirtualMerger.Incomings[i] = newPortToMerger;
					AddAtomicConnection(fromOuts[i], flowVirtualMerger);
				}
				var newPortFromMerger = new FlowPort<TForward, TBackward>();
				flowVirtualMerger.Outgoing = newPortFromMerger;
				to.Incoming = newPortFromMerger;
				AddAtomicConnection(flowVirtualMerger, to);
				return flowVirtualMerger;
			}
		}

		/*
		public Tuple<FlowMerger<TForward, TBackward>,FlowSplitter<TForward, TBackward>>
				ConnectOutsWithIns(IFlowComponentUniqueOutgoing<TForward, TBackward>[] fromOuts,
																 IFlowComponentUniqueIncoming<TForward, TBackward> tos)
		{
			return null;
		}
		*/

		// For Composites (internal view)
		public void ConnectInWithIn(IFlowComposite<TForward, TBackward> @from, IFlowComponentUniqueIncoming<TForward, TBackward> to)
		{
			ConnectOutWithIn(from.FlowIn, to);
		}

		public void ConnectInWithIns(IFlowComposite<TForward, TBackward> @from, IFlowComponentUniqueIncoming<TForward, TBackward>[] tos)
		{
			ConnectOutWithIns(from.FlowIn, tos);
		}
		
		public void ConnectOutWithOut(IFlowComponentUniqueOutgoing<TForward, TBackward> @from, IFlowComposite<TForward, TBackward> to)
		{
			ConnectOutWithIn(from, to.FlowOut);
		}

		public void ConnectOutsWithOut(IFlowComponentUniqueOutgoing<TForward, TBackward>[] @froms, IFlowComposite<TForward, TBackward> to)
		{
			ConnectOutsWithIn(froms, to.FlowOut);
		}
	}
}
