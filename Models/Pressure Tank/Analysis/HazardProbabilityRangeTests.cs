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

namespace SafetySharp.CaseStudies.PressureTank.Analysis
{
	using FluentAssertions;
	using ISSE.SafetyChecking.Modeling;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;

	class HazardProbabilityRangeTests
	{
		[Test]
		public void CalculateHazardIsRuptured()
		{
			var model = new Model();
			model.Pump.SuppressPumping.ProbabilityOfOccurrence = new Probability(0.0);
			model.Sensor.SuppressIsFull.ProbabilityOfOccurrence = new Probability(0.0001);
			model.Sensor.SuppressIsEmpty.ProbabilityOfOccurrence = new Probability(0.0);
			model.Timer.SuppressTimeout.ProbabilityOfOccurrence = new Probability(0.0001);

			var result = SafetySharpModelChecker.CalculateProbabilityRangeToReachStateBounded(model, model.Tank.IsRuptured,200);
			Console.Write($"Probability of hazard: {result}");
		}

		[Test]
		public void CalculateHazardIsDepleted()
		{
			var model = new Model();
			model.Pump.SuppressPumping.ProbabilityOfOccurrence = Probability.Zero;
			model.Sensor.SuppressIsEmpty.ProbabilityOfOccurrence = Probability.Zero;
			model.Sensor.SuppressIsFull.ProbabilityOfOccurrence = Probability.Zero;
			model.Timer.SuppressTimeout.ProbabilityOfOccurrence = Probability.Zero;

			var result = SafetySharpModelChecker.CalculateProbabilityRangeToReachStateBounded(model, model.Tank.IsDepleted,200);
			Console.Write($"Probability of hazard: {result}");
		}
	}
}
