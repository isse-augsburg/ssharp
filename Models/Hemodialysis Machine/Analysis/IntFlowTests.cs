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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Analysis
{
	using System.Diagnostics;
	using Modeling;
	using SafetySharp.Modeling;
	using Runtime;
	using SafetySharp.Analysis;
	using Utilities;
	using Utilities.BidirectionalFlow;

	class IntFlowModel : ModelBase
	{
		[Root(RootKind.Controller)]
		public readonly IntFlowCombinator Combinator = new IntFlowCombinator();

		[Root(RootKind.Controller)]
		public new IntFlowComponentCollection Components;

		
		[Provided]
		public Int CreateForward()
		{
			return new Int(7);
		}

		[Provided]
		public Int CreateBackward()
		{
			return new Int(1);
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
		public void SimpleFlowArrives()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct = new IntFlowInToOut();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct, sink);

			source.SendForward = testModel.CreateForward;
			source.ReceivedBackward = testModel.PrintReceivedBackward;
			sink.SendBackward = testModel.CreateBackward;
			sink.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIn(source, direct);
			testModel.Combinator.ConnectOutWithIn(direct, sink);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var directAfterStep = (IntFlowInToOut)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sourceAfterStep.Outgoing.Forward.Should().Be((Int)7);
			directAfterStep.Incoming.Forward.Should().Be((Int)7);
			directAfterStep.Outgoing.Forward.Should().Be((Int)7);
			sinkAfterStep.Incoming.Forward.Should().Be((Int)7);
			sinkAfterStep.Incoming.Backward.Should().Be(1);
			directAfterStep.Outgoing.Backward.Should().Be(1);
			directAfterStep.Incoming.Backward.Should().Be(1);
			sourceAfterStep.Outgoing.Backward.Should().Be(1);
		}


		[Test]
		public void TwoStepFlowArrives()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var direct1 = new IntFlowInToOut();
			var direct2 = new IntFlowInToOut();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, direct1, direct2, sink);

			source.SendForward = testModel.CreateForward;
			source.ReceivedBackward = testModel.PrintReceivedBackward;
			sink.SendBackward = testModel.CreateBackward;
			sink.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIn(source, direct1);
			testModel.Combinator.ConnectOutWithIn(direct1, direct2);
			testModel.Combinator.ConnectOutWithIn(direct2, sink);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			sinkAfterStep.Incoming.Forward.Should().Be(7);
			sourceAfterStep.Outgoing.Backward.Should().Be(1);
		}

		[Test]
		public void DelegateFlowArrives()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var del = new IntFlowDelegate();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, del, sink);

			source.SendForward = testModel.CreateForward;
			source.ReceivedBackward = testModel.PrintReceivedBackward;
			sink.SendBackward = testModel.CreateBackward;
			sink.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIn(source, del);
			testModel.Combinator.ConnectOutWithIn(del, sink);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var delAfterStep = (IntFlowDelegate)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[2];
			sourceAfterStep.Outgoing.Forward.Should().Be((Int)7);
			delAfterStep.Incoming.Forward.Should().Be((Int)7);
			delAfterStep.Outgoing.Forward.Should().Be((Int)7);
			sinkAfterStep.Incoming.Forward.Should().Be((Int)7);
			sinkAfterStep.Incoming.Backward.Should().Be(1);
			delAfterStep.Outgoing.Backward.Should().Be(1);
			delAfterStep.Incoming.Backward.Should().Be(1);
			sourceAfterStep.Outgoing.Backward.Should().Be(1);
		}

		[Test]
		public void CompositeFlowArrives()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var composite = new IntFlowComposite();
			var firstInComposite = new IntFlowInToOut();
			var secondInComposite = new IntFlowInToOut();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, composite, firstInComposite, secondInComposite, sink);

			source.SendForward = testModel.CreateForward;
			source.ReceivedBackward = testModel.PrintReceivedBackward;
			sink.SendBackward = testModel.CreateBackward;
			sink.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIn(source, composite);
			testModel.Combinator.ConnectInWithIn(composite, firstInComposite);
			testModel.Combinator.ConnectOutWithIn(firstInComposite, secondInComposite);
			testModel.Combinator.ConnectOutWithOut(secondInComposite, composite);
			testModel.Combinator.ConnectOutWithIn(composite, sink);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var compositeAfterStep = (IntFlowComposite)flowComponentsAfterStep[1];
			var firstInCompositeAfterStep = (IntFlowInToOut)flowComponentsAfterStep[2];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sourceAfterStep.Outgoing.Forward.Should().Be((Int)7);
			compositeAfterStep.Incoming.Forward.Should().Be((Int)7);
			compositeAfterStep.FlowIn.Outgoing.Forward.Should().Be((Int)7);
			firstInCompositeAfterStep.Incoming.Forward.Should().Be((Int)7);
			compositeAfterStep.FlowOut.Outgoing.Forward.Should().Be((Int)7);
			compositeAfterStep.Outgoing.Forward.Should().Be((Int)7);
			sinkAfterStep.Incoming.Forward.Should().Be((Int)7);
			sourceAfterStep.Outgoing.Backward.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorks()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var way1Direct = new IntFlowInToOut();
			var way2Direct = new IntFlowInToOut();
			var way1Sink = new IntFlowSink();
			var way2Sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, way1Direct, way2Direct, way1Sink, way2Sink);

			source.SendForward = testModel.CreateForward;
			source.ReceivedBackward = testModel.PrintReceivedBackward;
			way1Sink.SendBackward = testModel.CreateBackward;
			way1Sink.ReceivedForward = testModel.PrintReceivedForward;
			way2Sink.SendBackward = testModel.CreateBackward;
			way2Sink.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIns(source, new IFlowComponentUniqueIncoming<Int, Int>[] { way1Direct, way2Direct });
			testModel.Combinator.ConnectOutWithIn(way1Direct, way1Sink);
			testModel.Combinator.ConnectOutWithIn(way2Direct, way2Sink);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sink1AfterStep = (IntFlowSink)flowComponentsAfterStep[3];
			var sink2AfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sink1AfterStep.Incoming.Forward.Should().Be(7);
			sink2AfterStep.Incoming.Forward.Should().Be(7);
			sourceAfterStep.Outgoing.Backward.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorks()
		{
			var testModel = new IntFlowModel();
			var source1 = new IntFlowSource();
			var source2 = new IntFlowSource();
			var way1Direct = new IntFlowInToOut();
			var way2Direct = new IntFlowInToOut();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source1, source2, way1Direct, way2Direct, sink);

			source1.SendForward = testModel.CreateForward;
			source1.ReceivedBackward = testModel.PrintReceivedBackward;
			source2.SendForward = testModel.CreateForward;
			source2.ReceivedBackward = testModel.PrintReceivedBackward;
			sink.SendBackward = testModel.CreateBackward;
			sink.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIn(source1, way1Direct);
			testModel.Combinator.ConnectOutWithIn(source2, way2Direct);
			testModel.Combinator.ConnectOutsWithIn(new IFlowComponentUniqueOutgoing<Int, Int>[] { way1Direct, way2Direct }, sink);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var source1AfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var source2AfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			sinkAfterStep.Incoming.Forward.Should().Be(7);
			source1AfterStep.Outgoing.Backward.Should().Be(1);
			source2AfterStep.Outgoing.Backward.Should().Be(1);
		}

		[Test]
		public void SplitFlowWorksWithComposite()
		{
			var testModel = new IntFlowModel();
			var source = new IntFlowSource();
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOut();
			var way2Direct = new IntFlowInToOut();
			var sinkInside = new IntFlowSink();
			var sinkOutside = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(source, composite, way1Direct, way2Direct, sinkInside, sinkOutside);

			source.SendForward = testModel.CreateForward;
			source.ReceivedBackward = testModel.PrintReceivedBackward;
			sinkInside.SendBackward = testModel.CreateBackward;
			sinkInside.ReceivedForward = testModel.PrintReceivedForward;
			sinkOutside.SendBackward = testModel.CreateBackward;
			sinkOutside.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIn(source, composite);
			testModel.Combinator.ConnectInWithIns(composite, new IFlowComponentUniqueIncoming<Int, Int>[] { way1Direct, way2Direct });
			testModel.Combinator.ConnectOutWithIn(way1Direct, sinkInside);
			testModel.Combinator.ConnectOutWithOut(way2Direct, composite);
			testModel.Combinator.ConnectOutWithIn(composite, sinkOutside);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var sourceAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkInsideAfterStep = (IntFlowSink)flowComponentsAfterStep[4];
			var sinkOutsideAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkInsideAfterStep.Incoming.Forward.Should().Be(7);
			sinkOutsideAfterStep.Incoming.Forward.Should().Be(7);
			sourceAfterStep.Outgoing.Backward.Should().Be(1);
		}

		[Test]
		public void MergeFlowWorksWithComposite()
		{
			var testModel = new IntFlowModel();
			var sourceOutside = new IntFlowSource();
			var sourceInside = new IntFlowSource();
			var composite = new IntFlowComposite();
			var way1Direct = new IntFlowInToOut();
			var way2Direct = new IntFlowInToOut();
			var sink = new IntFlowSink();
			testModel.Components = new IntFlowComponentCollection(sourceOutside, sourceInside, composite, way1Direct, way2Direct, sink);

			sourceOutside.SendForward = testModel.CreateForward;
			sourceOutside.ReceivedBackward = testModel.PrintReceivedBackward;
			sourceInside.SendForward = testModel.CreateForward;
			sourceInside.ReceivedBackward = testModel.PrintReceivedBackward;
			sink.SendBackward = testModel.CreateBackward;
			sink.ReceivedForward = testModel.PrintReceivedForward;
			testModel.Combinator.ConnectOutWithIn(sourceOutside, composite);
			testModel.Combinator.ConnectInWithIn(composite, way1Direct);
			testModel.Combinator.ConnectOutWithIn(sourceInside, way2Direct);
			testModel.Combinator.ConnectOutsWithOut(new IFlowComponentUniqueOutgoing<Int, Int>[] { way1Direct, way2Direct }, composite);
			testModel.Combinator.ConnectOutWithIn(composite, sink);
			testModel.Combinator.CommitFlow();

			var simulator = new SafetySharpSimulator(testModel); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var flowCombinatorAfterStep = (IntFlowCombinator)simulator.Model.Roots[0];
			var flowComponentsAfterStep = ((IntFlowComponentCollection)simulator.Model.Roots[1]).Components;
			var sourceInsideAfterStep = (IntFlowSource)flowComponentsAfterStep[1];
			var sourceOutsideAfterStep = (IntFlowSource)flowComponentsAfterStep[0];
			var sinkAfterStep = (IntFlowSink)flowComponentsAfterStep[5];
			sinkAfterStep.Incoming.Forward.Should().Be(7);
			sourceOutsideAfterStep.Outgoing.Backward.Should().Be(1);
			sourceInsideAfterStep.Outgoing.Backward.Should().Be(1);
		}
	}
}
