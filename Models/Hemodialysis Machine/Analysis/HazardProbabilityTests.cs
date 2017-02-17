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
	using SafetySharp.Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;

	class HazardProbabilityTests
	{
		private readonly Probability _prob1Eneg1 = new Probability(0.1);
		private readonly Probability _prob1Eneg2 = new Probability(0.01);
		private readonly Probability _prob1Eneg3 = new Probability(0.001);
		private readonly Probability _prob1Eneg5 = new Probability(0.00001);
		private readonly Probability _prob1Eneg7 = new Probability(0.0000001);

		[Test]
		public void IncomingBloodIsContaminated()
		{
			var model = new Model();

			model.HdMachine.DialyzingFluidDeliverySystem.DialyzingFluidPreparation.DialyzingFluidPreparationPumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.PumpDefect.ProbabilityOfOccurrence = _prob1Eneg3;
			model.HdMachine.DialyzingFluidDeliverySystem.SafetyBypass.SafetyBypassFault.ProbabilityOfOccurrence = _prob1Eneg3;
			model.HdMachine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefect.ProbabilityOfOccurrence = _prob1Eneg2;

			model.HdMachine.ExtracorporealBloodCircuit.ArterialBloodPump.BloodPumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.ExtracorporealBloodCircuit.VenousSafetyDetector.SafetyDetectorDefect.ProbabilityOfOccurrence = _prob1Eneg7;
			model.HdMachine.ExtracorporealBloodCircuit.VenousTubingValve.ValveDoesNotClose.ProbabilityOfOccurrence = _prob1Eneg5;

			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = _prob1Eneg5;

			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.IncomingBloodWasNotOk);
			Console.Write($"Probability of hazard: {result.Value}");
		}

		[Test]
		public void DialysisFinishedAndBloodNotCleaned()
		{
			var model = new Model();

			model.HdMachine.DialyzingFluidDeliverySystem.DialyzingFluidPreparation.DialyzingFluidPreparationPumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.PumpDefect.ProbabilityOfOccurrence = _prob1Eneg3;
			model.HdMachine.DialyzingFluidDeliverySystem.SafetyBypass.SafetyBypassFault.ProbabilityOfOccurrence = _prob1Eneg3;
			model.HdMachine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefect.ProbabilityOfOccurrence = _prob1Eneg2;

			model.HdMachine.ExtracorporealBloodCircuit.ArterialBloodPump.BloodPumpDefect.ProbabilityOfOccurrence = _prob1Eneg5;
			model.HdMachine.ExtracorporealBloodCircuit.VenousSafetyDetector.SafetyDetectorDefect.ProbabilityOfOccurrence = _prob1Eneg7;
			model.HdMachine.ExtracorporealBloodCircuit.VenousTubingValve.ValveDoesNotClose.ProbabilityOfOccurrence = _prob1Eneg5;

			model.HdMachine.Dialyzer.DialyzerMembraneRupturesFault.ProbabilityOfOccurrence = _prob1Eneg5;

			var result = SafetySharpModelChecker.CalculateProbabilityToReachState(model, model.BloodNotCleanedAndDialyzingFinished);
			Console.Write($"Probability of hazard: {result.Value}");
		}
	}
}
