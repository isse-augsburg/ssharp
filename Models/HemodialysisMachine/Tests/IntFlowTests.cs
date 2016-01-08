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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FluentAssertions;

namespace HemodialysisMachine.Tests
{
	using Utilities;

	class IntFlowTests
	{
		[Test]
		public void SimpleFlowArrives()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var direct = new IntFlowInToOutSegment(In=>In);
			var sink = new IntFlowSink();
			combinator.Connect(source.Outgoing,direct.Incoming);
			combinator.Connect(direct.Outgoing,sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var direct1 = new IntFlowInToOutSegment(In => In);
			var direct2 = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, direct1.Incoming);
			combinator.Connect(direct1.Outgoing, direct2.Incoming);
			combinator.Connect(direct2.Outgoing, sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		/*
		[Test]
		public void CompositeFlowArrives()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment(In => In);
			var secondInComposite = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, composite);
			combinator.ConnectInWithIn(composite, firstInComposite);
			combinator.ConnectOutWithIn(firstInComposite, secondInComposite);
			combinator.ConnectOutWithOut(secondInComposite, composite);
			combinator.Connect(composite.Outgoing, sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}
		*/

		[Test]
		public void SplitFlowWorks()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source.Outgoing, new PortFlowIn<int>[] {way1Direct.Incoming, way2Direct.Incoming});
			combinator.Connect(way1Direct.Outgoing, way1Sink.Incoming);
			combinator.Connect(way2Direct.Outgoing, way2Sink.Incoming);
			combinator.UpdateFlows();
			way1Sink.ElementInOfCurrentCycle.Should().Be(7);
			way2Sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks()
		{
			var combinator = new IntFlowCombinator();
			var source1 = new IntFlowSource(() => 7);
			var source2 = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source1.Outgoing, way1Direct.Incoming);
			combinator.Connect(source2.Outgoing, way2Direct.Incoming);
			combinator.ConnectOutWithIn(new PortFlowOut<int>[] { way1Direct.Outgoing, way2Direct.Outgoing },sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source1.SuctionInOfCurrentCycle.Should().Be(1);
			source2.SuctionInOfCurrentCycle.Should().Be(1);
		}

		/*
		[Test]
		public void SplitFlowWorksWithComposite()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			combinator.ConnectOutWithIn(source.Outgoing, composite);
			combinator.ConnectInWithIn(composite, new PortFlowIn<int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, sinkInside);
			combinator.ConnectOutWithOut(way2Direct, composite);
			combinator.ConnectOutWithIn(composite, sinkOutside);
			combinator.UpdateFlows();
			sinkInside.ElementInOfCurrentCycle.Should().Be(7);
			sinkOutside.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite()
		{
			var combinator = new IntFlowCombinator();
			var sourceOutside = new IntFlowSource(() => 7);
			var sourceInside = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(sourceOutside, composite);
			combinator.ConnectInWithIn(composite, way1Direct);
			combinator.ConnectOutWithIn(sourceInside, way2Direct);
			combinator.ConnectOutWithOut(new PortFlowOut<int>[] { way1Direct, way2Direct }, composite);
			combinator.ConnectOutWithIn(composite, sink);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			sourceOutside.SuctionInOfCurrentCycle.Should().Be(1);
			sourceInside.SuctionInOfCurrentCycle.Should().Be(1);
		}
		*/
	}
}
