// The MIT License (MIT)
//
// Copyright (c) 2014-2018, Institute for Software & Systems Engineering
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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;

	using SafetySharp.Modeling;
	using Modeling;

	using NUnit.Framework;

	public partial class SimulationTests
	{
        [Test, TestCaseSource(nameof(PerformanceMeasurementConfigurationsWithSeeds)), Timeout(2000000)]
        public void SimulateProfileBased(Model model, int seed)
        {
            model.Faults.SuppressActivations();
			var stInit = Stopwatch.StartNew();
            var profileBasedSimulator = new ProfileBasedSimulator(model);
			stInit.Stop();
			var stSim = Stopwatch.StartNew();
            profileBasedSimulator.Simulate(numberOfSteps: 1000, seed: seed);
			stSim.Stop();
			Console.WriteLine("Initialization time: " + stInit.Elapsed.TotalSeconds + "s");
			Console.WriteLine("Simulation time: " + stSim.Elapsed.TotalSeconds + "s");
		}

		[Test, TestCaseSource(nameof(PerformanceEvaluationConfigurations)), TestCaseSource(nameof(FaultTypeEvaluationConfigurations))]
		public void PerformanceEvaluation(Model model, int seed)
		{
			var testNameWithoutSeed = TestContext.CurrentContext.Test.Name.Replace($" #{seed:000}", "");
			var reportsDirectory = Path.Combine("performance-reports", testNameWithoutSeed);

			const int numberOfSteps = 1000;
			var timeLimit = TimeSpan.FromMinutes(45);

			// disable output
			var console = Console.Out;
			Console.SetOut(TextWriter.Null);
			var listeners = new TraceListener[Debug.Listeners.Count];
			Debug.Listeners.CopyTo(listeners, 0);
			Debug.Listeners.Clear();

			console.WriteLine($"Test {seed}");
			console.WriteLine("==================");

			var initializationStopwatch = Stopwatch.StartNew();
			console.WriteLine("Initializing...");

			using (var simulator = new ProfileBasedSimulator(model))
			{
				initializationStopwatch.Stop();
				console.WriteLine($"Initialization complete after {initializationStopwatch.Elapsed.TotalSeconds}s.");

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

				console.WriteLine("Simulating...");
				thread.Start();
				var completed = thread.Join(timeLimit);

				if (!completed)
				{
					thread.Abort();
					console.WriteLine("Test timed out.");
				}
				else if (exception != null)
					console.WriteLine($"Test failed with exception {exception.GetType().Name}: '{exception.Message}'.");
				else
				{
					console.WriteLine($"Test succeeded after {(report.SimulationEnd - report.SimulationStart).TotalSeconds}s of simulation.");
					WriteSimulationData(reportsDirectory, report);
				}
			}

			// cleanup
			Console.SetOut(console);
			Debug.Listeners.AddRange(listeners);
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
				globalWriter.WriteLine("Seed;Model;Steps;Start;End;Resource Throughput;Capability Throughput");
				globalWriter.WriteLine($"{report.Seed};{report.Model};{report.Steps};{report.SimulationStart.Ticks};{report.SimulationEnd.Ticks};{report.ResourceThroughput};{report.CapabilityThroughput}");
			}

			// write reconf data
			using (var reconfWriter = new StreamWriter(Path.Combine(subdirectory, "reconfigurations.csv")))
			{
				reconfWriter.WriteLine("Step;Duration;End (Ticks);Failed;InvolvedAgents;AffectedAgents;RemovedRoles;AddedRoles");

				foreach (var reconfiguration in report.Reconfigurations)
				{
					var involved = string.Join(" ", reconfiguration.ConfigUpdate.InvolvedAgents.Select(a => a.Id));
					var affected = string.Join(" ", reconfiguration.ConfigUpdate.AffectedAgents.Select(a => a.Id));

					// compute number of removed, added roles; exclude roles both removed and added to same agent (compare modulo locked)
					var addedRolesCount = 0;
					var removedRolesCount = 0;
					foreach (var agent in reconfiguration.ConfigUpdate.AffectedAgents)
					{
						var changes = reconfiguration.ConfigUpdate.GetChanges(agent);
						var recordedRemoved = changes.Item1;
						var recordedAdded = changes.Item2;

						removedRolesCount += recordedRemoved.Count(role1 => !recordedAdded.Any(role2 => role1.PreCondition == role2.PreCondition && role1.PostCondition == role2.PostCondition));
						addedRolesCount += recordedAdded.Count(role1 => !recordedRemoved.Any(role2 => role1.PreCondition == role2.PreCondition && role1.PostCondition == role2.PostCondition));
					}

					reconfWriter.WriteLine(
						$"{reconfiguration.Step};{reconfiguration.NanosecondsDuration};{reconfiguration.End.Ticks};{reconfiguration.ConfigUpdate.Failed};{involved};{affected};{removedRolesCount};{addedRolesCount}");
				}
			}

			// write agent-reconf data
			using (var agentWriter = new StreamWriter(Path.Combine(subdirectory, "agent-reconfigurations.csv")))
			{
				agentWriter.WriteLine("Step;Agent;Duration");
				foreach (var reconfiguration in report.AgentReconfigurations)
				{
					agentWriter.WriteLine($"{reconfiguration.Step};{reconfiguration.Agent};{reconfiguration.NanosecondsDuration}");
				}
			}
		}

		private const int NumRuns = 100;
		private static IEnumerable PerformanceEvaluationConfigurations()
		{
			return from seed in Enumerable.Range(1, NumRuns)
				   from modelSet in new[] {
					   Tuple.Create(SampleModels.CreatePerformanceEvaluationConfigurationsCentralized(), "Centralized"),
					   Tuple.Create(SampleModels.CreatePerformanceEvaluationConfigurationsCoalition(), "Coalition")
				   }
				   from model in modelSet.Item1
				   select new TestCaseData(model, seed).SetName($"{model.Name} ({modelSet.Item2}) #{seed:000}").SetCategory(modelSet.Item2).SetCategory(model.Name);
		}

		private static IEnumerable PerformanceMeasurementConfigurationsWithSeeds()
		{
			return from seed in Enumerable.Range(1, NumRuns)
				   from model in SampleModels.CreatePerformanceEvaluationConfigurationsCoalition()
				   let testCase = new TestCaseData(model, seed)
				   select testCase.SetName($"{model.Name} (Coalition) #{seed:000}");
		}

		private static IEnumerable FaultTypeEvaluationConfigurations()
		{
			var modelsAndConfigs = new[] {
				Tuple.Create<Func<Model>, string>(SampleModels.CreateFaultKindComparisonModelCentralized, "Centralized"),
				Tuple.Create<Func<Model>, string>(SampleModels.CreateFaultKindComparisonModelCoalition, "Coalition")
			};
			var faultPredicates = new[] {
				Tuple.Create<Func<Model, IEnumerable<Fault>>, string>(ToolFaults, "tools"),
				Tuple.Create<Func<Model, IEnumerable<Fault>>, string>(m => m.RobotAgents.Select(a => a.Broken).Concat(m.CartAgents.Select(a => a.Broken)), "agents"),
				Tuple.Create<Func<Model, IEnumerable<Fault>>, string>(m => m.RobotAgents.Select(a => a.ResourceTransportFault), "IO"),
				Tuple.Create<Func<Model, IEnumerable<Fault>>, string>(m => m.TolerableFaults(), "tolerable")
			};

			return from faultPredicate in faultPredicates
				   from modelAndConfig in modelsAndConfigs
				   from seed in Enumerable.Range(1, NumRuns)
				   let model = WithFaults(modelAndConfig.Item1.Invoke(), faultPredicate.Item1)
				   let config = modelAndConfig.Item2
				   select new TestCaseData(model, seed).SetName($"{faultPredicate.Item2} ({config}) #{seed:000}")
													   .SetCategory(config).SetCategory(faultPredicate.Item2);
		}

		private static Model WithFaults(Model model, Func<Model, IEnumerable<Fault>> faultSelector)
		{
			model.Faults.SuppressActivations();
			faultSelector(model).MakeNondeterministic();
			return model;
		}

		private static IEnumerable<Fault> ToolFaults(Model model)
		{
			return from robot in model.RobotAgents
				   from fault in new[]
				   {
					   robot.DrillBroken, robot.InsertBroken, robot.PolishBroken, robot.TightenBroken, robot.GenericABroken, robot.GenericBBroken,
					   robot.GenericCBroken, robot.GenericDBroken, robot.GenericEBroken, robot.GenericFBroken, robot.GenericGBroken, robot.GenericHBroken,
					   robot.GenericIBroken, robot.GenericJBroken, robot.GenericKBroken, robot.GenericLBroken, robot.GenericMBroken, robot.GenericNBroken,
					   robot.GenericOBroken, robot.GenericPBroken
				   }
				   select fault;
		}
    }
}