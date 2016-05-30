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

using SafetySharp.CaseStudies.HemodialysisMachine.Utilities.BidirectionalFlow;
using SafetySharp.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.CircuitBasedPressureTank.Modeling
{
	public class Electron : IFlowElement<Electron>
	{
		[Hidden]
		public bool Available;

		public void CopyValuesFrom(Electron from)
		{
			Available = from.Available;
		}
	}

	public class PositiveCharge : IFlowElement<PositiveCharge>
	{
		[Hidden]
		public bool Available;

		public void CopyValuesFrom(PositiveCharge from)
		{
			Available = from.Available;
		}
	}
	public class CurrentInToOut : FlowInToOut<PositiveCharge, Electron>
	{
		public CurrentInToOut()
		{
		}

		public CurrentInToOut(Func<bool> isPowered)
		{
			Action<PositiveCharge, PositiveCharge> setPositiveCharge = (toSuccessor, fromPredecessor) =>
			{
				if (isPowered())
				{
					toSuccessor.Available = fromPredecessor.Available;
				}
				else
				{
					toSuccessor.Available = false;
				}
			};
			Action<Electron, Electron> setElectron = (fromSuccessor, toPredecessor) =>
			{
				if (isPowered())
				{
					toPredecessor.Available = fromSuccessor.Available;
				}
				else
				{
					toPredecessor.Available = false;
				}
			};
			UpdateForward = setPositiveCharge;
			UpdateBackward = setElectron;
		}

		public bool IsPowered()
		{
			return Incoming.Forward.Available && Outgoing.Backward.Available;
		}
	}

	public class CurrentSource : FlowSource<PositiveCharge, Electron>
	{
	}

	public class CurrentSink : FlowSink<PositiveCharge, Electron>
	{
	}


	public class CurrentSplitter : FlowSplitter<PositiveCharge, Electron>
	{
		public CurrentSplitter(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			//Standard behavior: Copy each value
			for (var i = 0; i < Number; i++)
			{
				Outgoings[i].Forward.CopyValuesFrom(Incoming.Forward);
			}
		}

		public override void UpdateBackwardInternal()
		{
			var target = Incoming.Backward;
			target.CopyValuesFrom(Outgoings[0].Backward);

			for (var i = 1; i < Outgoings.Length; i++) //start with second element
			{
				target.Available |= Outgoings[i].Backward.Available;
			}
		}
	}

	public class CurrentMerger : FlowMerger<PositiveCharge, Electron>
	{
		public CurrentMerger(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			var target = Outgoing.Forward;
			target.CopyValuesFrom(Incomings[0].Forward);

			for (var i = 1; i < Incomings.Length; i++) //start with second element
			{
				target.Available |= Incomings[i].Forward.Available;
			}
		}

		public override void UpdateBackwardInternal()
		{
			//Standard behavior: Copy each value
			for (var i = 0; i < Number; i++)
			{
				Incomings[i].Backward.CopyValuesFrom(Outgoing.Backward);
			}
		}
	}

	public class CurrentComposite : FlowComposite<PositiveCharge, Electron>
	{
	}

	public class CurrentDelegate : FlowDelegate<PositiveCharge, Electron>
	{
	}

	public class CurrentCombinator : FlowCombinator<PositiveCharge, Electron>
	{
		public override FlowMerger<PositiveCharge, Electron> CreateFlowVirtualMerger(int elementNos)
		{
			return new CurrentMerger(elementNos);
		}

		public override FlowSplitter<PositiveCharge, Electron> CreateFlowVirtualSplitter(int elementNos)
		{
			return new CurrentSplitter(elementNos);
		}
	}
}
