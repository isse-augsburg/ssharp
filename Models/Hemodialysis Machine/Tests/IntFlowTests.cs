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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Tests
{
	using Analysis;
	using Modeling;
	using Runtime;
	using Utilities;
	using Utilities.BidirectionalFlow;

	class IntFlowModel : Component
	{
		[Root(Role.SystemOfInterest)]
		public readonly IntFlowCombinator Combinator = new IntFlowCombinator();

		[Root(Role.SystemOfInterest)]
		public IntFlowComponentCollection Components;


		[Provided]
		public void PassForward(Int outgoingForward, Int incomingForward)
		{
			outgoingForward.Value = incomingForward.Value;
		}

		[Provided]
		public void PassBackward(Int outgoingBackward, Int incomingBackward)
		{
			outgoingBackward.Value = incomingBackward.Value;
		}

		[Provided]
		public void CreateForward(Int outgoingForward)
		{
			outgoingForward.Value = 7;
		}

		[Provided]
		public void CreateBackward(Int outgoingBackward)
		{
			outgoingBackward.Value = 1;
		}

		[Provided]
		public void PrintReceivedForward(Int incomingForward)
		{
			System.Console.WriteLine("Received Forward Int: "+ incomingForward.Value);
		}

		[Provided]
		public void PrintReceivedBackward(Int incomingBackward)
		{
			System.Console.WriteLine("Received Backward Int: " + incomingBackward.Value);
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
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(source.Outgoing, direct.Incoming);
			testModel.Combinator.Connect(direct.Outgoing, sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator) simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sourceAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			directAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			directAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.BackwardToPredecessor.Should().Be(1);
			directAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
			directAfterStep.Incoming.BackwardToPredecessor.Should().Be(1);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SimpleFlowArrivesWithStandardBehavior_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			testModel.Combinator.SetStandardBehavior(source, direct, sink);
			testModel.Combinator.Connect(source.Outgoing, direct.Incoming);
			testModel.Combinator.Connect(direct.Outgoing, sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sourceAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)0);
			directAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)0);
			directAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)0);
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)0);
			sinkAfterStep.Incoming.BackwardToPredecessor.Should().Be(0);
			directAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(0);
			directAfterStep.Incoming.BackwardToPredecessor.Should().Be(0);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(0);
		}

		[Test]
		public void SimpleFlowArrivesWithStandardSuction_ExplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			testModel.Combinator.SetStandardBehaviorForBackward(source,direct,sink);
			testModel.Combinator.Connect(source.Outgoing, direct.Incoming);
			testModel.Combinator.Connect(direct.Outgoing, sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sourceAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			directAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			directAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.BackwardToPredecessor.Should().Be(0);
			directAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(0);
			directAfterStep.Incoming.BackwardToPredecessor.Should().Be(0);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(0);
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
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(direct1.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(direct1.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(direct2.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(direct2.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(source.Outgoing, direct1.Incoming);
			testModel.Combinator.Connect(direct1.Outgoing, direct2.Incoming);
			testModel.Combinator.Connect(direct2.Outgoing, sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
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
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(firstInComposite.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(firstInComposite.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(secondInComposite.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(secondInComposite.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(source.Outgoing, composite.Incoming);
			testModel.Combinator.Connect(composite.InternalSource.Outgoing, firstInComposite.Incoming);
			testModel.Combinator.Connect(firstInComposite.Outgoing, secondInComposite.Incoming);
			testModel.Combinator.Connect(secondInComposite.Outgoing, composite.InternalSink.Incoming);
			testModel.Combinator.Connect(composite.Outgoing, sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var compositeAfterStep = (IntFlowComposite)flowComponentsAfterStep[1];
			var firstInCompositeAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[2];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sourceAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			compositeAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.InternalSource.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			firstInCompositeAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.InternalSink.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
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
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way1Sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(way1Sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			Component.Bind(nameof(way2Sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(way2Sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(source.Outgoing, new PortFlowIn<Int, Int>[] { way1Direct.Incoming, way2Direct.Incoming });
			testModel.Combinator.Connect(way1Direct.Outgoing, way1Sink.Incoming);
			testModel.Combinator.Connect(way2Direct.Outgoing, way2Sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var way1DirectAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sink1AfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			var sink2AfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sink1AfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sink2AfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			way1DirectAfterStep.Incoming.BackwardToPredecessor.Should().Be(1);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
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
			Component.Bind(nameof(source1.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source1.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(source2.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source2.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(source1.Outgoing, way1Direct.Incoming);
			testModel.Combinator.Connect(source2.Outgoing, way2Direct.Incoming);
			testModel.Combinator.Connect(new PortFlowOut<Int, Int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var source1AfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var source2AfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			source1AfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
			source2AfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
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
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sinkInside.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sinkInside.SetOutgoingBackward), nameof(testModel.CreateBackward));
			Component.Bind(nameof(sinkOutside.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sinkOutside.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(source.Outgoing, composite.Incoming);
			testModel.Combinator.Connect(composite.InternalSource.Outgoing, new PortFlowIn<Int, Int>[] { way1Direct.Incoming, way2Direct.Incoming });
			testModel.Combinator.Connect(way1Direct.Outgoing, sinkInside.Incoming);
			testModel.Combinator.Connect(way2Direct.Outgoing, composite.InternalSink.Incoming);
			testModel.Combinator.Connect(composite.Outgoing, sinkOutside.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkInsideAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			var sinkOutsideAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkInsideAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sinkOutsideAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
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
			Component.Bind(nameof(sourceOutside.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(sourceOutside.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(sourceInside.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(sourceInside.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(sourceOutside.Outgoing, composite.Incoming);
			testModel.Combinator.Connect(composite.InternalSource.Outgoing, way1Direct.Incoming);
			testModel.Combinator.Connect(sourceInside.Outgoing, way2Direct.Incoming);
			testModel.Combinator.Connect(new PortFlowOut<Int, Int>[] { way1Direct.Outgoing, way2Direct.Outgoing }, composite.InternalSink.Incoming);
			testModel.Combinator.Connect(composite.Outgoing, sink.Incoming);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceInsideAfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sourceOutsideAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceOutsideAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
			sourceInsideAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
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
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.Connect(source.Outgoing, direct.Incoming);
			testModel.Combinator.Connect(stubOut.Outgoing, stubIn.Incoming);

			testModel.Combinator.Replace(stubIn.Incoming, sink.Incoming);
			testModel.Combinator.Replace(stubOut.Outgoing, direct.Outgoing);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}





		// Implicit Ports


		[Test]
		public void SimpleFlowArrives_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.ConnectOutWithIn(source, direct);
			testModel.Combinator.ConnectOutWithIn(direct, sink);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sourceAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			directAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			directAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.BackwardToPredecessor.Should().Be(1);
			directAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
			directAfterStep.Incoming.BackwardToPredecessor.Should().Be(1);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct1 = new IntFlowInToOutSegment();
			var direct2 = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct1, direct2, sink);
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(direct1.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(direct1.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(direct2.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(direct2.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.ConnectOutWithIn(source, direct1);
			testModel.Combinator.ConnectOutWithIn(direct1, direct2);
			testModel.Combinator.ConnectOutWithIn(direct2, sink);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOutSegment();
			var secondInComposite = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, composite, firstInComposite, secondInComposite, sink);
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(firstInComposite.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(firstInComposite.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(secondInComposite.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(secondInComposite.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.ConnectOutWithIn(source, composite);
			testModel.Combinator.ConnectInWithIn(composite, firstInComposite);
			testModel.Combinator.ConnectOutWithIn(firstInComposite, secondInComposite);
			testModel.Combinator.ConnectOutWithOut(secondInComposite, composite);
			testModel.Combinator.ConnectOutWithIn(composite, sink);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var compositeAfterStep = (IntFlowComposite)flowComponentsAfterStep[1];
			var firstInCompositeAfterStep = (IntFlowInToOutSegment)flowComponentsAfterStep[2];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sourceAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			compositeAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.InternalSource.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			firstInCompositeAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.InternalSink.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			compositeAfterStep.Outgoing.ForwardToSuccessor.Should().Be((Int)7);
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be((Int)7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorks_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, way1Direct, way2Direct, way1Sink, way2Sink);
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way1Sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(way1Sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			Component.Bind(nameof(way2Sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(way2Sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.ConnectOutWithIns(source, new IFlowComponentUniqueIncoming<Int, Int>[] { way1Direct, way2Direct });
			testModel.Combinator.ConnectOutWithIn(way1Direct, way1Sink);
			testModel.Combinator.ConnectOutWithIn(way2Direct, way2Sink);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sink1AfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			var sink2AfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sink1AfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sink2AfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var source1 = new IntFlowSource();
			var source2 = new IntFlowSource();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source1, source2, way1Direct, way2Direct, sink);
			Component.Bind(nameof(source1.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source1.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(source2.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source2.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.ConnectOutWithIn(source1, way1Direct);
			testModel.Combinator.ConnectOutWithIn(source2, way2Direct);
			testModel.Combinator.ConnectOutsWithIn(new IFlowComponentUniqueOutgoing<Int, Int>[] { way1Direct, way2Direct }, sink);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var source1AfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var source2AfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			source1AfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
			source2AfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, composite, way1Direct, way2Direct, sinkInside, sinkOutside);
			Component.Bind(nameof(source.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(source.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sinkInside.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sinkInside.SetOutgoingBackward), nameof(testModel.CreateBackward));
			Component.Bind(nameof(sinkOutside.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sinkOutside.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.ConnectOutWithIn(source, composite);
			testModel.Combinator.ConnectInWithIns(composite, new IFlowComponentUniqueIncoming<Int, Int>[] { way1Direct, way2Direct });
			testModel.Combinator.ConnectOutWithIn(way1Direct, sinkInside);
			testModel.Combinator.ConnectOutWithOut(way2Direct, composite);
			testModel.Combinator.ConnectOutWithIn(composite, sinkOutside);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkInsideAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			var sinkOutsideAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkInsideAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sinkOutsideAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite_ImplicitPort()
		{
			var testModel = new IntFlowModel();
			var sourceOutside = new IntFlowSource();
			var sourceInside = new IntFlowSource();
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOutSegment();
			var way2Direct = new IntFlowInToOutSegment();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(sourceOutside, sourceInside, composite, way1Direct, way2Direct, sink);
			Component.Bind(nameof(sourceOutside.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(sourceOutside.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(sourceInside.SetOutgoingForward), nameof(testModel.CreateForward));
			Component.Bind(nameof(sourceInside.BackwardFromSuccessorWasUpdated), nameof(testModel.PrintReceivedBackward));
			Component.Bind(nameof(way1Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way1Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(way2Direct.SetOutgoingForward), nameof(testModel.PassForward));
			Component.Bind(nameof(way2Direct.SetOutgoingBackward), nameof(testModel.PassBackward));
			Component.Bind(nameof(sink.ForwardFromPredecessorWasUpdated), nameof(testModel.PrintReceivedForward));
			Component.Bind(nameof(sink.SetOutgoingBackward), nameof(testModel.CreateBackward));
			testModel.Combinator.ConnectOutWithIn(sourceOutside, composite);
			testModel.Combinator.ConnectInWithIn(composite, way1Direct);
			testModel.Combinator.ConnectOutWithIn(sourceInside, way2Direct);
			testModel.Combinator.ConnectOutsWithOut(new IFlowComponentUniqueOutgoing<Int, Int>[] { way1Direct, way2Direct }, composite);
			testModel.Combinator.ConnectOutWithIn(composite, sink);

			var simulator = new Simulator(new Model(testModel)); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.RootComponents[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.RootComponents[1]).Components;
			var sourceInsideAfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sourceOutsideAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkAfterStep.Incoming.ForwardFromPredecessor.Should().Be(7);
			sourceOutsideAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
			sourceInsideAfterStep.Outgoing.BackwardFromSuccessor.Should().Be(1);
		}
	}
}
