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
using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
using ISSE.SafetyChecking.Modeling;
using NUnit.Framework;
using System.Linq;

namespace SafetySharp.CaseStudies.SmallModels.DegradedMode
{
	using Analysis;
	using Bayesian;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using ModelChecking;
	using Runtime;
	using LtmdpModelChecker = ISSE.SafetyChecking.LtmdpModelChecker;

	public class EvaluationTests
	{
		[Test]
		public void CalculateHazardProbability()
		{
			var tc = SafetySharpModelChecker.TraversalConfiguration;
			tc.WriteGraphvizModels = true;
			tc.EnableStaticPruningOptimization = false;
			SafetySharpModelChecker.TraversalConfiguration = tc;

			var model = new DegradedModeModel();
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.System.HazardActive, 50);

			Console.WriteLine($"Probability of hazard in model: {result}");
		}

		[Test]
		public void CreateMarkovChainWithFalseFormula()
		{
			var model = new DegradedModeModel();

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(new ExecutableStateFormula(() => false));
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazards()
		{
			var model = new DegradedModeModel();

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardsWithoutStaticPruning()
		{
			var model = new DegradedModeModel();

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardRetraversal1()
		{
			var model = new DegradedModeModel();

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck( model.System.HazardActive);
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = true;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}

		[Test]
		public void CreateMarkovChainWithHazardsRetraversal2()
		{
			var model = new DegradedModeModel();

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

			var retraversalMarkovChainGenerator = new MarkovChainFromMarkovChainGenerator(markovChain);
			retraversalMarkovChainGenerator.Configuration.SuccessorCapacity *= 2;
			retraversalMarkovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			retraversalMarkovChainGenerator.Configuration.UseCompactStateStorage = true;
			retraversalMarkovChainGenerator.Configuration.UseAtomarPropositionsAsStateLabels = false;
			retraversalMarkovChainGenerator.GenerateLabeledMarkovChain();
		}


		[Test]
		public void CreateMarkovChainWithHazardFaultsInState()
		{
			var model = new DegradedModeModel();

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
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
			var model = new DegradedModeModel();

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
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
			var model = new DegradedModeModel();
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = 1;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.System.HazardActive, 50);
			SafetySharpModelChecker.TraversalConfiguration.CpuCount = Int32.MaxValue;
			Console.Write($"Probability of hazard: {result}");
		}



		[Test]
		public void CalculateHazardWithoutEarlyTermination()
		{
			var model = new DegradedModeModel();
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = false;
			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.System.HazardActive, 50);
			SafetySharpModelChecker.TraversalConfiguration.EnableEarlyTermination = true;
			Console.Write($"Probability of hazard: {result}");
		}


		[Test]
		public void CalculateLtmdpWithoutFaultsWithPruning()
		{
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = true;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var markovChain = markovChainGenerator.GenerateLabeledTransitionMarkovDecisionProcess();
		}

		[Test]
		public void CalculateLtmdpWithoutStaticPruning()
		{
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuiltInLtmdp;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
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
		public void CalculateMdpNewStates()
		{
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
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
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
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
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
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
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, false);
		}

		[Test]
		public void CalculateMdpNewStatesConstantWithoutFaults()
		{
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp, markovChainGenerator.Configuration.DefaultTraceOutput, true);
		}


		[Test]
		public void CalculateMdpFlattenedWithoutFaults()
		{
			var model = new DegradedModeModel();
			model.System.SignalDetector1.F1.ProbabilityOfOccurrence = null;

			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			var markovChainGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel) { Configuration = SafetySharpModelChecker.TraversalConfiguration };
			markovChainGenerator.Configuration.SuccessorCapacity *= 2;
			markovChainGenerator.Configuration.EnableStaticPruningOptimization = false;
			markovChainGenerator.Configuration.LtmdpModelChecker = LtmdpModelChecker.BuildInMdpWithNewStates;
			markovChainGenerator.AddFormulaToCheck(model.System.HazardActive);
			markovChainGenerator.Configuration.UseCompactStateStorage = true;
			markovChainGenerator.Configuration.EnableEarlyTermination = false;
			var nmdp = markovChainGenerator.GenerateNestedMarkovDecisionProcess();

			var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
		}
	}
}