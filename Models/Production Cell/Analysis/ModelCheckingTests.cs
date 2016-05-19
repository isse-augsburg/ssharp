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

namespace SafetySharp.CaseStudies.ProductionCell.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using Modeling;
    using Modeling.Controllers;
	using Modeling.Plants;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public class ModelCheckingTests
	{
		[Test]
		public void EnumerateStateSpace()
		{
			var model = new Model();
			var modelChecker = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = modelChecker.CheckInvariant(model, true);

			Assert.IsTrue(result.FormulaHolds);
		}

		[Test]
		public void NoDamagedWorkpieces()
		{
			var model = new Model();
			model.Faults.SuppressActivations();

			var modelChecker = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = modelChecker.CheckInvariant(model, !model.Workpieces.Any(w => w.IsDamaged));

			Assert.IsTrue(result.FormulaHolds);
		}

		[Test]
		public void AllWorkpiecesCompleteEventually()
		{
			var model = new Model();
			model.Faults.SuppressActivations();

			var modelChecker = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = modelChecker.CheckInvariant(model, !model.Workpieces.All(w => w.IsComplete));

			Assert.IsFalse(result.FormulaHolds);
		}

		[Test]
		public void HasResourceAndHasWorkpieceMatch()
		{
			var model = new Model();
			model.Faults.SuppressActivations();

			var modelChecker = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = modelChecker.CheckInvariant(model, 
				model.RobotAgents.All(a => a.HasResource == a.Robot.HasWorkpiece) &&
				model.CartAgents.All(a => a.HasResource == a.Cart.HasWorkpiece));

			Assert.IsTrue(result.FormulaHolds);
		}

		[Test]
		public void IsReconfPossible()
		{
			var model = new Model();
			var hazard =
				!((Formula)IsReconfPossible(model.RobotAgents, model.CartAgents, model.Tasks, model.ObserverController)).EquivalentTo(
					!model.ObserverController.ReconfigurationFailed);

			var safetyAnalysis = new SafetyAnalysis { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, hazard, maxCardinality: 2);

			Console.WriteLine(result);
		}


		private bool IsReconfPossible(IEnumerable<RobotAgent> robotsAgents, IEnumerable<CartAgent> cartAgents, IEnumerable<Task> tasks, ObserverController observerController)
		{
	        var isReconfPossible = true;
			var matrix = GetConnectionMatrix(robotsAgents);

            foreach (var task in tasks)
            {
                isReconfPossible &= task.Capabilities.All(capability => robotsAgents.Any(agent => agent.AvailableCapabilites.Contains(capability)));
	            if (!isReconfPossible)
		            break;

	            var candidates = robotsAgents.Where(agent => agent.AvailableCapabilites.Contains(task.Capabilities.First())).ToArray();

                for (var i = 0; i < task.Capabilities.Length-1; i++)
                {
					candidates = candidates.SelectMany(r => matrix[r]).Where(r => r.AvailableCapabilites.Contains(task.Capabilities[i + 1])).ToArray();
	                if (candidates.Length == 0)
	                {
		                isReconfPossible = false;
		                goto end;
	                }
                }
            }

			end:
			if (isReconfPossible == observerController.ReconfigurationFailed)
				;

	        return isReconfPossible;
	    }

		private Dictionary<RobotAgent, List<RobotAgent>> GetConnectionMatrix(IEnumerable<RobotAgent> robotAgents)
		{
			var matrix = new Dictionary<RobotAgent, List<RobotAgent>>();

			foreach (var robot in robotAgents)
			{
				var list = new List<RobotAgent>(robotAgents.Where(r => IsConnected(robot, r, new HashSet<RobotAgent>())));
				matrix.Add(robot, list);
			}

			return matrix;
		}

	    private bool IsConnected(RobotAgent source, RobotAgent target, HashSet<RobotAgent> seenRobots)
	    {
		    if (source == target)
			    return true;

		    if (!seenRobots.Add(source))
			    return false;

		    foreach (var output in source.Outputs)
		    {
			    foreach (var output2 in output.Outputs)
			    {
				    if (output2 == target)
					    return true;

				    if (IsConnected((RobotAgent)output2, target, seenRobots))
					    return true;
			    }
		    }

		    return false;
	    }
	}
}