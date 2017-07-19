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

namespace SafetySharp.Odp.Reconfiguration
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using Modeling;

    // TODO: should ignore dead agents
	public abstract class AbstractMiniZincController : AbstractController
	{
		private readonly string _constraintsModel;
		[Hidden]
		private string _inputFile;
		[Hidden]
		private string _outputFile;

		public static string MiniZinc = "minizinc.exe";
		private static int _counter = 0;

		protected AbstractMiniZincController(string constraintsModel, BaseAgent[] agents) : base(agents)
		{
			_constraintsModel = constraintsModel;
		}

		// synchronous implementation
		public override Task<ConfigurationUpdate> CalculateConfigurations(object context, ITask task)
		{
			var configs = new ConfigurationUpdate();
		    configs.RecordInvolvement(GetAvailableAgents()); // central controller uses all agents!
			configs.RemoveAllRoles(task, Agents);
			lock(MiniZinc)
			{
				CreateDataFile(task);
				ExecuteMiniZinc();
				ParseConfigurations(configs, task);
			}

			OnConfigurationsCalculated(task, configs);
			return Task.FromResult(configs);
		}

		private void CreateDataFile(ITask task)
		{
			_inputFile = $"data{++_counter}.dzn";
			using (var writer = new StreamWriter(_inputFile))
			{
				WriteInputData(task, writer);
			}
		}

		protected abstract void WriteInputData(ITask task, StreamWriter writer);

		private void ExecuteMiniZinc()
		{
			_outputFile = $"output{_counter}.dzn";
			var startInfo = new ProcessStartInfo
			{
				FileName = MiniZinc,
				Arguments = $"-o {_outputFile} {_constraintsModel} {_inputFile}",
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

				process.OutputDataReceived += (o, e) => Console.WriteLine(e.Data);
				process.ErrorDataReceived += (o, e) => Console.WriteLine(e.Data);

				process.WaitForExit();
			}
		}

		private void ParseConfigurations(ConfigurationUpdate configs, ITask task)
		{
			var lines = File.ReadAllLines(_outputFile);
			if (lines[0].Contains("UNSATISFIABLE"))
			{
				configs.Fail();
				return;
			}

			var agentIds = ParseList(lines[0]);
			var capabilityIds = ParseList(lines[1]);

			var role = default(Role);
			BaseAgent lastAgent = null;

			for (int i = 0; i < agentIds.Length; ++i)
			{
				var agent = GetAgent(agentIds[i]);
				// connect to previous role
				role.PostCondition.Port = agent;
				// get new role
				role = GetRole(task, lastAgent, lastAgent == null ? null : (Condition?)role.PostCondition);

				// collect capabilities for the current agent into one role
				for (var current = agentIds[i]; i < agentIds.Length && current == agentIds[i]; ++i)
				{
					if (capabilityIds[i] >= 0)
					{
						var capability = task.RequiredCapabilities[capabilityIds[i]];
						role.AddCapability(capability);
					}
				}

				configs.AddRoles(agent, role);
				lastAgent = agent;
			}
		}

		private static int[] ParseList(string line)
		{
			var openBrace = line.IndexOf("[", StringComparison.Ordinal);
			var closeBrace = line.IndexOf("]", StringComparison.Ordinal);

			return line.Substring(openBrace + 1, closeBrace - openBrace - 1).Split(',')
				.Select(n => int.Parse(n.Trim()) - 1).ToArray();
		}

		protected virtual BaseAgent GetAgent(int index)
		{
			return Agents[index];
		}
	}
}