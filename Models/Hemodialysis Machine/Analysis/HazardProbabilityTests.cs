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
	using System.Collections;
	using System.Globalization;
	using System.IO;
	using FluentAssertions;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;
	using ModelChecking;
	using Modeling;
	using SafetySharp.Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;

	class HazardProbabilityTests
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

		public HazardProbabilityTests()
		{
			var tc = SafetySharpModelChecker.TraversalConfiguration;
			tc.AllowFaultsOnInitialTransitions = false;
			tc.MomentOfIndependentFaultActivation = MomentOfIndependentFaultActivation.AtStepBeginning;
			SafetySharpModelChecker.TraversalConfiguration = tc;
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("IncomingBloodIsContaminated")]
		public void IncomingBloodIsContaminated(Model model)
		{
			SetProbabilities(model);
			
			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.IncomingBloodWasNotOk);
			Console.Write($"Probability of hazard: {result}");
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("IncomingBloodIsContaminated_FaultTree")]
		public void IncomingBloodIsContaminated_FaultTree(Model model)
		{
			SetProbabilities(model);
			var analysis = new SafetySharpSafetyAnalysis { Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };

			var result = analysis.ComputeMinimalCriticalSets(model, model.IncomingBloodWasNotOk);
			var steps = 6;
			var reaProbability = 0.0;
			foreach (var mcs in result.MinimalCriticalSets)
			{
				var mcsProbability = 1.0;
				foreach (var fault in mcs)
				{
					var pFaultInOneStep = fault.ProbabilityOfOccurrence.Value.Value;
					var pFault = 1.0 - Math.Pow(1.0 - pFaultInOneStep, steps);
					mcsProbability *= pFault;
				}
				reaProbability += mcsProbability;
			}

			Console.WriteLine($"Result with fault tree rare event approximation: {reaProbability.ToString(CultureInfo.InvariantCulture)}");
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("DialysisFinishedAndBloodNotCleaned")]
		public void DialysisFinishedAndBloodNotCleaned(Model model)
		{
			SetProbabilities(model);

			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.BloodNotCleanedAndDialyzingFinished);
			Console.Write($"Probability of hazard: {result}");
		}


		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("DialysisFinishedAndBloodNotCleaned_FaultTree")]
		public void DialysisFinishedAndBloodNotCleaned_FaultTree(Model model)
		{
			SetProbabilities(model);
			var analysis = new SafetySharpSafetyAnalysis { Heuristics = { new MaximalSafeSetHeuristic(model.Faults) } };

			var result = analysis.ComputeMinimalCriticalSets(model, model.BloodNotCleanedAndDialyzingFinished);
			var steps = 6;
			var reaProbability = 0.0;
			foreach (var mcs in result.MinimalCriticalSets)
			{
				var mcsProbability = 1.0;
				foreach (var fault in mcs)
				{
					var pFaultInOneStep = fault.ProbabilityOfOccurrence.Value.Value;
					var pFault = 1.0 - Math.Pow(1.0 - pFaultInOneStep, steps);
					mcsProbability *= pFault;
				}
				reaProbability += mcsProbability;
			}

			Console.WriteLine($"Result with fault tree rare event approximation: {reaProbability.ToString(CultureInfo.InvariantCulture)}");
		}

		[TestCaseSource(nameof(CreateModelVariants))]
		[Category("Parametric")]
		public void Parametric(Model model)
		{
			SetProbabilities(model);
			var parameter = new QuantitativeParametricAnalysisParameter
			{
				StateFormula = model.IncomingBloodWasNotOk,
				Bound = null,
				From = 0.001,
				To = 0.1,
				Steps = 25,
				UpdateParameterInModel = value => { model.HdMachine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefect.ProbabilityOfOccurrence=new Probability(value); }
			};
			var result=SafetySharpModelChecker.ConductQuantitativeParametricAnalysis(model, parameter);
			var fileWriterContamination = new StreamWriter("contamination.csv", append: false);
			result.ToCsv(fileWriterContamination);
			fileWriterContamination.Close();

			parameter.StateFormula = model.BloodNotCleanedAndDialyzingFinished;
			result=SafetySharpModelChecker.ConductQuantitativeParametricAnalysis(model, parameter);
			var fileWriterUnsuccessful = new StreamWriter("unsuccessful.csv", append: false);
			result.ToCsv(fileWriterUnsuccessful);
			fileWriterUnsuccessful.Close();
		}

		private static IEnumerable CreateModelVariants()
		{
			Func<bool, Model> createVariant = (waterHeaterFaultIsPermanent) =>
			{
				var previousValue = Modeling.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefectIsPermanent;
				Modeling.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefectIsPermanent = waterHeaterFaultIsPermanent;
				var model = new Model();
				Modeling.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefectIsPermanent = previousValue;
				return model;
			};

			return from waterHeaterFaultIsPermanent in new []{true,false}
		           let name = "WaterHeater"+ (waterHeaterFaultIsPermanent ? "Permanent":"Transient")
				   select new TestCaseData(createVariant(waterHeaterFaultIsPermanent)).SetName(name);
		}
	}
}
