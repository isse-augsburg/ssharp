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

namespace ProductionCell
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;

	internal class MiniZincObserverController : ObserverController
	{
		private const String DataFile = "data.dzn";
		private const String SolutionFile = "s.sol";
		private const String MinizincExe = "minizinc.exe";
		private const String MinizincModel = "rolealloc-compact.mzn";

		public override void Reconfigure()
		{
			var sw = new Stopwatch();
			sw.Start();
			CreateDznFileFromCurrentConfig();
			ExecuteMinizinc();

			EnroleConfiguration(ReadVar(SolutionFile));
			//Console.WriteLine($"{sw.Elapsed.TotalMilliseconds}ms");
		}

		private void EnroleConfiguration(Tuple<int[], int[]> varVals)
		{
			//Console.WriteLine("Applying roles...");

			var agents = varVals.Item1;
			var workedOn = varVals.Item2;
			foreach (var agent in Agents)
			{
				RolePool.AddRange(agent.AllocatedRoles);
				agent.AllocatedRoles.Clear();
			}

			for (int i = 0; i < agents.Length; i++)
			{
				if (workedOn[i] == 0)
					continue;

				var cap = Agents[agents[i] - 1].AvailableCapabilites.First(c => c == CurrentTask.RequiresCapabilities[workedOn[i] - 1]);

				OdpRole roleToAllocate = RolePool.Last();
				RolePool.RemoveAt(RolePool.Count - 1);
				roleToAllocate.CapabilitiesToApply.Clear();
				roleToAllocate.CapabilitiesToApply.Add(cap);

				Agents[agents[i] - 1].AllocatedRoles.Add(roleToAllocate);

				//Console.WriteLine($"Agent {agents[i] - 1}: Add capability {cap}");
			}
		}

		private static void ExecuteMinizinc()
		{
			//Console.WriteLine("Executing Minizinc");

			var startInfo = new ProcessStartInfo
			{
				FileName = MinizincExe,
				Arguments = "-o " + SolutionFile + " " + MinizincModel + " " + DataFile,
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
			process.Start();

			process.BeginErrorReadLine();
			process.BeginOutputReadLine();

			//process.OutputDataReceived += (o, e) => Console.WriteLine(e.Data);
			//process.ErrorDataReceived += (o, e) => Console.WriteLine(e.Data);

			process.WaitForExit();
		}

		private void CreateDznFileFromCurrentConfig()
		{
			List<string> capabilities = new List<string>(Agents.Count);
			List<bool> isCart = new List<bool>(Agents.Count);
			foreach (var agent in Agents)
			{
				capabilities.Add("{" + string.Join(",", agent.AvailableCapabilites) + "}");
				isCart.Add(agent.IsCart);
			}

			IEnumerable<Tuple<String, String>> dnzVars = new List<Tuple<string, string>>
			{
				Tuple.Create("task", "[" + string.Join(",", CurrentTask.GetTaskAsStrings()) + "]"),
				Tuple.Create("noAgents", Agents.Count().ToString()),
				Tuple.Create("capabilities", "[" + string.Join(",", capabilities.ToArray()) + "]"),
				Tuple.Create("isCart", "[" + string.Join(",", isCart.ToArray()).ToLower() + "]")
			};

			//foreach (var v in dnzVars)
			//	Console.WriteLine($"{v.Item1} {v.Item2}");

			CreateDznFile(dnzVars);
		}

		private void CreateDznFile(IEnumerable<Tuple<String, String>> data)
		{
			List<String> lines = new List<string>();
			foreach (var d in data)
			{
				lines.Add($"{d.Item1} = {d.Item2};");
			}
			File.WriteAllLines(DataFile, lines);
		}

		private Tuple<int[], int[]> ReadVar(String file)
		{
			var lines = File.ReadAllLines(file);
			var agentsData = new int[] { };
			var workedOnData = new int[] { };

			foreach (var line in lines)
			{
				if (line.Contains("UNSATISFIABLE"))
				{
					Unsatisfiable = true;
					break;
				}
				if (line.StartsWith("-"))
				{
					break;
				}
				if (line.Split('=')[0].Contains("agents"))
				{
					var splittedSolutionString = line.Split('=')[1].Trim(new[] { ' ', '[', ']' });
					agentsData = Array.ConvertAll(splittedSolutionString.Split(','), s => int.Parse(s));
				}

				if (!line.Split('=')[0].Contains("workedOn"))
					continue;
				{
					var splittedSolutionString = line.Split('=')[1].Trim(new[] { ' ', '[', ']' });
					workedOnData = Array.ConvertAll(splittedSolutionString.Split(','), s => int.Parse(s));
				}
			}
			return new Tuple<int[], int[]>(agentsData, workedOnData);
		}
	}
}