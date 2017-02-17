// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using SafetySharp.Modeling;

	internal struct Int
	{
		public readonly int Value;

		public Int(int value)
		{
			Value = value;
		}

		public static implicit operator int(Int value)
		{
			return value.Value;
		}
		//  User-defined conversion from double to Digit
		public static implicit operator Int(int value)
		{
			return new Int(value);
		}

		public override bool Equals(object other)
		{
			if (other is int)
			{
				return Value == (int)other;
			}
			if (other is Int)
			{
				var otherInt = (Int)other;
				return Value == otherInt.Value;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}

	interface IIntFlowComponent : IFlowComponent<Int,Int>
	{
	}

	class IntFlowInToOut : FlowInToOut<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowSource : FlowSource<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowSink : FlowSink<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowSplitter : FlowSplitter<Int, Int>, IIntFlowComponent
	{
		public IntFlowSplitter(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			//Standard behavior: Copy each value
			for (int i = 0; i < Number; i++)
			{
				Outgoings[i].Forward=Incoming.Forward;
			}
		}

		public override void UpdateBackwardInternal()
		{
			//Standard behavior: Select first source
			Incoming.Backward=Outgoings[0].Backward;
		}
	}
	

	class IntFlowMerger : FlowMerger<Int, Int>, IIntFlowComponent
	{
		public IntFlowMerger(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			//Standard behavior: Select first source
			Outgoing.Forward=Incomings[0].Forward;
		}

		public override void UpdateBackwardInternal()
		{
			//Standard behavior: Copy each value
			for (int i = 0; i < Number; i++)
			{
				Incomings[i].Backward=Outgoing.Backward;
			}
		}
	}

	class IntFlowCombinator : FlowCombinator<Int, Int>
	{
		public override FlowMerger<Int, Int> CreateFlowVirtualMerger(int elementNos)
		{
			return new IntFlowMerger(elementNos);
		}

		public override FlowSplitter<Int, Int> CreateFlowVirtualSplitter(int elementNos)
		{
			return new IntFlowSplitter(elementNos);
		}
	}


	class IntFlowComposite : FlowComposite<Int,Int>, IIntFlowComponent
	{
	}

	class IntFlowDelegate : FlowDelegate<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowComponentCollection : Component
	{
		/// <summary>
		///   The IIntFlowComponent contained in the collection.
		/// </summary>
		public readonly IIntFlowComponent[] Components;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public IntFlowComponentCollection(params IIntFlowComponent[] components)
		{
			Components = components;
		}
	}
}