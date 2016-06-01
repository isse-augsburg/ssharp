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

namespace SafetySharp.CaseStudies.CircuitBasedPressureTank.Utilities.BidirectionalFlow
{
	using System.Collections.Generic;
	using System.Linq;
	using Modeling;
	public class FlowInToOut<TForward, TBackward> : IFlowAtomic<TForward,TBackward>, IFlowComponentUniqueOutgoing<TForward, TBackward>, IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : class, IFlowElement<TForward>, new()
		where TBackward : class, IFlowElement<TBackward>, new()
	{
		public FlowPort<TForward, TBackward> Incoming { get; } = new FlowPort<TForward, TBackward>();
		public FlowPort<TForward, TBackward> Outgoing { get; } = new FlowPort<TForward, TBackward>();

		public Action<TBackward, TBackward> UpdateBackward = (fromSuccessor,toPredecessor) =>
		{
			// Standard behavior: Just copy. For a different behavior you have to overwrite this function
			toPredecessor.CopyValuesFrom(fromSuccessor);
		};
		
		public Action<TForward, TForward> UpdateForward = (toSuccessor,fromPredecessor) =>
		{
			// Standard behavior: Just copy. For a different behavior you have to overwrite this function
			toSuccessor.CopyValuesFrom(fromPredecessor);
		};

		public void UpdateForwardInternal()
		{
			UpdateForward(Outgoing.Forward, Incoming.Forward);
		}

		public void UpdateBackwardInternal()
		{
			UpdateBackward(Outgoing.Backward, Incoming.Backward);
		}
	}
}
