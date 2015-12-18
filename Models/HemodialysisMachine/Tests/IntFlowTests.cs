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
			var direct = new IntFlowDirect();
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source,direct);
			combinator.ConnectOutWithIn(direct,sink);
			combinator.UpdateFlows();
			sink.ElementOfCurrentCycle.Should().Be(7);
			source.SuctionOfCurrentCycle.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var direct1 = new IntFlowDirect();
			var direct2 = new IntFlowDirect();
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, direct1);
			combinator.ConnectOutWithIn(direct1, direct2);
			combinator.ConnectOutWithIn(direct2, sink);
			combinator.UpdateFlows();
			sink.ElementOfCurrentCycle.Should().Be(7);
			source.SuctionOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowDirect();
			var secondInComposite = new IntFlowDirect();
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIn(composite, firstInComposite);
			combinator.ConnectOutWithIn(firstInComposite, secondInComposite);
			combinator.ConnectOutWithOut(secondInComposite, composite);
			combinator.ConnectOutWithIn(composite, sink);
			combinator.UpdateFlows();
			sink.ElementOfCurrentCycle.Should().Be(7);
			source.SuctionOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorks()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowDirect();
			var way2Direct = new IntFlowDirect();
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source,   new IFlowIn<int>[] {way1Direct,way2Direct});
			combinator.ConnectOutWithIn(way1Direct, way1Sink);
			combinator.ConnectOutWithIn(way2Direct, way2Sink);
			combinator.UpdateFlows();
			way1Sink.ElementOfCurrentCycle.Should().Be(7);
			way2Sink.ElementOfCurrentCycle.Should().Be(7);
			//source.SuctionOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks()
		{
			var combinator = new IntFlowCombinator();
			var source1 = new IntFlowSource(() => 7);
			var source2 = new IntFlowSource(() => 7);
			var way1Direct = new IntFlowDirect();
			var way2Direct = new IntFlowDirect();
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(source1, way1Direct);
			combinator.ConnectOutWithIn(source2, way2Direct);
			combinator.ConnectOutWithIn(new IFlowOut<int>[] { way1Direct, way2Direct },sink);
			combinator.UpdateFlows();
			sink.ElementOfCurrentCycle.Should().Be(7);
			source1.SuctionOfCurrentCycle.Should().Be(1);
			source2.SuctionOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite()
		{
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowDirect();
			var way2Direct = new IntFlowDirect();
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIn(composite, new IFlowIn<int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, sinkInside);
			combinator.ConnectOutWithOut(way2Direct, composite);
			combinator.ConnectOutWithIn(composite, sinkOutside);
			combinator.UpdateFlows();
			sinkInside.ElementOfCurrentCycle.Should().Be(7);
			sinkOutside.ElementOfCurrentCycle.Should().Be(7);
			source.SuctionOfCurrentCycle.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite()
		{
			var combinator = new IntFlowCombinator();
			var sourceOutside = new IntFlowSource(() => 7);
			var sourceInside = new IntFlowSource(() => 7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowDirect();
			var way2Direct = new IntFlowDirect();
			var sink = new IntFlowSink();
			combinator.ConnectOutWithIn(sourceOutside, composite);
			combinator.ConnectInWithIn(composite, way1Direct);
			combinator.ConnectOutWithIn(sourceInside, way2Direct);
			combinator.ConnectOutWithOut(new IFlowOut<int>[] { way1Direct, way2Direct }, composite);
			combinator.ConnectOutWithIn(composite, sink);
			combinator.UpdateFlows();
			sink.ElementOfCurrentCycle.Should().Be(7);
			sourceOutside.SuctionOfCurrentCycle.Should().Be(1);
			sourceInside.SuctionOfCurrentCycle.Should().Be(1);
		}
	}
}
