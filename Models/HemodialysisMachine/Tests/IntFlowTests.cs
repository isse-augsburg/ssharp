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
		public void SimpleFlowArrives_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, direct.Incoming);
			combinator.Connect(direct.Outgoing, sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives_ExplicitPort()
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

		[Test]
		public void CompositeFlowArrives_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment(In => In);
			var secondInComposite = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, composite.Incoming);
			combinator.Connect(composite.IncomingProxy, firstInComposite.Incoming);
			combinator.Connect(firstInComposite.Outgoing, secondInComposite.Incoming);
			combinator.Connect(secondInComposite.Outgoing, composite.OutgoingProxy);
			combinator.Connect(composite.Outgoing, sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorks_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, new PortFlowIn<int>[] { way1Direct.Incoming, way2Direct.Incoming });
			combinator.Connect(way1Direct.Outgoing, way1Sink.Incoming);
			combinator.Connect(way2Direct.Outgoing, way2Sink.Incoming);
			combinator.UpdateFlows();
			way1Sink.ElementInOfCurrentCycle.Should().Be(7);
			way2Sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source1 = new IntFlowSource(() => 7);
			var source2 = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source1.Outgoing, way1Direct.Incoming);
			combinator.Connect(source2.Outgoing, way2Direct.Incoming);
			combinator.Connect(new PortFlowOut<int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source1.SuctionInOfCurrentCycle.Should().Be(1);
			source2.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			combinator.Connect(source.Outgoing, composite.Incoming);
			combinator.Connect(composite.IncomingProxy, new PortFlowIn<int>[] { way1Direct.Incoming, way2Direct.Incoming });
			combinator.Connect(way1Direct.Outgoing, sinkInside.Incoming);
			combinator.Connect(way2Direct.Outgoing, composite.OutgoingProxy);
			combinator.Connect(composite.Outgoing, sinkOutside.Incoming);
			combinator.UpdateFlows();
			sinkInside.ElementInOfCurrentCycle.Should().Be(7);
			sinkOutside.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var sourceOutside = new IntFlowSource(() => 7);
			var sourceInside = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(sourceOutside.Outgoing, composite.Incoming);
			combinator.Connect(composite.IncomingProxy, way1Direct.Incoming);
			combinator.Connect(sourceInside.Outgoing, way2Direct.Incoming);
			combinator.Connect(new PortFlowOut<int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, composite.OutgoingProxy);
			combinator.Connect(composite.Outgoing, sink.Incoming);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			sourceOutside.SuctionInOfCurrentCycle.Should().Be(1);
			sourceInside.SuctionInOfCurrentCycle.Should().Be(1);
		}

		
		[Test]
		public void SimpleFlowArrives_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, direct);
			combinator.ConnectOutWithIn(direct, sink);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var direct1 = new IntFlowInToOutSegment(In => In);
			var direct2 = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, direct1);
			combinator.ConnectOutWithIn(direct1, direct2);
			combinator.ConnectOutWithIn(direct2, sink);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment(In => In);
			var secondInComposite = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIn(composite, firstInComposite);
			combinator.ConnectOutWithIn(firstInComposite, secondInComposite);
			combinator.ConnectOutWithOut(secondInComposite, composite);
			combinator.ConnectOutWithIn(composite, sink);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorks_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			combinator.ConnectOutWithIns(source, new IFlowComponentUniqueIncoming<int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, way1Sink);
			combinator.ConnectOutWithIn(way2Direct, way2Sink);
			combinator.UpdateFlows();
			way1Sink.ElementInOfCurrentCycle.Should().Be(7);
			way2Sink.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source1 = new IntFlowSource(() => 7);
			var source2 = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source1, way1Direct);
			combinator.ConnectOutWithIn(source2, way2Direct);
			combinator.ConnectOutsWithIn(new IFlowComponentUniqueOutgoing<int>[] { way1Direct, way2Direct }, sink);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			source1.SuctionInOfCurrentCycle.Should().Be(1);
			source2.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIns(composite, new IFlowComponentUniqueIncoming<int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, sinkInside);
			combinator.ConnectOutWithOut(way2Direct, composite);
			combinator.ConnectOutWithIn(composite, sinkOutside);
			combinator.UpdateFlows();
			sinkInside.ElementInOfCurrentCycle.Should().Be(7);
			sinkOutside.ElementInOfCurrentCycle.Should().Be(7);
			source.SuctionInOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite_ImplicitPort()
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
			combinator.ConnectOutsWithOut(new IFlowComponentUniqueOutgoing<int>[] { way1Direct, way2Direct }, composite);
			combinator.ConnectOutWithIn(composite, sink);
			combinator.UpdateFlows();
			sink.ElementInOfCurrentCycle.Should().Be(7);
			sourceOutside.SuctionInOfCurrentCycle.Should().Be(1);
			sourceInside.SuctionInOfCurrentCycle.Should().Be(1);
		}
	}
}
