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
			var model = Model.GetDefaultInstance();
			var safetyAnalysis = new SafetyAnalysis { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, model.Workpieces.Any(w => w.IsDamaged));

			Console.WriteLine(result);
		}

		[Test]
		public void ReconfigurationFailed()
		{
			var model = Model.GetDefaultInstance();
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
			var model = Model.GetDefaultInstance();
			model.Components.OfType<Robot>().Select(r => r.SwitchFault).ToArray().SuppressActivations();

			var safetyAnalysis = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16, GenerateCounterExample = false } };
			var result = safetyAnalysis.CheckInvariant(model, !Hazard(model));

			Console.WriteLine(result);
		}

		[Test]
		public void Exception()
		{
			var model = Model.GetDefaultInstance();
			model.Faults.SuppressActivations();
			model.Carts[0].Broken.ForceActivation();
			model.Robots[0].ApplyFault.ForceActivation();
			model.Robots[1].Tools.First(t => t.Capability.ProductionAction == ProductionAction.Drill).Broken.ForceActivation();

			var modelChecker = new SSharpChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 16 } };
			var result = modelChecker.CheckInvariant(model, true);

			Assert.IsTrue(result.FormulaHolds);
		}

	    [Test]
	    public void EvalIctss1()
	    {
	        var model = new Model();

	        model.ObserverController = new MiniZincObserverController(model.RobotAgents.Cast<Agent>().Concat(model.CartAgents), model.Tasks);

            var produce = (Func<ProduceCapability>)(() => new ProduceCapability(model.Resources, model.Tasks));
            var insert = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Insert));
            var drill = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Drill));
            var tighten = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Tighten));
            var polish = (Func<ProcessCapability>)(() => new ProcessCapability(ProductionAction.Polish));
            var consume = (Func<ConsumeCapability>)(() => new ConsumeCapability());

            model.CreateWorkpieces(5, produce(), drill(), insert(), tighten(), polish(), consume());

            model.CreateRobot(produce(), drill(), insert());
            model.CreateRobot(insert(), drill());
            model.CreateRobot(tighten(), polish(), tighten(), drill());
            model.CreateRobot(polish(), consume());

            model.CreateCart(model.Robots[0], new Route(model.Robots[0], model.Robots[1]), new Route(model.Robots[0], model.Robots[2]), new Route(model.Robots[0], model.Robots[3]));
            model.CreateCart(model.Robots[1], new Route(model.Robots[1], model.Robots[2]), new Route(model.Robots[0], model.Robots[1]));
            model.CreateCart(model.Robots[2], new Route(model.Robots[2], model.Robots[3]));
        }

		private bool Hazard(Model model)
		{
			var agents = model.CartAgents.Cast<Agent>().Concat(model.RobotAgents).ToArray();

			if (model.ObserverController.ReconfigurationState == ReconfStates.NotSet)
				return false;

            if (model.ObserverController.ReconfigurationState == ReconfStates.Succedded &&
                !IsReconfPossible(model.RobotAgents, model.CartAgents, model.Tasks, model.ObserverController))
                    return true;

			if (model.ObserverController.ReconfigurationState == ReconfStates.Failed &&
				IsReconfPossible(model.RobotAgents, model.CartAgents, model.Tasks, model.ObserverController))
				return true;

			if (model.ObserverController.ReconfigurationState == ReconfStates.Failed)
				return false;

		    if (CheckConstraints(agents))
		        return false; 

            return false;
		}

	    private bool CheckConstraints(IEnumerable<Agent> agents)
	    {
	        foreach (var agent in agents)
	        {
	            if (agent.AllocatedRoles.All(role => role.PreCondition.Port == null || agent.Inputs.Contains(role.PreCondition.Port)))
	            {
	                return false;
	            }
	            if (agent.AllocatedRoles.All(role => role.PostCondition.Port == null || agent.Outputs.Contains(role.PostCondition.Port)))
	            {
	                return false; 
	            }
	            if (agent.AllocatedRoles.All(
	                role => role.CapabilitiesToApply.All(capability => agent.AvailableCapabilites.Contains(capability))))
	            {
	                return false;
	            }
	            if (agent.AllocatedRoles.Any(role => role.PostCondition.Port == null || role.PreCondition.Port == null)
	                ? true
	                : agent.AllocatedRoles.TrueForAll(role => PostMatching(role, agent) && PreMatching(role, agent)))
	            {
	                return false; 
	            }

	        }
	        return true;
	     }

        private bool PostMatching(Role role, Agent agent)
        {
            if (!role.PostCondition.Port.AllocatedRoles.Any(role1 => role1.PreCondition.Port.Equals(agent)))
            {
                ;
            }
            else if (
                !role.PostCondition.Port.AllocatedRoles.Any(
                    role1 =>
                        role.PostCondition.State.Select(capability => capability.Identifier)
                            .SequenceEqual(role1.PreCondition.State.Select(capability => capability.Identifier))))
            {
                ;
            }
            else if (!role.PostCondition.Port.AllocatedRoles.Any(role1 => role.PostCondition.Task.Equals(role1.PreCondition.Task)))
            {
                ;
            }

            return role.PostCondition.Port.AllocatedRoles.Any(role1 => role1.PreCondition.Port.Equals(agent)
                                                                       &&
                                                                       role.PostCondition.State.Select(capability => capability.Identifier)
                                                                           .SequenceEqual(role1.PreCondition.State.Select(capability => capability.Identifier))
                                                                       && role.PostCondition.Task.Equals(role1.PreCondition.Task));
        }

        private bool PreMatching(Role role, Agent agent)
        {
            return role.PreCondition.Port.AllocatedRoles.Any(role1 => role1.PostCondition.Port.Equals(agent)
                                                                      && role.PreCondition.State.SequenceEqual(role1.PostCondition.State)
                                                                      && role.PreCondition.Task.Equals(role1.PostCondition.Task));
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
					}
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