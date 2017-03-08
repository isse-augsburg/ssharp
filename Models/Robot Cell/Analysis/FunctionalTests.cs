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
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ModelChecking;
	using Modeling;
	using Modeling.Controllers;
	using NUnit.Framework;

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

			var modelChecker = new SafetySharpQualitativeChecker { Configuration = { CpuCount = 1, ModelCapacity = new ModelCapacityByModelDensity(1 << 20, ModelDensityLimit.Medium) } };
			var result = modelChecker.CheckInvariant(model, true);

			Console.WriteLine(result);
		}

		[TestCaseSource(nameof(CreateConfigurationsMiniZinc))]
		public void Evaluation(Model model)
		{
			Dcca(model);
		}

		[Test,TestCaseSource(nameof(CreateConfigurationsFast)), Category("TestEvaluationFast")]
		public void EvaluationFastController(Model model)
		{
			Dcca(model);
		}

		[Test, TestCaseSource(nameof(CreateConfigurationsFast)), Category("TestEvaluationFast")]
		public void EvaluationFastControllerNoHeuristics(Model model)
		{
			Dcca(model, enableHeuristics: false);
		}

		[Test,TestCaseSource(nameof(CreateConfigurationsFast)), Category("TestEvaluationFast")]
		public void DepthFirstSearchFastController(Model model)
		{
			var modelChecker = new SafetySharpQualitativeChecker { Configuration = { CpuCount = 1, ModelCapacity = new ModelCapacityByModelDensity(1 << 20, ModelDensityLimit.Medium) } };
			var result = modelChecker.CheckInvariant(model, true);

			Console.WriteLine(result);
		}

		private static void Dcca(Model model, bool enableHeuristics = true)
		{
			var safetyAnalysis = new SafetySharpSafetyAnalysis
			{
				Configuration =
				{
					CpuCount = 1,
					ModelCapacity = new ModelCapacityByModelDensity(1 << 20, ModelDensityLimit.Medium),
					GenerateCounterExample = false
				},
				FaultActivationBehavior = FaultActivationBehavior.ForceOnly
			};

			if (enableHeuristics)
			{
				safetyAnalysis.Heuristics.Add(RedundancyHeuristic(model));
				safetyAnalysis.Heuristics.Add(new SubsumptionHeuristic(model.Faults));
			}
		
			var result = safetyAnalysis.ComputeMinimalCriticalSets(model, model.ObserverController.ReconfigurationState == ReconfStates.Failed);
			Console.WriteLine(result);
		}

		private static IFaultSetHeuristic RedundancyHeuristic(Model model)
		{
			var cartFaults = model.Carts.Select(cart => cart.Broken)
				.Concat(model.Carts.Select(cart => cart.Lost))
				.Concat(model.CartAgents.Select(cartAgent => cartAgent.ConfigurationUpdateFailed));

			return new MinimalRedundancyHeuristic(
				model.Faults.Except(cartFaults).ToArray(),
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Drill).Select(t => t.Broken)),
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Insert).Select(t => t.Broken)),
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Tighten).Select(t => t.Broken)),
				model.Robots.SelectMany(d => d.Tools.Where(t => t.Capability.ProductionAction == ProductionAction.Polish).Select(t => t.Broken)));
		}

		private static IEnumerable CreateConfigurationsMiniZinc()
		{
			return Model.CreateConfigurations<MiniZincObserverController>(AnalysisMode.TolerableFaults)
						.Select(model => new TestCaseData(model).SetName(model.Name + " (MiniZinc)"));
		}

		private static IEnumerable CreateConfigurationsFast()
		{
			return Model.CreateConfigurations<FastObserverController>(AnalysisMode.TolerableFaults)
						.Select(model => new TestCaseData(model).SetName(model.Name + " (Fast)"));
		}
	}
}