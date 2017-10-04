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
using System.Linq;
using System.Reflection;
using FluentAssertions;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using SafetySharp.CaseStudies.ZNNSystem.Modeling;
using SafetySharp.Modeling;
using SafetySharp.Analysis;

namespace SafetySharp.CaseStudies.ZNNSystem.Analysis
{
	/// <summary>
	/// Contains a set of tests that simulate the case study to validate certain aspects of its behavior.
	/// </summary>
	public class SimulationTests
	{
		/// <summary>
		/// Tests the model without occuring faults
		/// </summary>
		[Test]
		public void TestModelWithoutFaults()
		{
			var model = new Model();
			model.Faults.SuppressActivations();

            var simulator = new SafetySharpSimulator(model);
            model = (Model) simulator.Model;
			simulator.FastForward(steps: 120);

			model.Servers.First().QueryCompleteCount.Should().BeGreaterOrEqualTo(1);
			for(int i = 0; i < model.Servers.Count; i++)
				Console.WriteLine("Completed Queries of server " + i + ": " + model.Servers[i].QueryCompleteCount);
		}

		/// <summary>
		/// Tests the model with occuring fault based on activation criteria
		/// </summary>
		[Test]
		public void TestModelWithFaults()
		{
			var model = new Model();

			// Get faults of proxy
			var proxyFaults = model.Proxy.GetType().GetFields().Where(prop => prop.IsDefined(typeof(FaultActivationAttribute), false));
			foreach(var pFault in proxyFaults)
			{
				var attr = (FaultActivationAttribute) pFault.GetCustomAttribute(typeof(FaultActivationAttribute), false);
				var canAct = (bool) attr.ActivationProperty.GetValue(model.Proxy);
				var fault = (Fault)pFault.GetValue(model.Proxy);
				fault.Activation = canAct ? Activation.Forced : Activation.Suppressed;
			}



			var simulator = new SafetySharpSimulator(model);
			model = (Model) simulator.Model;
			simulator.FastForward(steps: 120);

			model.Servers.First().QueryCompleteCount.Should().BeGreaterOrEqualTo(1);
			for(int i = 0; i < model.Servers.Count; i++)
				Console.WriteLine("Completed Queries of server " + i + ": " + model.Servers[i].QueryCompleteCount);
		}
	}
}