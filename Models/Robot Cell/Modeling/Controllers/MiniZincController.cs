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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Odp;

	internal class MiniZincController : AbstractMiniZincController<Agent>
	{
		private const string MinizincModel = "ConstraintModel.mzn";

		public MiniZincController(IEnumerable<Agent> agents) : base(MinizincModel, agents.ToArray()) { }

		protected override void WriteInputData(ITask task, StreamWriter writer)
		{
			var taskSequence = String.Join(",", task.RequiredCapabilities.Select(GetIdentifier));
			var isCart = String.Join(",", Agents.Select(a => (a is CartAgent).ToString().ToLower()));
			var capabilities = String.Join(",", Agents.Select(a =>
				$"{{{String.Join(",", a.AvailableCapabilities.Select(GetIdentifier))}}}"));
			var isConnected = String.Join("\n|", Agents.Select(from =>
				String.Join(",", Agents.Select(to => (from.Outputs.Contains(to) || from == to).ToString().ToLower()))));

			writer.WriteLine($"task = [{taskSequence}];");
			writer.WriteLine($"noAgents = {Agents.Length};");
			writer.WriteLine($"capabilities = [{capabilities}];");
			writer.WriteLine($"isCart = [{isCart}];");
			writer.WriteLine($"isConnected = [|{isConnected}|]");
		}

		private int GetIdentifier(ICapability capability)
		{
			if (capability is ProduceCapability)
				return 1;
			else if (capability is ProcessCapability)
				return (int)(capability as ProcessCapability).ProductionAction + 1;
			else if (capability is ConsumeCapability)
				return (int)Enum.GetValues(typeof(ProductionAction)).Cast<ProductionAction>().Max() + 2;
			throw new InvalidOperationException("unsupported capability");
		}

		#region isReconfPossible
		// TODO: move comparison with isReconfPossible into CentralReconf subclass (?)

        private bool IsReconfPossible(IEnumerable<RobotAgent> robotsAgents, IEnumerable<Task> tasks)
        {
            var isReconfPossible = true;
            var matrix = GetConnectionMatrix(robotsAgents);

            foreach (var task in tasks)
            {
                isReconfPossible &=
                    task.RequiredCapabilities.All(capability => robotsAgents.Any(agent => agent.AvailableCapabilities.Contains(capability)));
                if (!isReconfPossible)
                    break;

                var candidates = robotsAgents.Where(agent => agent.AvailableCapabilities.Contains(task.RequiredCapabilities.First())).ToArray();

                for (var i = 0; i < task.RequiredCapabilities.Length - 1; i++)
                {
                    candidates =
                        candidates.SelectMany(r => matrix[r])
                                  .Where(r => r.AvailableCapabilities.Contains(task.RequiredCapabilities[i + 1]))
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

        private static bool IsConnected(RobotAgent source, RobotAgent target, HashSet<RobotAgent> seenRobots)
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
		#endregion
	}
}