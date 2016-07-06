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

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Modeling;
	using Modeling.Controllers;
	using Modeling.Plants;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public class FunctionalTests
	{
		[Test]
		public void DamagedWorkpieces()
		{
			var model = new Model();
			var safetyAnalysis = new SafetyAnalysis { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, model.Workpieces.Any(w => w.IsDamaged));

			Console.WriteLine(result);
		}

		[Test]
		public void ReconfigurationFailed()
		{
			var model = new Model();
			model.Components.OfType<Robot>().Select(r => r.SwitchFault).ToArray().SuppressActivations();

			foreach (var robot in model.Robots)
				robot.ResourceTransportFault.SuppressActivation();

			var safetyAnalysis = new SafetyAnalysis { Configuration = { CpuCount = 1, StateCapacity = 1 << 16, GenerateCounterExample = false } };
			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, model.ObserverController.ReconfigurationState == ReconfStates.Failed);

			Console.WriteLine(result);
		}

		[Test]
		public void InvariantViolation()
		{
			var model = new Model();
			model.Components.OfType<Robot>().Select(r => r.SwitchFault).ToArray().SuppressActivations();

			var safetyAnalysis = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16, GenerateCounterExample = false } };
			var result = safetyAnalysis.CheckInvariant(model, !Hazard(model));

			Console.WriteLine(result);
		}

	    [Test]
	    public void Eval01Test()
	    {
	        
	    }

		[Test]
		public void Exception()
		{
			var model = new Model();
			model.Faults.SuppressActivations();
			model.Carts[0].Broken.ForceActivation();
			model.Robots[0].ApplyFault.ForceActivation();
			model.Robots[1].Tools.First(t => t.Capability.ProductionAction == ProductionAction.Drill).Broken.ForceActivation();

			var modelChecker = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = modelChecker.CheckInvariant(model, true);

			Assert.IsTrue(result.FormulaHolds);
		}


        private CartParam CreateCart(Robot startPosition, params Route[] routes)
        {
            // compute the transitive closure of the routes
            routes = routes
                .SelectMany(route => Closure(route.Robot1, robot => routes.Where(r => r.Robot1 == robot).Select(r => r.Robot2))
                .Select(target => new Route(route.Robot1, target))).ToArray();

            // make sure we don't have duplicate routes
            routes = routes.Distinct(new RouteComparer()).ToArray();

            // for efficiency (less faults), remove reflexive routes
            routes = routes.Where(route => route.Robot1 != route.Robot2).ToArray();

            var cart = new Cart(startPosition, routes);
            var agent = new CartAgent(cart);
            
            return new CartParam();

        }

	    public class CartParam
	    {
	        private Cart cart { get; set; }
            private CartAgent agent { get; set;}
            private IEnumerable<Route> routes { get; set; }

	    }

    }
}