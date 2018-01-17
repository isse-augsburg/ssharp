// The MIT License (MIT)
//
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;

	public abstract class AbstractMiniZincConfigurationFinder : IConfigurationFinder
	{
		public static string MiniZinc = "minizinc.exe";
		private static int _counter;

		private readonly string _constraintsModel;

		protected AbstractMiniZincConfigurationFinder(string constraintsModel)
		{
			_constraintsModel = constraintsModel;
		}

		public Task<Tuple<BaseAgent[], BaseAgent[]>> Find(ISet<BaseAgent> availableAgents, ICapability[] capabilities)
		{
			var agents = availableAgents.ToArray();

			lock (MiniZinc)
			{
				var inputFile = CreateDataFile(capabilities, agents);
				var outputFile = ExecuteMiniZinc(inputFile);
				return Task.FromResult(ParseSolution(outputFile, agents));
			}
		}

		private string CreateDataFile(ICapability[] capabilities, BaseAgent[] availableAgents)
		{
			var inputFile = $"data{++_counter}.dzn";
			using (var writer = new StreamWriter(inputFile))
			{
				WriteInputData(capabilities, availableAgents, writer);
			}
			return inputFile;
		}

		protected abstract void WriteInputData(ICapability[] capabilities, BaseAgent[] availableAgents, StreamWriter writer);

		private string ExecuteMiniZinc(string inputFile)
		{
			var outputFile = $"output{_counter}.dzn";
			var startInfo = new ProcessStartInfo
			{
				FileName = MiniZinc,
				Arguments = $"-o {outputFile} {_constraintsModel} {inputFile}",
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
			return outputFile;
		}

		private static Tuple<BaseAgent[], BaseAgent[]> ParseSolution(string outputFile, BaseAgent[] availableAgents)
		{
			var lines = File.ReadAllLines(outputFile);
			if (lines[0].Contains("UNSATISFIABLE"))
				return null;

			var distribution = ParseList(lines[0]).Select(i => availableAgents[i]).ToArray();
			var resourceFlow = ParseList(lines[1]).Select(i => availableAgents[i]).ToArray();
			return Tuple.Create(distribution, resourceFlow);
		}

		private static IEnumerable<int> ParseList(string line)
		{
			var openBrace = line.IndexOf("[", StringComparison.Ordinal);
			var closeBrace = line.IndexOf("]", StringComparison.Ordinal);

			return line.Substring(openBrace + 1, closeBrace - openBrace - 1).Split(',')
					   .Select(n => int.Parse(n.Trim()) - 1);
		}
	}
}
