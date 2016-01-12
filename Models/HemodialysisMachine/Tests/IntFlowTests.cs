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
			var source = new IntFlowSource(value => value.Value = 7);
			var direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();

			source.Outgoing.ElementToSuccessor = new Int();
			direct.Incoming.ElementFromPredecessor = new Int();
			direct.Outgoing.ElementToSuccessor = new Int();
			sink.Incoming.ElementFromPredecessor = new Int();

			combinator.Connect(source.Outgoing, direct.Incoming);
			combinator.Connect(direct.Outgoing, sink.Incoming);
			combinator.Update();
			source.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			direct.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			direct.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			sink.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
			direct.Incoming.SuctionToPredecessor.Should().Be(1);
			direct.Outgoing.SuctionFromSuccessor.Should().Be(1);
			sink.Incoming.SuctionToPredecessor.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value = 7);
			var direct1 = new IntFlowInToOutSegment(In => In);
			var direct2 = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, direct1.Incoming);
			combinator.Connect(direct1.Outgoing, direct2.Incoming);
			combinator.Connect(direct2.Outgoing, sink.Incoming);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value = 7);
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment(In => In);
			var secondInComposite = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, composite.Incoming);
			combinator.Connect(composite.InternalSource.Outgoing, firstInComposite.Incoming);
			combinator.Connect(firstInComposite.Outgoing, secondInComposite.Incoming);
			combinator.Connect(secondInComposite.Outgoing, composite.InternalSink.Incoming);
			combinator.Connect(composite.Outgoing, sink.Incoming);
			combinator.Update();

			source.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			composite.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			composite.InternalSource.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			firstInComposite.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			composite.InternalSink.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			composite.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			sink.Incoming.ElementFromPredecessor.Should().Be((Int)7);

			source.Outgoing.SuctionFromSuccessor.Should().Be(1);


		}

		[Test]
		public void SplitFlowWorks_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			combinator.Connect(source.Outgoing, new PortFlowIn<Int>[] { way1Direct.Incoming, way2Direct.Incoming });
			combinator.Connect(way1Direct.Outgoing, way1Sink.Incoming);
			combinator.Connect(way2Direct.Outgoing, way2Sink.Incoming);
			combinator.Update();
			way1Sink.Incoming.ElementFromPredecessor.Should().Be(7);
			way2Sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source1 = new IntFlowSource(value => value.Value = 7);
			var source2 = new IntFlowSource(value => value.Value = 7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(source1.Outgoing, way1Direct.Incoming);
			combinator.Connect(source2.Outgoing, way2Direct.Incoming);
			combinator.Connect(new PortFlowOut<Int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, sink.Incoming);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source1.Outgoing.SuctionFromSuccessor.Should().Be(1);
			source2.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			combinator.Connect(source.Outgoing, composite.Incoming);
			combinator.Connect(composite.InternalSource.Outgoing, new PortFlowIn<Int>[] { way1Direct.Incoming, way2Direct.Incoming });
			combinator.Connect(way1Direct.Outgoing, sinkInside.Incoming);
			combinator.Connect(way2Direct.Outgoing, composite.InternalSink.Incoming);
			combinator.Connect(composite.Outgoing, sinkOutside.Incoming);
			combinator.Update();
			sinkInside.Incoming.ElementFromPredecessor.Should().Be(7);
			sinkOutside.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var sourceOutside = new IntFlowSource(value => value.Value=7);
			var sourceInside = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.Connect(sourceOutside.Outgoing, composite.Incoming);
			combinator.Connect(composite.InternalSource.Outgoing, way1Direct.Incoming);
			combinator.Connect(sourceInside.Outgoing, way2Direct.Incoming);
			combinator.Connect(new PortFlowOut<Int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, composite.InternalSink.Incoming);
			combinator.Connect(composite.Outgoing, sink.Incoming);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceOutside.Outgoing.SuctionFromSuccessor.Should().Be(1);
			sourceInside.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}




		[Test]
		public void StubsCanBeReplaced_ExplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var stubOut = new IntFlowUniqueOutgoingStub();
			var stubIn = new IntFlowUniqueIncomingStub();
			var sink = new IntFlowSink();
			var direct = new IntFlowInToOutSegment(In => In);
			combinator.Connect(source.Outgoing, direct.Incoming);
			combinator.Connect(stubOut.Outgoing, stubIn.Incoming);

			combinator.Replace(stubIn.Incoming, sink.Incoming);
			combinator.Replace(stubOut.Outgoing, direct.Outgoing);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}





		// Implicit Ports



		[Test]
		public void SimpleFlowArrives_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, direct);
			combinator.ConnectOutWithIn(direct, sink);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var direct1 = new IntFlowInToOutSegment(In => In);
			var direct2 = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, direct1);
			combinator.ConnectOutWithIn(direct1, direct2);
			combinator.ConnectOutWithIn(direct2, sink);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment(In => In);
			var secondInComposite = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIn(composite, firstInComposite);
			combinator.ConnectOutWithIn(firstInComposite, secondInComposite);
			combinator.ConnectOutWithOut(secondInComposite, composite);
			combinator.ConnectOutWithIn(composite, sink);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorks_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			combinator.ConnectOutWithIns(source, new IFlowComponentUniqueIncoming<Int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, way1Sink);
			combinator.ConnectOutWithIn(way2Direct, way2Sink);
			combinator.Update();
			way1Sink.Incoming.ElementFromPredecessor.Should().Be(7);
			way2Sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source1 = new IntFlowSource(value => value.Value=7);
			var source2 = new IntFlowSource(value => value.Value=7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source1, way1Direct);
			combinator.ConnectOutWithIn(source2, way2Direct);
			combinator.ConnectOutsWithIn(new IFlowComponentUniqueOutgoing<Int>[] { way1Direct, way2Direct }, sink);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source1.Outgoing.SuctionFromSuccessor.Should().Be(1);
			source2.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIns(composite, new IFlowComponentUniqueIncoming<Int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, sinkInside);
			combinator.ConnectOutWithOut(way2Direct, composite);
			combinator.ConnectOutWithIn(composite, sinkOutside);
			combinator.Update();
			sinkInside.Incoming.ElementFromPredecessor.Should().Be(7);
			sinkOutside.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite_ImplicitPort()
		{
			var combinator = new IntFlowCombinator();
			var sourceOutside = new IntFlowSource(value => value.Value=7);
			var sourceInside = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(sourceOutside, composite);
			combinator.ConnectInWithIn(composite, way1Direct);
			combinator.ConnectOutWithIn(sourceInside, way2Direct);
			combinator.ConnectOutsWithOut(new IFlowComponentUniqueOutgoing<Int>[] { way1Direct, way2Direct }, composite);
			combinator.ConnectOutWithIn(composite, sink);
			combinator.Update();
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceOutside.Outgoing.SuctionFromSuccessor.Should().Be(1);
			sourceInside.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}
	}
}
