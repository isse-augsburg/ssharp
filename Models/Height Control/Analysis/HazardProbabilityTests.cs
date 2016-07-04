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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HeightControl.Analysis
{
	using FluentAssertions;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	class HazardProbabilityTests
	{
		[Test]
		public void CalculateHazardInOriginalDesign()
		{
			var model = Model.CreateOriginal();
			model.VehicleSet.LeftHV.ProbabilityOfOccurrence = new Probability(0.01);
			model.VehicleSet.LeftOHV.ProbabilityOfOccurrence = new Probability(0.001);
			model.VehicleSet.SlowTraffic.ProbabilityOfOccurrence = new Probability(0.1);
			model.HeightControl.PreControl.PositionDetector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			model.HeightControl.PreControl.PositionDetector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);
			model.HeightControl.MainControl.PositionDetector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			model.HeightControl.MainControl.PositionDetector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);
			model.HeightControl.MainControl.LeftDetector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			model.HeightControl.MainControl.LeftDetector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);
			model.HeightControl.MainControl.RightDetector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			model.HeightControl.MainControl.RightDetector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);
			model.HeightControl.EndControl.LeftDetector.Misdetection.ProbabilityOfOccurrence = new Probability(0.0001);
			model.HeightControl.EndControl.LeftDetector.FalseDetection.ProbabilityOfOccurrence = new Probability(0.005);

			var result = ModelChecker.CalculateProbabilityOfHazard(model, model.Collision);
			Console.Write($"Probability of hazard: {result.Value}");
		}
	}
}
