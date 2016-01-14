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
	using SafetySharp.Modeling;

	class Int : IElement<Int>
	{
		public int Value;

		public static implicit operator int(Int value)
		{
			return value.Value;
		}
		//  User-defined conversion from double to Digit
		public static implicit operator Int(int value)
		{
			return new Int { Value = value };
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

		public void CopyValuesFrom(Int @from)
		{
			this.Value = from.Value;
		}
		public Int()
		{
		}
	}

	interface IIntFlowComponent : IFlowComponent<Int,Int>
	{
	}

	class IntFlowInToOutSegment : FlowInToOutSegment<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowSource : FlowSource<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowSink : FlowSink<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowComposite : FlowComposite<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowCombinator : FlowCombinator<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowUniqueOutgoingStub : FlowUniqueOutgoingStub<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowUniqueIncomingStub : FlowUniqueIncomingStub<Int, Int>, IIntFlowComponent
	{
	}

	class IntFlowComponentCollection : Component
	{
		/// <summary>
		///   The vehicles contained in the collection.
		/// </summary>
		//[Hidden(HideElements = true)]
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