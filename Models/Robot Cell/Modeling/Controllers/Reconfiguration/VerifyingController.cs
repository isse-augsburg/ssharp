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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Odp.Reconfiguration;
	using Odp;

	internal class VerifyingController : IController
	{
		private readonly IController _controller;

		public VerifyingController(IController controller)
		{
			_controller = controller;
		}

		// composition
		public BaseAgent[] Agents => _controller.Agents;
		public bool ReconfigurationFailure => _controller.ReconfigurationFailure;

		public event Action<BaseAgent[]> ConfigurationsCalculated
		{
			add { _controller.ConfigurationsCalculated += value; }
			remove { _controller.ConfigurationsCalculated -= value; }
		}

		// delegate calculation to _controller, then verify result
		public Task<ConfigurationUpdate> CalculateConfigurations(object context, params ITask[] tasks)
		{
			var previousFailure = _controller.ReconfigurationFailure;
			var isPossible = IsReconfigurationPossible(tasks);

			var config = _controller.CalculateConfigurations(context, tasks);

			if (!_controller.ReconfigurationFailure && !isPossible)
				throw new Exception("Reconfiguration successful even though there is no valid configuration.");
			if (!previousFailure && _controller.ReconfigurationFailure && isPossible)
				throw new Exception("Reconfiguration failed even though there is a solution.");

			return config;
		}

		private bool IsReconfigurationPossible(IEnumerable<ITask> tasks)
		{
			var isReconfPossible = true;

			var robotsAgents = Agents.OfType<RobotAgent>();
			var matrix = GetConnectionMatrix(robotsAgents);

			foreach (var task in tasks)
			{
				isReconfPossible &= task.RequiredCapabilities.All(capability => robotsAgents.Any(agent => agent.AvailableCapabilities.Contains(capability)));
				if (!isReconfPossible)
					break;

				var candidates = robotsAgents.Where(agent => agent.AvailableCapabilities.Contains(task.RequiredCapabilities.First())).ToArray();

				for (var i = 0; i < task.RequiredCapabilities.Length - 1; i++)
				{
					candidates = candidates.SelectMany(r => matrix[r])
						.Where(r => r.AvailableCapabilities.Contains(task.RequiredCapabilities[i + 1]))
						.ToArray();
					if (candidates.Length == 0)
					{
						isReconfPossible = false;
						break;
					}
				}
			}

			return isReconfPossible;
		}

		private static Dictionary<RobotAgent, List<RobotAgent>> GetConnectionMatrix(IEnumerable<RobotAgent> robotAgents)
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
	}
}