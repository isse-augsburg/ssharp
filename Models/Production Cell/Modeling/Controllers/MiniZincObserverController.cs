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

namespace SafetySharp.CaseStudies.ProductionCell.Modeling.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	internal class MiniZincObserverController : ObserverController
	{
		private const string ConstraintsFile = "Constraints.dzn";
		private const string ConfigurationFile = "Configuration.out";
		private const string MinizincExe = "minizinc.exe";
		private const string MinizincModel = "ConstraintModel.mzn";

		public MiniZincObserverController(IEnumerable<Agent> agents, List<Task> tasks)
			: base(agents, tasks)
		{
		}

		protected override void Reconfigure()
		{
			CreateConstraintsFile();
			ExecuteMinizinc();
			UpdateConfiguration();
		}

		private void CreateConstraintsFile()
		{
			if (Tasks.Count != 1)
				throw new InvalidOperationException("The constraint model expects exactly one task.");

			using (var writer = new StreamWriter(ConstraintsFile))
			{
				var task = String.Join(",", Tasks[0].Capabilities.Select(c => c.Identifier));
				var isCart = String.Join(",", Agents.Select(a => (a is CartAgent).ToString().ToLower()));
				var capabilities = String.Join(",", Agents.Select(a =>
					$"{{{String.Join(",", a.AvailableCapabilites.Select(c => c.Identifier))}}}"));
				var isConnected = String.Join("\n|", Agents.Select(from =>
					String.Join(",", Agents.Select(to => (from.Outputs.Contains(to) || from == to).ToString().ToLower()))));

				writer.WriteLine($"task = [{task}];");
				writer.WriteLine($"noAgents = {Agents.Length};");
				writer.WriteLine($"capabilities = [{capabilities}];");
				writer.WriteLine($"isCart = [{isCart}];");
				writer.WriteLine($"isConnected = [|{isConnected}|]");
			}
		}

		private static void ExecuteMinizinc()
		{
			var startInfo = new ProcessStartInfo
			{
				FileName = MinizincExe,
				Arguments = $"-o {ConfigurationFile} {MinizincModel} {ConstraintsFile}",
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

		private void UpdateConfiguration()
		{
			var lines = File.ReadAllLines(ConfigurationFile);
			if (lines[0].Contains("UNSATISFIABLE"))
			{
				ReconfigurationFailed = true;
				return;
			}

			foreach (var agent in Agents)
			{
				RolePool.Return(agent.AllocatedRoles);
				agent.AllocatedRoles.Clear();
				agent.OnReconfigured();
			}

			var roleAllocations = Parse(lines[0], lines[1]).ToArray();
			for (var i = 0; i < roleAllocations.Length; i++)
			{
				var agent = roleAllocations[i].Item1;
				var capabilities = roleAllocations[i].Item2;

				var role = RolePool.Allocate();
				role.CapabilitiesToApply.Clear();
				role.CapabilitiesToApply.AddRange(capabilities);
				role.Reset();
				role.PreCondition.Task = Tasks[0];
				role.PostCondition.Task = Tasks[0];
				role.PreCondition.Port = i == 0 ? null : roleAllocations[i - 1].Item1;
				role.PostCondition.Port = i == roleAllocations.Length - 1 ? null : roleAllocations[i + 1].Item1;
				role.PreCondition.State.Clear();
				role.PostCondition.State.Clear();

				agent.AllocatedRoles.Add(role);
			}
		}

		private IEnumerable<Tuple<Agent, Capability[]>> Parse(string agentsString, string capabilitiesString)
		{
			var agentIds = ParseList(agentsString);
			var capabilityIds = ParseList(capabilitiesString);

			for (var i = 0; i < agentIds.Length;)
			{
				var capabilities = EnumerateCapabilities(agentIds, capabilityIds, i).ToArray();
				yield return Tuple.Create(Agents[agentIds[i]], capabilities);

				i += Math.Max(1, capabilities.Length);
			}
		}

		private IEnumerable<Capability> EnumerateCapabilities(int[] agents, int[] capabilities, int offset)
		{
			var agentId = agents[offset];
			var agent = Agents[agentId];

			for (var i = offset; i < agents.Length && agents[i] == agentId; ++i)
			{
				if (capabilities[i] != -1)
					yield return agent.AvailableCapabilites.First(c => c == Tasks[0].Capabilities[capabilities[i]]);
			}
		}

		private static int[] ParseList(string line)
		{
			var openBrace = line.IndexOf("[", StringComparison.Ordinal);
			var closeBrace = line.IndexOf("]", StringComparison.Ordinal);

			return line.Substring(openBrace + 1, closeBrace - openBrace - 1).Split(',').Select(n => Int32.Parse(n.Trim()) - 1).ToArray();
		}
	}
}