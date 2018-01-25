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
	using Newtonsoft.Json;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using System.IO;

	class HazardProbabilityTests
	{
		public class ModelProbabilities
		{
#pragma warning disable 0169
#pragma warning disable 0649
			public double LightBarrierFalseDetection;
			public double LightBarrierMisdetection;
			public double OverheadDetectorFalseDetection;
			public double OverheadDetectorMisdetection;
			public double SmallLightBarrierFalseDetection;
			public double SmallLightBarrierMisdetection;
			public double LeftHV;
			public double LeftOHV;
			public double SlowTraffic;
#pragma warning restore 0169
#pragma warning restore 0649
		}

		public static void SetProbabilities(Model model)
		{
			var probabilities = JsonConvert.DeserializeObject<ModelProbabilities>(System.IO.File.ReadAllText("Analysis/heightcontrol_probabilities.json"));

			foreach (var detector in model.Components.OfType<LightBarrier>())
			{
				detector.FalseDetection.ProbabilityOfOccurrence = new Probability(probabilities.LightBarrierFalseDetection);
				detector.Misdetection.ProbabilityOfOccurrence = new Probability(probabilities.LightBarrierMisdetection);
			}

			foreach (var detector in model.Components.OfType<OverheadDetector>())
			{
				detector.FalseDetection.ProbabilityOfOccurrence = new Probability(probabilities.OverheadDetectorFalseDetection);
				detector.Misdetection.ProbabilityOfOccurrence = new Probability(probabilities.OverheadDetectorMisdetection);
			}
			foreach (var detector in model.Components.OfType<SmallLightBarrier>())
			{
				detector.FalseDetection.ProbabilityOfOccurrence = new Probability(probabilities.SmallLightBarrierFalseDetection);
				detector.Misdetection.ProbabilityOfOccurrence = new Probability(probabilities.SmallLightBarrierMisdetection);
			}

			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = new Probability(probabilities.LeftHV);
			model.VehicleSet.LeftOHV.ProbabilityOfOccurrence = new Probability(probabilities.LeftOHV);
			model.VehicleSet.SlowTraffic.ProbabilityOfOccurrence = new Probability(probabilities.SlowTraffic);
		}

		[Test]
		public void CalculateCollisionInOriginalDesign()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Collision, 50);
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateFalseAlarmInOriginalDesign()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.FalseAlarm, 50);
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculatePreventionInOriginalDesign()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.PreventedCollision, 50);
			Console.Write($"Probability of prevention: {result}");
		}


		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("CollisionProbability")]
		public void CalculateCollision(Model model, string variantName)
		{
			SetProbabilities(model);
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Collision, 50);
			Console.Write($"Probability of hazard: {result}");
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("FalseAlarmProbability")]
		public void CalculateFalseAlarm(Model model, string variantName)
		{
			SetProbabilities(model);
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.FalseAlarm, 50);
			Console.Write($"Probability of hazard: {result}");
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("PreventionProbability")]
		public void CalculatePrevention(Model model, string variantName)
		{
			SetProbabilities(model);
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.PreventedCollision, 50);
			Console.Write($"Probability of prevention: {result}");
		}


		[Test]
		public void ParametricLbInOriginalDesign()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			Action<double> updateParameterInModel = value =>
			{
				foreach (var detector in model.Components.OfType<LightBarrier>())
				{
					detector.FalseDetection.ProbabilityOfOccurrence = new Probability(value);
				}
			};

			var parameter = new QuantitativeParametricAnalysisParameter
			{
				StateFormula = model.Collision,
				Bound = 50,
				From = 000001,
				To = 0.01,
				Steps = 25,
				UpdateParameterInModel = updateParameterInModel
			};

			var result = SafetySharpModelChecker.ConductQuantitativeParametricAnalysis(model, parameter);
			var fileWriter = new StreamWriter("ParametricLbCollision.csv", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();

			parameter.StateFormula = model.FalseAlarm;
			result = SafetySharpModelChecker.ConductQuantitativeParametricAnalysis(model, parameter);
			fileWriter = new StreamWriter("ParametricLbFalseAlarm.csv", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();
			
			parameter.StateFormula = model.PreventedCollision;
			result = SafetySharpModelChecker.ConductQuantitativeParametricAnalysis(model, parameter);
			fileWriter = new StreamWriter("ParametricLbPreventedCollision.csv", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();
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
