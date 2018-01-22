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

namespace SafetySharp.CaseStudies.RailroadCrossing.Analysis
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
	using Runtime;
	using LtmdpModelChecker = ISSE.SafetyChecking.LtmdpModelChecker;

	class EvaluationTests
	{

		public static void SetProbabilities(Model model)
		{
			model.Channel.MessageDropped.ProbabilityOfOccurrence = new Probability(0.0001);
			model.CrossingController.Motor.BarrierMotorStuck.ProbabilityOfOccurrence = new Probability(0.001);
			model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(0.00003);
			model.CrossingController.TrainSensor.ErroneousTrainDetection.ProbabilityOfOccurrence = new Probability(0.0002);
			model.TrainController.Brakes.BrakesFailure.ProbabilityOfOccurrence = new Probability(0.00002);
			model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(0.02);
			model.TrainController.Odometer.OdometerSpeedOffset.ProbabilityOfOccurrence = new Probability(0.02);
		}
		
		[Test]
		public void CreateMarkovChainWithFalseFormula()
		{
			var model = new Model();
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
		public void CreateMarkovChainWithHazards()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardsWithoutStaticPruning()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardRetraversal1()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();
			
			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardsRetraversal2()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}


		[Test]
		public void CreateMarkovChainWithHazardFaultsInState()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
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
		public void CreateFaultAwareMarkovChainAllFaults()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CalculateHazardSingleCore()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			SafetySharpModelChecker.TraversalConfiguration.EnableStaticPruningOptimization = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.PossibleCollision, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			SafetySharpModelChecker.TraversalConfiguration.EnableStaticPruningOptimization = true;
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateHazardSingleCoreAllFaults()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			var formulaToCheck = new BoundedUnaryFormula(model.PossibleCollision, UnaryOperator.Finally, 10);
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();


			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(formulaToCheck);
				Console.Write($"Probability of formulaToCheck: {result}");
			}
		}



		[Test]
		public void CalculateHazardWithoutEarlyTermination()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.PossibleCollision, 50);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateLtmdpWithoutFaultsWithPruning()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = true;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
		}

		[Test]
		public void CalculateLtmdpWithoutStaticPruning()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
		}

		[Test]
		public void CalculateLtmdpWithoutStaticPruningSingleCore()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			var formulaToCheck = new BoundedUnaryFormula(model.PossibleCollision, UnaryOperator.Finally, 10);
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();


			using (var modelChecker = new ConfigurationDependentLtmdpModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbabilityRange(formulaToCheck);
				Console.Write($"Probability of formulaToCheck: {result}");
			}
		}

		[Test]
		public void CalculateMdpNewStates()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
		}

		[Test]
		public void CalculateMdpNewStatesConstant()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
		}


		[Test]
		public void CalculateMdpFlattened()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence=null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
		}


		[Test]
		public void CalculateMdpNewStatesWithoutFaults()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
		}

		[Test]
		public void CalculateMdpNewStatesConstantWithoutFaults()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
		}


		[Test]
		public void CalculateMdpFlattenedWithoutFaults()
		{
			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.ModelCapacity = new ModelCapacityByModelSize(3300000L, 1000000000L);
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.PossibleCollision);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
		}
	}
}
