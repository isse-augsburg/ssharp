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
	using System.IO;
	using FluentAssertions;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.Modeling;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using LtmcModelChecker = ISSE.SafetyChecking.LtmcModelChecker;

	class HazardProbabilityTests
	{
		[Test]
		public void Calculate()
		{
			var tc = SafetySharpModelChecker.TraversalConfiguration;
			tc.LtmcModelChecker = LtmcModelChecker.BuiltInLtmc;
			//tc.UseAtomarPropositionsAsStateLabels = false;
			SafetySharpModelChecker.TraversalConfiguration =tc;

			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = new Probability(0.0001);
			model.CrossingController.Motor.BarrierMotorStuck.ProbabilityOfOccurrence = new Probability(0.001);
			model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(0.00003);
			model.CrossingController.TrainSensor.ErroneousTrainDetection.ProbabilityOfOccurrence = new Probability(0.0002);
			model.TrainController.Brakes.BrakesFailure.ProbabilityOfOccurrence = new Probability(0.00002);
			model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(0.02);
			model.TrainController.Odometer.OdometerSpeedOffset.ProbabilityOfOccurrence = new Probability(0.02);

			var result = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(model, model.PossibleCollision,50);
			Console.Write($"Probability of hazard in 50 steps: {result}");
		}



		[Test]
		public void Parametric()
		{
			var tc = SafetySharpModelChecker.TraversalConfiguration;
			tc.LtmcModelChecker = LtmcModelChecker.BuiltInLtmc;
			//tc.UseAtomarPropositionsAsStateLabels = false;
			SafetySharpModelChecker.TraversalConfiguration = tc;

			var model = new Model();
			model.Channel.MessageDropped.ProbabilityOfOccurrence = new Probability(0.0001);
			model.CrossingController.Motor.BarrierMotorStuck.ProbabilityOfOccurrence = new Probability(0.001);
			model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(0.00003);
			model.CrossingController.TrainSensor.ErroneousTrainDetection.ProbabilityOfOccurrence = new Probability(0.0002);
			model.TrainController.Brakes.BrakesFailure.ProbabilityOfOccurrence = new Probability(0.00002);
			model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(0.02);
			model.TrainController.Odometer.OdometerSpeedOffset.ProbabilityOfOccurrence = new Probability(0.02);

			Action<double> updateParameterBsInModel = value =>
			{
				model.CrossingController.Sensor.BarrierSensorFailure.ProbabilityOfOccurrence = new Probability(value);
			};

			Action<double> updateParameterOpInModel = value =>
			{
				model.TrainController.Odometer.OdometerPositionOffset.ProbabilityOfOccurrence = new Probability(value);
			};

			var parameter = new QuantitativeParametricAnalysisParameter
			{
				StateFormula = model.PossibleCollision,
				Bound = 50,
				From = 0.000001,
				To = 0.01,
				Steps = 25,
				UpdateParameterInModel = updateParameterBsInModel
			};

			var result = SafetySharpModelChecker.ConductQuantitativeParametricAnalysis(model, parameter);
			var fileWriter = new StreamWriter("ParametricBsPossibleCollision.csv", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();

			parameter.UpdateParameterInModel = updateParameterOpInModel;
			result = SafetySharpModelChecker.ConductQuantitativeParametricAnalysis(model, parameter);
			fileWriter = new StreamWriter("ParametricOpPossibleCollision.csv", append: false);
			result.ToCsv(fileWriter);
			fileWriter.Close();
		}
	}
}
