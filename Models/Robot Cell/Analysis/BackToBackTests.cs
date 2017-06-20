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

namespace SafetySharp.CaseStudies.RobotCell.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ModelChecking;
	using Runtime;

	using Modeling;
	using Modeling.Controllers;

	using NUnit.Framework;

	[TestFixture]
	internal class BackToBackTests : DccaTestsBase
	{
		[Category("Back2BackTestingSlow")]
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void DepthFirstSearch(Model model)
		{
			Dcca(model,
				hazard: false,
				enableHeuristics: false,
				mode: "depth-first");
		}

		[Category("Back2BackTestingDcca")]
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void DccaOnly(Model model)
		{
			Dcca(model,
				hazard: model.ObserverController.ReconfigurationState == ReconfStates.Failed,
				enableHeuristics: false,
				mode: "dcca");
		}

		[Category("Back2BackTestingHeuristics")]
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void DccaWithHeuristics(Model model)
		{
			Dcca(model,
				hazard: model.ObserverController.ReconfigurationState == ReconfStates.Failed,
				enableHeuristics: true,
				mode: "heuristics");
		}

		[Category("Back2BackTestingDccaOracle")]
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void OracleDccaOnly(Model model)
		{
			Dcca(model,
				hazard: model.ObserverController.OracleState == ReconfStates.Failed,
				enableHeuristics: false,
				mode: "dcca-oracle");
		}

		[Category("Back2BackTestingHeuristicsOracle")]
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void OracleDccaWithHeuristics(Model model)
		{
			Dcca(model,
				hazard: model.ObserverController.OracleState == ReconfStates.Failed,
				enableHeuristics: true,
				mode: "heuristics-oracle");
		}

		[Test]
		// run without errors
		public void BoundaryClosenessBaseline()
		{
			var model = Model.CreateConfiguration<FastObserverController>(m => m.Ictss1(), "Ictss1", AnalysisMode.TolerableFaults);
			var result = Dcca(model,
				hazard: model.ObserverController.OracleState == ReconfStates.Failed,
				enableHeuristics: true,
				stopOnFirstException: false);
			Assert.AreEqual(0, result.Exceptions.Count);

			Console.WriteLine(string.Join(",", result.MinimalCriticalSets.Select(s => new FaultSet(s)._faults)));
		}

		[Test, Category("Back2BackTestingBoundary")]
		public void BoundaryCloseness()
		{
			var minimalCritical =
				new[]
					{
						// determined by above test
						1024, 4096, 1048576, 4194304, 134217728, 536870912, 49152, 147456, 278528, 41943040, 1090519040, 70866960384, 277025390592,
						551903297536, 2201170739200, 8798240505856, 2207613190144, 8804682956800, 67149824, 67248128, 67641344, 103079231488, 94489280512,
						300647710720, 2284922601472, 8881992368128, 2491081031680, 9088150798336, 103146332160, 858993475584, 575525650432, 2765958971392,
						9363028738048, 575525748736, 2765959069696, 9363028836352, 609885356032, 2800318676992, 9397388443648, 859060576256
					}
					.Select(v => new FaultSet(v))
					.ToArray();

			var model = Model.CreateConfiguration<FastObserverController>(m => m.Ictss1(), "Ictss1", AnalysisMode.TolerableFaults);
			var result = Dcca(model,
				hazard: model.ObserverController.OracleState == ReconfStates.Failed,
				enableHeuristics: false,
				stopOnFirstException: false);

			var belowBoundary = 0;
			var aboveBoundary = 0;
			foreach (var testCase in result.Exceptions.Keys)
			{
				var t = new FaultSet(testCase);
				if (IsAboveBoundary(t, model, minimalCritical))
					aboveBoundary++;
				else if (IsBelowBoundary(t, model, minimalCritical))
					belowBoundary++;
			}

			Console.WriteLine("Fault: " + GetCurrentFault());
			Console.WriteLine("Failed tests: " + result.Exceptions.Count);
			Console.WriteLine("below boundary: " + belowBoundary);
			Console.WriteLine("above boundary: " + aboveBoundary);
		}

		private bool IsAboveBoundary(FaultSet set, Model model, IEnumerable<FaultSet> minCrit)
		{
			var subsets = model.Faults.Where(set.Contains).Select(set.Remove);
			return IsCritical(set, minCrit) && subsets.Any(s => !IsCritical(s, minCrit));
		}

		private bool IsBelowBoundary(FaultSet set, Model model, IEnumerable<FaultSet> minCrit)
		{
			var supersets = model.Faults.Where(f => !set.Contains(f)).Select(set.Add);
			return !IsCritical(set, minCrit) && supersets.Any(s => IsCritical(s, minCrit));
		}

		private bool IsCritical(FaultSet set, IEnumerable<FaultSet> minCrit)
		{
			return minCrit.Any(m => m.IsSubsetOf(set));
		}

		private void Dcca(Model model, Formula hazard, bool enableHeuristics, string mode)
		{
			var result = Dcca(model, hazard, enableHeuristics, true);
			Console.WriteLine(result);

			LogResult(model, result, mode);
		}

		private SafetyAnalysisResults<SafetySharpRuntimeModel> Dcca(Model model, Formula hazard, bool enableHeuristics, bool stopOnFirstException)
		{
			var safetyAnalysis = new SafetySharpSafetyAnalysis
			{
				Configuration =
				{
					CpuCount = 4,
					ModelCapacity = new ModelCapacityByModelDensity(1 << 20, ModelDensityLimit.Medium),
					GenerateCounterExample = false
				},
				FaultActivationBehavior = FaultActivationBehavior.ForceOnly,
				StopOnFirstException = stopOnFirstException
			};

			if (enableHeuristics)
			{
				safetyAnalysis.Heuristics.Add(RedundancyHeuristic(model));
				safetyAnalysis.Heuristics.Add(new SubsumptionHeuristic(model.Faults));
			}

			return safetyAnalysis.ComputeMinimalCriticalSets(model, hazard);
		}

		private StreamWriter _csv;

		[TestFixtureSetUp]
		public void Setup()
		{
			var file = File.Open("evaluation_results.csv", FileMode.Append, FileAccess.Write, FileShare.Read);
			_csv = new StreamWriter(file);
		}

		private void LogResult(Model model, SafetyAnalysisResults<SafetySharpRuntimeModel> result, string mode)
		{
			var faultCount = result.Faults.Count() - result.SuppressedFaults.Count();
			var cardinalitySum = result.MinimalCriticalSets.Sum(set => set.Count);
			var minimalSetCardinalityAverage = cardinalitySum == 0 ? -1 : cardinalitySum / (double)result.MinimalCriticalSets.Count;
			var minimalSetCardinalityMinimum = result.MinimalCriticalSets.Count == 0 ? -1 : result.MinimalCriticalSets.Min(set => set.Count);
			var minimalSetCardinalityMaximum = result.MinimalCriticalSets.Count == 0 ? -1 : result.MinimalCriticalSets.Max(set => set.Count);

			var exception = result.Exceptions.Values.FirstOrDefault();
			var exceptionText = exception == null ? null : exception.GetType().Name + " (" + exception.Message + ")";

			object[] columns = {
				GetCurrentFault(),										// tested fault
				mode,													// testing mode
				model.Name,												// model name
				exceptionText,											// thrown exception (if any)
				faultCount,												// # faults
				(int)result.Time.TotalMilliseconds,						// required time
				result.CheckedSetCount,									// # checked sets
				result.CheckedSetCount * 100.0 / (1L << faultCount),	// % checked sets
				result.TrivialChecksCount,								// # trivial checks
				result.HeuristicSuggestionCount,						// # suggestions
				result.HeuristicNonTrivialSafeCount * 100.0				// % good suggestions
					/ result.HeuristicSuggestionCount,
				(result.HeuristicSuggestionCount						// % non-trivially critical (bad) suggestions
					- result.HeuristicTrivialCount - result.HeuristicNonTrivialSafeCount) * 100.0 / result.HeuristicSuggestionCount,
				result.MinimalCriticalSets.Count,						// # minimal-critical sets
				minimalSetCardinalityAverage,							// avg. cardinality of minimal-critical sets
				minimalSetCardinalityMinimum,							// min. cardinality of minimal-critical sets
				minimalSetCardinalityMaximum							// max. cardinality of minimal-critical sets
			};
			_csv.WriteLine(string.Join(",", columns));
			_csv.Flush();
		}

		private string GetCurrentFault()
		{
#if ENABLE_F1
			return "F1";
#elif ENABLE_F2
			return "F2";
#elif ENABLE_F4
			return "F4";
#elif ENABLE_F4b
			return "F4b";
#elif ENABLE_F5
			return "F5";
#elif ENABLE_F6
			return "F6";
#elif ENABLE_F7
			return "F7";
#else
			return "None";
#endif
		}

		[TestFixtureTearDown]
		public void Teardown()
		{
			_csv.Flush();
			_csv.Close();
		}
	}
}