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
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using Modeling.Controllers;
	using Modeling.Sensors;
	using Newtonsoft.Json;
	using Runtime;
	using LtmdpModelChecker = ISSE.SafetyChecking.LtmdpModelChecker;

	class EvaluationTests
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
		public void CreateMarkovChainWithFalseFormula()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new ExecutableStateFormula(()=> false));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithBothHazards()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithBothHazardsWithoutStaticPruning()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithBothHazardsRetraversal1()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();
			
			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithBothHazardsRetraversal2()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}


		[Test]
		public void CreateMarkovChainWithBothHazardsFaultsInState()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
				markovChainGenerator.AddFormulaToPlainlyIntegrateIntoStateSpace(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}


		[Test]
		public void CreateFaultAwareMarkovChainLeftDetectorFalse()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(new FaultFormula(model.HeightControl.EndControl.LeftDetector.FalseDetection));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateFaultAwareMarkovChainLeftDetectorMis()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(new FaultFormula(model.HeightControl.EndControl.LeftDetector.Misdetection));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateFaultAwareMarkovChainPositionDetectorFalse()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(new FaultFormula(model.HeightControl.PreControl.PositionDetector.FalseDetection));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateFaultAwareMarkovChainPositionDetectorMis()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(new FaultFormula(model.HeightControl.PreControl.PositionDetector.Misdetection));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateFaultAwareMarkovChainTwoFaults()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.AddFormulaToCheck(new FaultFormula(model.HeightControl.EndControl.LeftDetector.FalseDetection));
			markovChainGenerator.AddFormulaToCheck(new FaultFormula(model.HeightControl.EndControl.LeftDetector.Misdetection));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateFaultAwareMarkovChainAllFaults()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CalculateHazardSingleCore()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.CpuCount = 1;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			var collision = new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50);
			var falseAlarm = new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50);
			markovChainGenerator.AddFormulaToCheck(collision);
			markovChainGenerator.AddFormulaToCheck(falseAlarm);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();
			markovChainGenerator.Configuration.CpuCount = Int32.MaxValue;

			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(collision);
				Console.Write($"Probability of collision: {result}");
			}

			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(falseAlarm);
				Console.Write($"Probability of falseAlarm: {result}");
			}
		}

		[Test]
		public void CalculateHazardSingleCoreAllFaults()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.CpuCount = 1;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}


		[Test]
		public void CalculateHazardSingleCorePositionDetectorMis()
		{
			var model = Model.CreateOriginal();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.CpuCount = 1;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.AddFormulaToCheck(new FaultFormula(model.HeightControl.PreControl.PositionDetector.Misdetection));
			var collision = new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50);
			var falseAlarm = new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50);
			markovChainGenerator.AddFormulaToCheck(collision);
			markovChainGenerator.AddFormulaToCheck(falseAlarm);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var ltmc = markovChainGenerator.GenerateLabeledMarkovChain();
			var markovChain = markovChainGenerator.GenerateMarkovChain();

			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, ltmc, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(collision);
				Console.Write($"Probability of collision: {result}");
			}

			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, ltmc, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(falseAlarm);
				Console.Write($"Probability of falseAlarm: {result}");
			}
		}

		[Test]
		public void CalculateCollisionSingleCore()
		{
			var model = Model.CreateOriginal();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Collision, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateFalseAlarmSingleCore()
		{
			var model = Model.CreateOriginal();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.FalseAlarm, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateCollisionWithoutEarlyTermination()
		{
			var model = Model.CreateOriginal();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Collision, 50);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateFalseAlarmWithoutEarlyTermination()
		{
			var model = Model.CreateOriginal();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.FalseAlarm, 50);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateCollisionSingleCoreWithoutEarlyTermination()
		{
			var model = Model.CreateOriginal();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.Collision, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateFalseAlarmSingleCoreWithoutEarlyTermination()
		{
			var model = Model.CreateOriginal();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.FalseAlarm, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateLtmdpWithoutFaultsWithPruning()
		{
			var model = Model.CreateOriginal();
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = true;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
		}

		[Test]
		public void CalculateLtmdpWithoutStaticPruning()
		{
			var model = Model.CreateOriginal();
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			/*
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			*/
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
		}

		[Test]
		public void CalculateLtmdpWithoutStaticPruningSingleCore()
		{
			var model = Model.CreateOriginal();
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			var collision = new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50);
			var falseAlarm = new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50);
			markovChainGenerator.AddFormulaToCheck(collision);
			markovChainGenerator.AddFormulaToCheck(falseAlarm);
			/*
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			*/
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.CpuCount = 1;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();

			using (var modelChecker = new ConfigurationDependentLtmdpModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbabilityRange(collision);
				Console.Write($"Probability of collision: {result}");
			}

			using (var modelChecker = new ConfigurationDependentLtmdpModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbabilityRange(falseAlarm);
				Console.Write($"Probability of falseAlarm: {result}");
			}
		}



		[Test]
		public void CalculateMdpNewStatesWithoutFaults()
		{
			var model = Model.CreateOriginal();
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
		}

		[Test]
		public void CalculateMdpNewStatesConstantWithoutFaults()
		{
			var model = Model.CreateOriginal();
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
		}


		[Test]
		public void CalculateMdpFlattenedWithoutFaults()
		{
			var model = Model.CreateOriginal();
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.Collision, UnaryOperator.Finally, 50));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.FalseAlarm, UnaryOperator.Finally, 50));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
		}
	}
}
