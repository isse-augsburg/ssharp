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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Utilities.BidirectionalFlow
{
	using Modeling;
	using SafetySharp.Modeling;

	public abstract class FlowSplitter<TForward, TBackward> : IFlowAtomic<TForward, TBackward>,
		IFlowComponentUniqueIncoming<TForward, TBackward>
		where TForward : struct
		where TBackward : struct
	{
		protected int Number { get; }
		[Hidden]
		public FlowPort<TForward, TBackward> Incoming { get; set; }

		[Hidden(HideElements = true)]
		public FlowPort<TForward, TBackward>[] Outgoings { get; }
		
		protected FlowSplitter(int number)
		{
			Number = number;
			Outgoings = new FlowPort<TForward, TBackward>[number];
		}

		public virtual void UpdateForwardInternal()
		{
			//Standard behavior: Copy each value
			for (int i = 0; i < Number; i++)
			{
				Outgoings[i].Forward=Incoming.Forward;
			}
		}

		public virtual void UpdateBackwardInternal()
		{
			//Standard behavior: Select first source
			Incoming.Backward=Outgoings[0].Backward;
		}
	}
}
