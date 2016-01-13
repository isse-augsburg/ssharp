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
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;

	class IntFlowModel : Component
	{
		[Root(Role.SystemOfInterest)]
		public readonly IntFlowCombinator Combinator = new IntFlowCombinator();

		[Root(Role.SystemOfInterest)]
		public IntFlowComponentCollection Components;


		[Provided]
		public void ForwardElement(Int outgoingElement, Int incomingElement)
		{
			outgoingElement.Value = incomingElement.Value;
		}

		[Provided]
		public void ForwardSuction(ref int outgoingSuction, int incomingSuction)
		{
			outgoingSuction = incomingSuction;
		}

		[Provided]
		public void CreateElement(Int outgoingElement)
		{
			outgoingElement.Value = 7;
		}

		[Provided]
		public void CreateSuction(ref int outgoingSuction)
		{
			outgoingSuction = 1;
		}
	}

	class IntFlowTests
	{
		[Test]
		public void SimpleFlowArrives_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source,direct,sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(source.Outgoing, direct.Incoming);
			testModel.Combinator.Connect(direct.Outgoing, sink.Incoming);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator) simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sourceAfterStep.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			directAfterStep.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			directAfterStep.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.SuctionToPredecessor.Should().Be(1);
			directAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
			directAfterStep.Incoming.SuctionToPredecessor.Should().Be(1);
			sourceAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}
		
		[Test]
		public void TwoStepFlowArrives_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct1 = new IntFlowInToOutSegment();
			var direct2 = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct1, direct2, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct1.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct1.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(direct2.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct2.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(source.Outgoing, direct1.Incoming);
			testModel.Combinator.Connect(direct1.Outgoing, direct2.Incoming);
			testModel.Combinator.Connect(direct2.Outgoing, sink.Incoming);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			sinkAfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment();
			var secondInComposite = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, composite, firstInComposite, secondInComposite, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(firstInComposite.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(firstInComposite.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(secondInComposite.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(secondInComposite.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(source.Outgoing, composite.Incoming);
			testModel.Combinator.Connect(composite.InternalSource.Outgoing, firstInComposite.Incoming);
			testModel.Combinator.Connect(firstInComposite.Outgoing, secondInComposite.Incoming);
			testModel.Combinator.Connect(secondInComposite.Outgoing, composite.InternalSink.Incoming);
			testModel.Combinator.Connect(composite.Outgoing, sink.Incoming);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var compositeAfterStep = (IntFlowComposite)flowComponentsAfterStep[1];
			var firstInCompositeAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[2];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sourceAfterStep.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			compositeAfterStep.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.InternalSource.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			firstInCompositeAfterStep.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.InternalSink.Incoming.ElementFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.Outgoing.ElementToSuccessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.ElementFromPredecessor.Should().Be((Int)7);

			sourceAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);


		}

		[Test]
		public void SplitFlowWorks_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, way1Direct, way2Direct, way1Sink, way2Sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(way1Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way1Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(way2Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way2Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(way1Sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			Component.Bind(nameof(way2Sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(source.Outgoing, new PortFlowIn<Int>[] { way1Direct.Incoming, way2Direct.Incoming });
			testModel.Combinator.Connect(way1Direct.Outgoing, way1Sink.Incoming);
			testModel.Combinator.Connect(way2Direct.Outgoing, way2Sink.Incoming);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sink1AfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			var sink2AfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sink1AfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			sink2AfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source1 = new IntFlowSource();
			var source2 = new IntFlowSource();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source1, source2, way1Direct, way2Direct, sink);
			Component.Bind(nameof(source1.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(source2.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(way1Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way1Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(way2Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way2Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(source1.Outgoing, way1Direct.Incoming);
			testModel.Combinator.Connect(source2.Outgoing, way2Direct.Incoming);
			testModel.Combinator.Connect(new PortFlowOut<Int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, sink.Incoming);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var source1AfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var source2AfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sinkAfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			source1AfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
			source2AfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, composite, way1Direct, way2Direct, sinkInside, sinkOutside);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(way1Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way1Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(way2Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way2Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sinkInside.SetOutgoingSuction), nameof(testModel.CreateSuction));
			Component.Bind(nameof(sinkOutside.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(source.Outgoing, composite.Incoming);
			testModel.Combinator.Connect(composite.InternalSource.Outgoing, new PortFlowIn<Int>[] { way1Direct.Incoming, way2Direct.Incoming });
			testModel.Combinator.Connect(way1Direct.Outgoing, sinkInside.Incoming);
			testModel.Combinator.Connect(way2Direct.Outgoing, composite.InternalSink.Incoming);
			testModel.Combinator.Connect(composite.Outgoing, sinkOutside.Incoming);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkInsideAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			var sinkOutsideAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkInsideAfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			sinkOutsideAfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var sourceOutside = new IntFlowSource();
			var sourceInside = new IntFlowSource();
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(sourceOutside, sourceInside, composite, way1Direct, way2Direct, sink);
			Component.Bind(nameof(sourceOutside.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(sourceInside.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(way1Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way1Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(way2Direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(way2Direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(sourceOutside.Outgoing, composite.Incoming);
			testModel.Combinator.Connect(composite.InternalSource.Outgoing, way1Direct.Incoming);
			testModel.Combinator.Connect(sourceInside.Outgoing, way2Direct.Incoming);
			testModel.Combinator.Connect(new PortFlowOut<Int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, composite.InternalSink.Incoming);
			testModel.Combinator.Connect(composite.Outgoing, sink.Incoming);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceInsideAfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sourceOutsideAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkAfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceOutsideAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
			sourceInsideAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}




		[Test]
		public void StubsCanBeReplaced_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var stubOut = new IntFlowUniqueOutgoingStub();
			var stubIn = new IntFlowUniqueIncomingStub();
			var sink = new IntFlowSink();
			var direct = new IntFlowInToOutSegment();
			testModel.Components = new IntFlowComponentCollection(source, stubOut, stubOut, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			testModel.Combinator.Connect(source.Outgoing, direct.Incoming);
			testModel.Combinator.Connect(stubOut.Outgoing, stubIn.Incoming);

			testModel.Combinator.Replace(stubIn.Incoming, sink.Incoming);
			testModel.Combinator.Replace(stubOut.Outgoing, direct.Outgoing);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sinkAfterStep.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}





		// Implicit Ports
		/*


		[Test]
		public void SimpleFlowArrives_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			combinator.ConnectOutWithIn(source, direct);
			combinator.ConnectOutWithIn(direct, sink);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var direct1 = new IntFlowInToOutSegment(In => In);
			var direct2 = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			combinator.ConnectOutWithIn(source, direct1);
			combinator.ConnectOutWithIn(direct1, direct2);
			combinator.ConnectOutWithIn(direct2, sink);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment(In => In);
			var secondInComposite = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIn(composite, firstInComposite);
			combinator.ConnectOutWithIn(firstInComposite, secondInComposite);
			combinator.ConnectOutWithOut(secondInComposite, composite);
			combinator.ConnectOutWithIn(composite, sink);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorks_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			combinator.ConnectOutWithIns(source, new IFlowComponentUniqueIncoming<Int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, way1Sink);
			combinator.ConnectOutWithIn(way2Direct, way2Sink);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			way1Sink.Incoming.ElementFromPredecessor.Should().Be(7);
			way2Sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var combinator = new IntFlowCombinator();
			var source1 = new IntFlowSource(value => value.Value=7);
			var source2 = new IntFlowSource(value => value.Value=7);
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			combinator.ConnectOutWithIn(source1, way1Direct);
			combinator.ConnectOutWithIn(source2, way2Direct);
			combinator.ConnectOutsWithIn(new IFlowComponentUniqueOutgoing<Int>[] { way1Direct, way2Direct }, sink);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			source1.Outgoing.SuctionFromSuccessor.Should().Be(1);
			source2.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var combinator = new IntFlowCombinator();
			var source = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			combinator.ConnectOutWithIn(source, composite);
			combinator.ConnectInWithIns(composite, new IFlowComponentUniqueIncoming<Int>[] { way1Direct, way2Direct });
			combinator.ConnectOutWithIn(way1Direct, sinkInside);
			combinator.ConnectOutWithOut(way2Direct, composite);
			combinator.ConnectOutWithIn(composite, sinkOutside);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sinkInside.Incoming.ElementFromPredecessor.Should().Be(7);
			sinkOutside.Incoming.ElementFromPredecessor.Should().Be(7);
			source.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var combinator = new IntFlowCombinator();
			var sourceOutside = new IntFlowSource(value => value.Value=7);
			var sourceInside = new IntFlowSource(value => value.Value=7);
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment(In => In);
			var way2Direct = new IntFlowInToOutSegment(In => In);
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingElement), nameof(testModel.CreateElement));
			Component.Bind(nameof(direct.SetOutgoingElement), nameof(testModel.ForwardElement));
			Component.Bind(nameof(direct.SetOutgoingSuction), nameof(testModel.ForwardSuction));
			Component.Bind(nameof(sink.SetOutgoingSuction), nameof(testModel.CreateSuction));
			combinator.ConnectOutWithIn(sourceOutside, composite);
			combinator.ConnectInWithIn(composite, way1Direct);
			combinator.ConnectOutWithIn(sourceInside, way2Direct);
			combinator.ConnectOutsWithOut(new IFlowComponentUniqueOutgoing<Int>[] { way1Direct, way2Direct }, composite);
			combinator.ConnectOutWithIn(composite, sink);

			var simulator = new Simulator(Model.Create(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sink.Incoming.ElementFromPredecessor.Should().Be(7);
			sourceOutside.Outgoing.SuctionFromSuccessor.Should().Be(1);
			sourceInside.Outgoing.SuctionFromSuccessor.Should().Be(1);
		}*/
	}
}
