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
	using System.Linq;
	using ModelChecking;
	using Modeling;
	using Modeling.Controllers;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.Heuristics;

	internal class FunctionalTests
	{
		[TestCaseSource(nameof(CreateConfigurationsFast))]
		public void ReconfigurationFailed(Model model)
		{
			Dcca(model);
		}

		[Test]
		public void DepthFirstSearch()
		{
			var model = new Model();
			model.InitializeDefaultInstance();
			model.CreateObserverController<MiniZincObserverController>();
			model.SetAnalysisMode(AnalysisMode.TolerableFaults);

			var modelChecker = new SafetySharpQualitativeChecker { Configuration = { CpuCount = 1, StateCapacity = 1 << 20 } };
			var result = modelChecker.CheckInvariant(model, true);

			Console.WriteLine(result);
		}

		[TestCaseSource(nameof(CreateConfigurationsMiniZinc))]
		public void Evaluation(Model model)
		{
			Dcca(model);
		}

		private static void Dcca(Model model)
		{
			var safetyAnalysis = new SafetySharpSafetyAnalysis
			{
				Configuration =
				{
					CpuCount = 1,
					StateCapacity = 1 << 20,
					GenerateCounterExample = false
				},
				FaultActivationBehavior = FaultActivationBehavior.ForceOnly,
				Heuristics = { RedundancyHeuristic(model), new SubsumptionHeuristic(model.Faults) }
			};
		
			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, model.ObserverController.ReconfigurationState == ReconfStates.Failed);
			Console.WriteLine(result);
		}

		private static IFaultSetHeuristic RedundancyHeuristic(Model model)
		{
			return new MinimalRedundancyHeuristic(
				model.Faults,
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Drill).Select(t => t.Broken)),
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Insert).Select(t => t.Broken)),
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Tighten).Select(t => t.Broken)),
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Polish).Select(t => t.Broken)));
		}

		private static IEnumerable CreateConfigurationsMiniZinc()
		{
			return Model.CreateConfigurations<MiniZincObserverController>(AnalysisMode.TolerableFaults)
						.Select(model => new TestCaseData(model).SetName(model.Name));
		}

		private static IEnumerable CreateConfigurationsFast()
		{
			return Model.CreateConfigurations<FastObserverController>(AnalysisMode.TolerableFaults)
						.Select(model => new TestCaseData(model).SetName(model.Name));
		}
	}
}