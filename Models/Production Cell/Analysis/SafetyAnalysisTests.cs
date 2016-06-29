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
	using System.IO;
	using System.Linq;
	using Modeling;
	using Modeling.Controllers;
	using Modeling.Plants;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	public class SafetyAnalysisTests
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

			var safetyAnalysis = new SafetyAnalysis { Configuration = { CpuCount = 1, StateCapacity = 1 << 16, GenerateCounterExample = false } };
			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, Hazard(model));

			Console.WriteLine(result);
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

		private bool Hazard(Model model)
		{
			var agents = model.CartAgents.Cast<Agent>().Concat(model.RobotAgents).ToArray();

			if (model.ObserverController.ReconfigurationState == ReconfStates.NotSet)
				return false;

			if (model.ObserverController.ReconfigurationState == ReconfStates.Failed &&
				IsReconfPossible(model.RobotAgents, model.CartAgents, model.Tasks, model.ObserverController))
				return true;

			if (model.ObserverController.ReconfigurationState == ReconfStates.Failed)
				return false;

//			foreach (var agent in agents)
//			{
//				foreach (var constraint in agent.Constraints)
//				{
//					if (!constraint())
//						;
//				}
//			}

			return agents.Any(agent => agent.Constraints.Any(constraint => !constraint()));
		}

		private bool IsReconfPossible(IEnumerable<RobotAgent> robotsAgents, IEnumerable<CartAgent> cartAgents, IEnumerable<Task> tasks,
									  ObserverController observerController)
		{
			var isReconfPossible = true;
			var matrix = GetConnectionMatrix(robotsAgents);

			foreach (var task in tasks)
			{
				isReconfPossible &= task.Capabilities.All(capability => robotsAgents.Any(agent => agent.AvailableCapabilites.Contains(capability)));
				if (!isReconfPossible)
					break;

				var candidates = robotsAgents.Where(agent => agent.AvailableCapabilites.Contains(task.Capabilities.First())).ToArray();

				for (var i = 0; i < task.Capabilities.Length - 1; i++)
				{
					candidates =
						candidates.SelectMany<RobotAgent, RobotAgent>(r => matrix[r])
								  .Where(r => r.AvailableCapabilites.Contains(task.Capabilities[i + 1]))
								  .ToArray();
					if (candidates.Length == 0)
					{
						isReconfPossible = false;
						goto end;
					}
				}
			}

			end:

			if (isReconfPossible == observerController.ReconfigurationState.Equals(ReconfStates.Failed))
			{
				var agents = robotsAgents.Cast<Agent>().Concat(cartAgents).ToArray();
				using (var writer = new StreamWriter("counterFile"))
				{
					var isCart = String.Join(",", agents.Select(a => (a is CartAgent).ToString().ToLower()));
					var capabilities = String.Join(",", agents.Select(a =>
						$"{{{String.Join(",", a.AvailableCapabilites.Select(c => c.Identifier))}}}"));
					var isConnected = String.Join("\n|", agents.Select(from =>
						String.Join(",", agents.Select(to => (from.Outputs.Contains(to) || from == to).ToString().ToLower()))));

					writer.WriteLine($"noAgents = {agents.Length};");
					writer.WriteLine($"capabilities = [{capabilities}];");
					writer.WriteLine($"isCart = [{isCart}];");
					writer.WriteLine($"isConnected = [|{isConnected}|]");
				}
			}

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