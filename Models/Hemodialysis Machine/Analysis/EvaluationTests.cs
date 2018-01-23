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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Analysis
{
	using FluentAssertions;
	using ISSE.SafetyChecking.Modeling;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using System.Collections;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using Runtime;
	using LtmdpModelChecker = ISSE.SafetyChecking.LtmdpModelChecker;

	class EvaluationTests
	{
		private static readonly Probability _prob1Eneg1 = new Probability(0.1);
		private static readonly Probability _prob1Eneg2 = new Probability(0.01);
		private static readonly Probability _prob1Eneg3 = new Probability(0.001);
		private static readonly Probability _prob1Eneg5 = new Probability(0.00001);
		private static readonly Probability _prob1Eneg7 = new Probability(0.0000001);

		public static void SetProbabilities(Model model)
		{

			model.HdMachine.DialyzingFluidDeliverySystem.DialyzingFluidPreparation.DialyzingFluidPreparationPumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.PumpDefect.ProbabilityOfOccurrence = _prob1Eneg3;
			model.HdMachine.DialyzingFluidDeliverySystem.SafetyBypass.SafetyBypassFault.ProbabilityOfOccurrence = _prob1Eneg3;
			model.HdMachine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefect.ProbabilityOfOccurrence = _prob1Eneg2;

			model.HdMachine.ExtracorporealBloodCircuit.ArterialBloodPump.BloodPumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.ExtracorporealBloodCircuit.VenousSafetyDetector.SafetyDetectorDefect.ProbabilityOfOccurrence = _prob1Eneg7;
			model.HdMachine.ExtracorporealBloodCircuit.VenousTubingValve.ValveDoesNotClose.ProbabilityOfOccurrence = _prob1Eneg5;

			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = _prob1Eneg5;
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
		public void CreateMarkovChainWithBothHazards()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithBothHazardsWithoutStaticPruning()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithBothHazardsRetraversal1()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();
			
			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithBothHazardsRetraversal2()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			retraversalMarkovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}


		[Test]
		public void CreateMarkovChainWithBothHazardsFaultsInState()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
				markovChainGenerator.AddFormulaToPlainlyIntegrateIntoStateSpace(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}


		[Test]
		public void CreateFaultAwareMarkovChain()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
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
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CalculateHazardSingleCore()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.CpuCount = 1;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var unsuccessful = new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6);
			var contamination = new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6);
			markovChainGenerator.AddFormulaToCheck(unsuccessful);
			markovChainGenerator.AddFormulaToCheck(contamination);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();


			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(unsuccessful);
				Console.Write($"Probability of collision: {result}");
			}

			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(contamination);
				Console.Write($"Probability of collision: {result}");
			}
		}
		
		[Test]
		public void CalculateHazardSingleCoreAllFaults()
		{
			var model = new Model();
			SetProbabilities(model);

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.CpuCount = 1;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			var unsuccessful = new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6);
			var contamination = new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6);
			markovChainGenerator.AddFormulaToCheck(unsuccessful);
			markovChainGenerator.AddFormulaToCheck(contamination);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();


			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(unsuccessful);
				Console.Write($"Probability of unsuccessful: {result}");
			}

			using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbability(contamination);
				Console.Write($"Probability of contamination: {result}");
			}
		}



		[Test]
		public void CalculateBloodNotCleanedAndDialyzingFinishedSingleCore()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.BloodNotCleanedAndDialyzingFinished, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateIncomingBloodWasNotOkSingleCore()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.IncomingBloodWasNotOk, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateBloodNotCleanedAndDialyzingFinishedWithoutEarlyTermination()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.BloodNotCleanedAndDialyzingFinished, 50);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateIncomingBloodWasNotOkWithoutEarlyTermination()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.IncomingBloodWasNotOk, 50);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}



		[Test]
		public void CalculateBloodNotCleanedAndDialyzingFinishedSingleCoreWithoutEarlyTermination()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.BloodNotCleanedAndDialyzingFinished, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateIncomingBloodWasNotOkSingleCoreWithoutEarlyTermination()
		{
			var model = new Model();

			SetProbabilities(model);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.IncomingBloodWasNotOk, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateLtmdpWithoutFaultsWithPruning()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = true;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
		}

		[Test]
		public void CalculateLtmdpWithoutStaticPruning()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
		}


		[Test]
		public void CalculateLtmdpWithoutStaticPruningSingleCore()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var unsuccessful = new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6);
			var contamination = new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6);
			markovChainGenerator.AddFormulaToCheck(unsuccessful);
			markovChainGenerator.AddFormulaToCheck(contamination);
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.CpuCount = 1;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();


			using (var modelChecker = new ConfigurationDependentLtmdpModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbabilityRange(unsuccessful);
				Console.Write($"Probability of unsuccessful: {result}");
			}

			using (var modelChecker = new ConfigurationDependentLtmdpModelChecker(markovChainGenerator.Configuration, markovChain, markovChainGenerator.Configuration.DefaultTraceOutput))
			{
				var result = modelChecker.CalculateProbabilityRange(contamination);
				Console.Write($"Probability of contamination: {result}");
			}
		}

		[Test]
		public void CalculateMdpNewStates()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
		}

		[Test]
		public void CalculateMdpNewStatesConstant()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
		}


		[Test]
		public void CalculateMdpFlattened()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			foreach (var fault in model.Faults)
			{
				var faultFormula = new FaultFormula(fault);
				markovChainGenerator.AddFormulaToCheck(faultFormula);
			}
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
		}

		[Test]
		public void CalculateMdpNewStatesWithoutFaults()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));

			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
		}

		[Test]
		public void CalculateMdpNewStatesConstantWithoutFaults()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));

			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
		}


		[Test]
		public void CalculateMdpFlattenedWithoutFaults()
		{
			var model = new Model();
			SetProbabilities(model);
			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.BloodNotCleanedAndDialyzingFinished, UnaryOperator.Finally, 6));
			markovChainGenerator.AddFormulaToCheck(new BoundedUnaryFormula(model.IncomingBloodWasNotOk, UnaryOperator.Finally, 6));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
		}
	}
}
