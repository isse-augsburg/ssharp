// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.CaseStudies.RailroadCrossing.Analysis
{
	using System;
	using FluentAssertions;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ModelChecking;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;

	public class SafetyAnalysisTests
	{
		[TestCase]
		public void Collision(
			[Values(SafetyAnalysisBackend.FaultOptimizedStateGraph, SafetyAnalysisBackend.FaultOptimizedOnTheFly)] SafetyAnalysisBackend backend)
		{
			var model = new Model();
			var result = SafetySharpSafetyAnalysis.AnalyzeHazard(model, model.PossibleCollision, backend: backend);
			result.SaveCounterExamples("counter examples/railroad crossing/dcca/collision");

			var orderResult = SafetySharpOrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);

			result.IsComplete.Should().BeTrue();
			result.MinimalCriticalSets.ShouldAllBeEquivalentTo(new[]
			{
				new[] { model.TrainController.Odometer.OdometerPositionOffset },
				new[] { model.TrainController.Odometer.OdometerSpeedOffset },
				new[] { model.CrossingController.Sensor.BarrierSensorFailure },
				new[] { model.CrossingController.TrainSensor.ErroneousTrainDetection },
				new[] { model.CrossingController.Motor.BarrierMotorStuck, model.TrainController.Brakes.BrakesFailure },
				new[] { model.Channel.MessageDropped, model.TrainController.Brakes.BrakesFailure }
			});
		}
	}
}