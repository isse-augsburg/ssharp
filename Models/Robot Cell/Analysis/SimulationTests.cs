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
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading;

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
            var result = tsg.Generate(100, 10, 40, new Random(42));
			Console.WriteLine("end")
	        ;
	    }

		[TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
		public void ComputeStateVectorLayout(Model model)
		{
			var modelChecker = new SSharpChecker { Configuration = { StateCapacity = 1 << 12 } };
			var result = modelChecker.CheckInvariant(model, false);

			Console.WriteLine(result.StateVectorLayout);
		}

		[Test, TestCaseSource(nameof(PerformanceMeasurementConfigurations))]
        public void Simulate(Model model)
        {
            model.Faults.SuppressActivations();
            var simulator = new Simulator(model);
            PrintTrace(simulator, model, steps: 100);
		}

		private static readonly int[] timeoutSeeds = { 0, 270310109, 270364453, 270442343, 270468015, 270544828, 270570265, 270673000, 272980687, 276191703, 276241156, 623786875, 624231578, 624556093, 624581281, 624706750, 624756593, 624807265, 624958734, 625362093, 625414296, 625466328, 625543906, 625983203, 626181156, 626521921, 626805109, 626918062, 627491484, 628411312, 629232765, 629526812, 630165046, 630505953, 630749781, 631411156, 631474984, 633358718, 635164156, 636060859, 636141031, 636552203, 637437437, 640059937, 640469234, 642011468, 643386421, 645328187, 647254250, 647507078, 648405250, 649056750, 650996734, 652048234, 655944359, 656312015, 657046593, 658462218, 660240546, 660777687, 662170750, 664218078, 665282468, 667382515, 669330796, 671611812, 672139562, 672319406, 673419296, 673627734, 673825968, 676020000, 676610375, 682097796, 682594968, 683098828, 689535390, 690608375, 693462953, 695463343, 697192656, 700195953, 706713031, 709886093, 710165234, 710719875, 711327828, 719939640, 721695937, 722428328, 729626875, 735689031, 736080500, 758092703, 758906531, 759617812, 762761984, 766407593, 768445796, 770160234, 774324000 };

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

		[Test, TestCaseSource(nameof(PerformanceEvaluationConfigurations))]
		public void PerformanceEvaluation(Model model, int seed)
		{
			const int numberOfSteps = 1000;
			var timeLimit = TimeSpan.FromMinutes(45);
			var reportsDirectory = Path.Combine("performance-reports", TestContext.CurrentContext.Test.Name);

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

			model.Faults.MakeNondeterministic();
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


		private static IEnumerable PerformanceEvaluationConfigurations()
		{
			const int numRuns = 100;
			return from modelSet in new[] {
					   Tuple.Create(SampleModels.CreatePerformanceEvaluationConfigurationsCentralized(), "Centralized"),
					   Tuple.Create(SampleModels.CreatePerformanceEvaluationConfigurationsCoalition(), "Coalition")
				   }
				   from model in modelSet.Item1
				   from seed in Enumerable.Range(1, numRuns)
				   select new TestCaseData(model, seed).SetName($"{model.Name} ({modelSet.Item2}) #{seed:000}").SetCategory(modelSet.Item2).SetCategory(model.Name);
		}

		private static IEnumerable PerformanceMeasurementConfigurations()
	    {
		    return SampleModels.CreatePerformanceEvaluationConfigurationsCentralized()
							   .Select(model => new TestCaseData(model).SetName(model.Name + " (Centralized)"))
							   .Concat(SampleModels.CreatePerformanceEvaluationConfigurationsCoalition()
												   .Select(model => new TestCaseData(model).SetName(model.Name + " (Coalition)")));
		}

		private static IEnumerable PerformanceMeasurementConfigurationsWithSeeds()
		{
			return (from model in SampleModels.CreatePerformanceEvaluationConfigurationsCoalition()
					from seed in timeoutSeeds
					let testCase = new TestCaseData(model, seed)
					select testCase.SetName(model.Name + " (Coalition) -- " + seed));
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