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

using NUnit.Framework;
using FluentAssertions;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Analysis
{
	using System;
	using Modeling;
	using Modeling.DialyzingFluidDeliverySystem;
	using SafetySharp.Modeling;
	using SafetySharp.Analysis;

	class DialyzingFluidFlowModel : ModelBase
	{
		[Root(RootKind.Controller)]
		public IComponent[] FlowElements;
}

	class DialyzingFluidFlowTests
	{
		[Test]
		public void SimpleFlowArrives()
		{
var supply = new WaterSupply();
var pump = new Pump();
var drain = new Drain();
var combinator = new DialyzingFluidFlowCombinator();
pump.PumpSpeed = 7;
combinator.ConnectOutWithIn(supply.MainFlow,pump.MainFlow);
combinator.ConnectOutWithIn(pump.MainFlow, drain.MainFlow);
combinator.CommitFlow();

			var model = new DialyzingFluidFlowModel
			{
				FlowElements = new IComponent[] { supply, pump, drain, combinator }
			};
			
			var simulator = new SafetySharpSimulator(model); //Important: Call after all objects have been created
			simulator.SimulateStep();

			var modelAfterStep = (DialyzingFluidFlowModel) simulator.Model;
			var supplyAfterStep = (WaterSupply)modelAfterStep.Components[0];
			var pumpAfterStep = (Pump)modelAfterStep.Components[1];
			var drainAfterStep = (Drain)modelAfterStep.Components[2];
			pumpAfterStep.MainFlow.Incoming.Backward.CustomSuctionValue.Should().Be(7);
			supplyAfterStep.MainFlow.Outgoing.Forward.Quantity.Should().Be(7);
			drainAfterStep.MainFlow.Incoming.Forward.Quantity.Should().Be(7);
		}
	}
}
