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
	using System.IO;
	using System.Linq;

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
		public void BreadthFirstSearch(Model model)
		{
			Dcca(model,
				hazard: false,
				enableHeuristics: false);
		}

		[Category("Back2BackTestingDcca")]
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void DccaOnly(Model model)
		{
			Dcca(model,
				hazard: model.ObserverController.ReconfigurationState == ReconfStates.Failed,
				enableHeuristics: false);
		}

		[Category("Back2BackTestingHeuristics")]
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void DccaWithHeuristics(Model model)
		{
			Dcca(model,
				hazard: model.ObserverController.ReconfigurationState == ReconfStates.Failed,
				enableHeuristics: true);
		}

		private void Dcca(Model model, Formula hazard, bool enableHeuristics)
		{
			var safetyAnalysis = new SafetySharpSafetyAnalysis
			{
				Configuration =
				{
					CpuCount = 4,
					ModelCapacity = new ModelCapacityByModelDensity(1 << 20, ModelDensityLimit.Medium),
					GenerateCounterExample = false
				}
			};

			if (enableHeuristics)
			{
				safetyAnalysis.Heuristics.Add(RedundancyHeuristic(model));
				safetyAnalysis.Heuristics.Add(new SubsumptionHeuristic(model.Faults));
			}

			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, hazard);
			Console.WriteLine(result);

			string mode = enableHeuristics ? "heuristics" : (hazard.ToString() == "false" ? "breadth-first" : "dcca");
			LogResult(model, result, mode);
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
			var minimalSetCardinalityAverage = cardinalitySum == 0 ? 0 : cardinalitySum / (double)result.MinimalCriticalSets.Count;

			object[] columns = {
				GetCurrentFault(),										// tested fault
				mode,													// testing mode
				model.Name,												// model name
				faultCount,												// # faults
				(int)result.Time.TotalMilliseconds,						// required time
				result.CheckedSets.Count,								// # checked sets
				result.CheckedSets.Count * 100.0 / (1L << faultCount),	// % checked sets
				result.TrivialChecksCount,								// # trivial checks
				result.HeuristicSuggestionCount,						// # suggestions
				result.HeuristicNonTrivialSafeCount * 100.0				// % good suggestions
					/ result.HeuristicSuggestionCount,
				(result.HeuristicSuggestionCount						// % non-trivially critical (bad) suggestions
					- result.HeuristicTrivialCount - result.HeuristicNonTrivialSafeCount) * 100.0 / result.HeuristicSuggestionCount,
				result.MinimalCriticalSets.Count,						// # minimal-critical sets
				minimalSetCardinalityAverage,							// avg. cardinality of minimal-critical sets
				result.MinimalCriticalSets.Min(set => set.Count),		// min. cardinality of minimal-critical sets
				result.MinimalCriticalSets.Max(set => set.Count)		// max. cardinality of minimal-critical sets
			};
			_csv.WriteLine(string.Join(",", columns));
		}

		private string GetCurrentFault()
		{
#if ENABLE_F1
			return "F1";
#elif ENABLE_F2
			return "F2";
#elif ENABLE_F4
			return "F4";
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