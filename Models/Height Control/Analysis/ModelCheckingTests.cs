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

namespace SafetySharp.CaseStudies.HeightControl.Analysis
{
	using System;
	using System.Collections;
	using System.Linq;
	using FluentAssertions;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using ModelChecking;
	using Modeling;
	using Modeling.Controllers;
	using NUnit.Framework;
	using SafetySharp.Analysis;

	public class ModelCheckingTests
	{
		[Test]
		public void EnumerateAllStatesOriginalDesign()
		{
			var model = Model.CreateOriginal();
			var result = SafetySharpModelChecker.CheckInvariant(model, true);

			result.FormulaHolds.Should().BeTrue();
		}

		[Test]
		public void StateGraphAllStatesOriginalDesign()
		{
			var model = Model.CreateOriginal();

			var result = SafetySharpModelChecker.CheckInvariants(model, true, false, true);
			result[0].FormulaHolds.Should().BeTrue();
			result[1].FormulaHolds.Should().BeFalse();
			result[2].FormulaHolds.Should().BeTrue();
		}

		[Test]
		public void CollisionOriginalDesign(
			[Values(SafetyAnalysisBackend.FaultOptimizedStateGraph, SafetyAnalysisBackend.FaultOptimizedOnTheFly)] SafetyAnalysisBackend backend)
		{
			var model = Model.CreateOriginal();

			// As collisions cannot occur without any overheight vehicles driving on the left lane, we 
			// force the activation of the LeftOHV fault to improve safety analysis times significantly
			model.VehicleSet.LeftOHV.Activation = Activation.Forced;

			var analysis = new SafetySharpSafetyAnalysis { Backend = backend, Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.Collision);
			result.SaveCounterExamples("counter examples/height control/dcca/collision/original");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}

		[Test]
		public void FalseAlarmOriginalDesign(
			[Values(SafetyAnalysisBackend.FaultOptimizedStateGraph, SafetyAnalysisBackend.FaultOptimizedOnTheFly)] SafetyAnalysisBackend backend)
		{
			var model = Model.CreateOriginal();
			var analysis = new SafetySharpSafetyAnalysis { Backend = backend, Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.FalseAlarm);
			result.SaveCounterExamples("counter examples/height control/dcca/false alarm/original");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		public void EnumerateAllStates(Model model, string variantName)
		{
			var result = SafetySharpModelChecker.CheckInvariant(model, true);
			result.FormulaHolds.Should().BeTrue();
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("CollisionDCCA")]
		public void Collision(Model model, string variantName)
		{
			// As collisions cannot occur without any overheight vehicles driving on the left lane, we 
			// force the activation of the LeftOHV fault to improve safety analysis times significantly
			model.VehicleSet.LeftOHV.Activation = Activation.Forced;

			var analysis = new SafetySharpSafetyAnalysis { Heuristics = { new MaximalSafeSetHeuristic(model.Faults, cardinalityLevel: 4) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.Collision);

			result.SaveCounterExamples($"counter examples/height control/dcca/collision/{variantName}");
			Console.WriteLine(result);
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("FalseAlarmDCCA")]
		public void FalseAlarm(Model model, string variantName)
		{
			var analysis = new SafetySharpSafetyAnalysis { Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };
			var result = analysis.ComputeMinimalCriticalSets(model, model.FalseAlarm);

			result.SaveCounterExamples($"counter examples/height control/dcca/false alarm/{variantName}");
			Console.WriteLine(result);
		}

		private static IEnumerable CreateModelVariants()
		{
			return from model in Model.CreateVariants()
				   let name = $"{model.HeightControl.PreControl.GetType().Name.Substring(nameof(PreControl).Length)}-" +
							  $"{model.HeightControl.MainControl.GetType().Name.Substring(nameof(MainControl).Length)}-" +
							  $"{model.HeightControl.EndControl.GetType().Name.Substring(nameof(EndControl).Length)}"
				   select new TestCaseData(model, name).SetName(name);
		}
	}
}