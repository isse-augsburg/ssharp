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
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using SafetySharp.Modeling;
	using Odp;

	using Role = Odp.Role<Agent, Task, Resource>;

	internal class MiniZincController : Controller
	{
        [Hidden]
        private string _constraintsFile;

        [Hidden]
        private static long myID;
		private const string ConfigurationFile = "Configuration.out";
		private const string MinizincExe = "minizinc.exe";
		private const string MinizincModel = "ConstraintModel.mzn";

		public MiniZincController(IEnumerable<Agent> agents) : base(agents) { }

		public override Dictionary<Agent, IEnumerable<Role>> CalculateConfigurations(params Task[] tasks)
		{
			var configs = new Dictionary<Agent, IEnumerable<Role>>();
			foreach (var task in tasks)
			{
				CreateConstraintsFile(task);
				ExecuteMinizinc();

				// TODO: move comparison to isReconfPossible in CentralReconf subclass?
				//var isReconfPossible = IsReconfPossible(Agents.OfType<RobotAgent>(), Tasks);

				var lines = File.ReadAllLines(ConfigurationFile);
				if (lines[0].Contains("UNSATISFIABLE"))
				{
					ReconfigurationFailure = true;
					//    if (isReconfPossible) 
					//       throw new Exception("Reconfiguration failed even though there is a solution.");
				}

				//if (!isReconfPossible)
				//   throw new Exception("Reconfiguration successful even though there is no valid configuration.");
				else
					ExtractConfigurations(configs, task, Parse(task, lines[0], lines[1]).ToArray());
			}
			return configs;
		}

		private void CreateConstraintsFile(Task task)
		{
		    _constraintsFile = "Constraints"+ ++myID + ".dzn";

            using (var writer = new StreamWriter(_constraintsFile))
			{
				var taskSequence = String.Join(",", task.RequiredCapabilities.Select(c => (c as Capability).Identifier));
				var isCart = String.Join(",", Agents.Select(a => (a is CartAgent).ToString().ToLower()));
				var capabilities = String.Join(",", Agents.Select(a =>
					$"{{{String.Join(",", a.AvailableCapabilities.Select(c => (c as Capability).Identifier))}}}"));
				var isConnected = String.Join("\n|", Agents.Select(from =>
					String.Join(",", Agents.Select(to => (from.Outputs.Contains(to) || from == to).ToString().ToLower()))));

				writer.WriteLine($"task = [{taskSequence}];");
				writer.WriteLine($"noAgents = {Agents.Length};");
				writer.WriteLine($"capabilities = [{capabilities}];");
				writer.WriteLine($"isCart = [{isCart}];");
				writer.WriteLine($"isConnected = [|{isConnected}|]");
			}
		}

		private void ExecuteMinizinc()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = MinizincExe,
				Arguments = $"-o {ConfigurationFile} {MinizincModel} {_constraintsFile}",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true })
			{
				process.Start();

				process.BeginErrorReadLine();
				process.BeginOutputReadLine();

				process.OutputDataReceived += (o, e) => PrintOutput(e.Data);
				process.ErrorDataReceived += (o, e) => PrintOutput(e.Data);

				process.WaitForExit();
			}
		}

		private static void PrintOutput(string output)
		{
			if (String.IsNullOrWhiteSpace(output))
				return;

			if (output.Contains("warning") || output.Contains(Path.GetFileNameWithoutExtension(MinizincModel)))
				return;

			Console.WriteLine(output);
		}

		private IEnumerable<Tuple<Agent, ICapability[]>> Parse(Task task, string agentsString, string capabilitiesString)
		{
			var agentIds = ParseList(agentsString);
			var capabilityIds = ParseList(capabilitiesString);

			for (var i = 0; i < agentIds.Length;)
			{
				var capabilities = EnumerateCapabilities(task, agentIds, capabilityIds, i).ToArray();
				yield return Tuple.Create(Agents[agentIds[i]], capabilities);

				i += Math.Max(1, capabilities.Length);
			}
		}

		private IEnumerable<ICapability> EnumerateCapabilities(Task task, int[] agents, int[] capabilities, int offset)
		{
			var agentId = agents[offset];
			var agent = Agents[agentId];

			for (var i = offset; i < agents.Length && agents[i] == agentId; ++i)
			{
				if (capabilities[i] != -1)
					yield return agent.AvailableCapabilities.First(c => (c as Capability).IsEquivalentTo(task.RequiredCapabilities[capabilities[i]]));
			}
		}

		private static int[] ParseList(string line)
		{
			var openBrace = line.IndexOf("[", StringComparison.Ordinal);
			var closeBrace = line.IndexOf("]", StringComparison.Ordinal);

			return line.Substring(openBrace + 1, closeBrace - openBrace - 1).Split(',').Select(n => Int32.Parse(n.Trim()) - 1).ToArray();
		}

        private bool ContainsCapability(IEnumerable<ICapability> capabilities, ICapability capability)
        {
            return capabilities.Any(c => (c as Capability).IsEquivalentTo(capability));
        }

        private bool IsReconfPossible(IEnumerable<RobotAgent> robotsAgents, IEnumerable<Task> tasks)
        {
            var isReconfPossible = true;
            var matrix = GetConnectionMatrix(robotsAgents);

            foreach (var task in tasks)
            {
                isReconfPossible &=
                    task.RequiredCapabilities.All(capability => robotsAgents.Any(agent => ContainsCapability(agent.AvailableCapabilities,capability)));
                if (!isReconfPossible)
                    break;

                var candidates = robotsAgents.Where(agent => ContainsCapability(agent.AvailableCapabilities,task.RequiredCapabilities.First())).ToArray();

                for (var i = 0; i < task.RequiredCapabilities.Length - 1; i++)
                {
                    candidates =
                        candidates.SelectMany(r => matrix[r])
                                  .Where(r => ContainsCapability(r.AvailableCapabilities,task.RequiredCapabilities[i + 1]))
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
    }
}