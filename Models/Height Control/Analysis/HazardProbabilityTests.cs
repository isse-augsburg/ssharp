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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HeightControl.Analysis
{
	using FluentAssertions;
	using ISSE.SafetyChecking.Modeling;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using System.Collections;
	using Modeling.Controllers;
	using Modeling.Sensors;

	class HazardProbabilityTests
	{
		public static void SetProbabilities(Model model)
		{
			foreach (var detector in model.Components.OfType<LightBarrier>())
			{
				detector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);
				detector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			}

			foreach (var detector in model.Components.OfType<OverheadDetector>())
			{
				detector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);
				detector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			}
			foreach (var detector in model.Components.OfType<SmallLightBarrier>())
			{
				detector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);
				detector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			}
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = new Probability(0.01);
			model.VehicleSet.LeftOHV.ProbabilityOfOccurrence = new Probability(0.001);
			model.VehicleSet.SlowTraffic.ProbabilityOfOccurrence = new Probability(0.1);
		}

		[Test]
		public void CalculateCollisionInOriginalDesign()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.Collision);
			Console.Write($"Probability of hazard: {result.Value}");
		}

		[Test]
		public void CalculateFalseAlarmInOriginalDesign()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.FalseAlarm);
			Console.Write($"Probability of hazard: {result.Value}");
		}


		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("CollisionProbability")]
		public void CalculateCollision(Model model, string variantName)
		{
			SetProbabilities(model);
			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.Collision);
			Console.Write($"Probability of hazard: {result.Value}");
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("FalseAlarmProbability")]
		public void CalculateFalseAlarm(Model model, string variantName)
		{
			SetProbabilities(model);
			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.FalseAlarm);
			Console.Write($"Probability of hazard: {result.Value}");
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
