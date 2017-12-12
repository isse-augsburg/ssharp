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
	using System.Collections;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Modeling;

	using NUnit.Framework;

	public partial class SimulationTests
	{
	    [Test]
	    public void TempTestSystemGeneratorTest()
	    {
	        var tsg = new TestSystemGenerator();
            var result = tsg.Generate(100, 10, 40);
	        ;
	    }

	    [Test, TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
        public void Simulate(Model model)
        {
            model.Faults.SuppressActivations();
            var simulator = new Simulator(model);
            PrintTrace(simulator, model, steps: 100);
		}

        [Test, TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
        public void SimulateProfileBased(Model model)
        {
            model.Faults.SuppressActivations();
            var profileBasedSimulator = new ProfileBasedSimulator(model);
            profileBasedSimulator.Simulate(numberOfSteps: 1000);
        }

		[Test, TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
		public void PerformanceEvaluation(Model model)
		{
			var console = Console.Out;
			Console.SetOut(TextWriter.Null);
			Debug.Listeners.Clear();

			const int simulationsPerModel = 100;
			const int numberOfSteps = 10000;
			const int timeLimitMs = 300000;

			var reportsDirectory = Path.Combine("performance-reports", TestContext.CurrentContext.Test.Name);

			var successful = 0;
			for (var seed = 0; successful <= simulationsPerModel; ++seed)
			{
				console.WriteLine($"Test {seed}");
				console.WriteLine("\tInitializing...");
				using (var simulator = new ProfileBasedSimulator(model))
				{
					Exception exception = null;
					ProfileBasedSimulator.SimulationReport report = null;

					var thread = new Thread(() =>
					{
						try
						{
							report = simulator.Simulate(numberOfSteps, seed);
						}
						catch (Exception e)
						{
							exception = e;
						}
					});

					console.WriteLine("\tSimulating...");
					thread.Start();
					var completed = thread.Join(timeLimitMs);

					if (!completed)
					{
						thread.Abort();
						console.WriteLine("\tTest timed out.");
					}
					else if (exception != null)
						console.WriteLine($"\tTest failed with exception {exception.GetType().Name}: '{exception.Message}'.");
					else
					{
						successful++;
						console.WriteLine($"\tTest succeeded after {(report.SimulationEnd - report.SimulationStart).TotalSeconds}s of simulation.");
						WriteSimulationData(reportsDirectory, report);
					}
				}
			}
		}

		private static void WriteSimulationData(string reportsDirectory, ProfileBasedSimulator.SimulationReport report)
		{
			if (!Directory.Exists(reportsDirectory))
				Directory.CreateDirectory(reportsDirectory);
			var subdirectory = Path.Combine(reportsDirectory, $"{report.Seed}_{report.Model}_{report.SimulationStart.Ticks}");
			Directory.CreateDirectory(subdirectory);

			// write global data
			using (var globalWriter = new StreamWriter(Path.Combine(subdirectory, "simulation.csv")))
			{
				globalWriter.WriteLine("Seed;Model;Steps;Start;End;Throughput");
				globalWriter.WriteLine($"{report.Seed};{report.Model};{report.Steps};{report.SimulationStart.Ticks};{report.SimulationEnd.Ticks};{report.Throughput}");
			}

			// write reconf data
			using (var reconfWriter = new StreamWriter(Path.Combine(subdirectory, "reconfigurations.csv")))
			{
				reconfWriter.WriteLine("Step;Duration;End;Failed;InvolvedAgents;AffectedAgents");
				foreach (var reconfiguration in report.Reconfigurations)
				{
					var involved = string.Join(" ", reconfiguration.ConfigUpdate.InvolvedAgents.Select(a => a.Id));
					var affected = string.Join(" ", reconfiguration.ConfigUpdate.AffectedAgents.Select(a => a.Id));
					reconfWriter.WriteLine(
						$"{reconfiguration.Step};{reconfiguration.Duration.Ticks};{reconfiguration.End.Ticks};{reconfiguration.ConfigUpdate.Failed};{involved};{affected}");
				}
			}

			// write agent-reconf data
			using (var agentWriter = new StreamWriter(Path.Combine(subdirectory, "agent-reconfigurations.csv")))
			{
				agentWriter.WriteLine("Step;Agent;Duration");
				foreach (var reconfiguration in report.AgentReconfigurations)
				{
					agentWriter.WriteLine($"{reconfiguration.Step};{reconfiguration.Agent};{reconfiguration.Duration.Ticks}");
				}
			}
		}

		private static IEnumerable PerformanceMeasurementConfigurations()
	    {
		    return SampleModels.CreatePerformanceEvaluationConfigurationsCentralized()
							   .Select(model => new TestCaseData(model).SetName(model.Name + " (Centralized)"))
							   .Concat(SampleModels.CreatePerformanceEvaluationConfigurationsCoalition()
												   .Select(model => new TestCaseData(model).SetName(model.Name + " (Coalition)")));
	    }

        private static void PrintTrace(Simulator simulator, Model model, int steps)
		{
			
			for (var i = 0; i < steps; ++i)
			{
				WriteLine($"=================  Step: {i}  =====================================");

				if (model.ReconfigurationMonitor.ReconfigurationFailure)
					WriteLine("Reconfiguration failed.");
				else
				{
					foreach (var robot in model.RobotAgents)
						WriteLine(robot);

					foreach (var cart in model.CartAgents)
						WriteLine(cart);

					foreach (var workpiece in model.Workpieces)
						WriteLine(workpiece);

					foreach (var robot in model.Robots)
						WriteLine(robot);

					foreach (var cart in model.Carts)
						WriteLine(cart);
				}

				simulator.SimulateStep();
			}
		}

		private static void WriteLine(object line)
		{
			Debug.WriteLine(line.ToString());
#if !DEBUG
			System.Console.WriteLine(line.ToString());
#endif
		}
        
    }
}