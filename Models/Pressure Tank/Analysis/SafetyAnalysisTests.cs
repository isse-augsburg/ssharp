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

namespace SafetySharp.CaseStudies.PressureTank.Analysis
{
	using System;
	using System.Linq;
	using FluentAssertions;
	using Modeling;
	using NUnit.Framework;
	using SafetySharp.Analysis;

	/// <summary>
	///   Conducts safety analyses using Deductive Cause Consequence Analysis for the hazards of the case study.
	/// </summary>
	public class SafetyAnalysisTests
	{
		/// <summary>
		///   Conducts a DCCA for the hazard of a tank rupture. It prints a summary of the analysis and writes out witnesses for
		///   minimal critical fault sets to disk that can be replayed using the case study's visualization.
		/// </summary>
		[Test]
		public void TankRupture()
		{
			var model = new Model();
			var result = SafetyAnalysis.AnalyzeHazard(model, model.Tank.IsRuptured);
			result.SaveCounterExamples("counter examples/pressure tank/dcca/tank rupture");

			var orderResult = OrderAnalysis.ComputeOrderRelationships(result);
			Console.WriteLine(orderResult);

			result.IsComplete.Should().BeTrue();
			result.MinimalCriticalSets.ShouldAllBeEquivalentTo(new[]
			{
				// The tank rupture hazard has only one single minimial critical set consisting of the following to faults
				new[] { model.Sensor.SuppressIsFull, model.Timer.SuppressTimeout }
			});

			orderResult.OrderRelationships[result.MinimalCriticalSets.Single()].Single().FirstFault.Should().Be(model.Sensor.SuppressIsFull);
			orderResult.OrderRelationships[result.MinimalCriticalSets.Single()].Single().SecondFault.Should().Be(model.Timer.SuppressTimeout);
		}

		/// <summary>
		///   Conducts a DCCA for the hazard of a fully depleted tank. It prints a summary of the analysis and writes out witnesses for
		///   minimal critical fault sets to disk that can be replayed using the case study's visualization.
		/// </summary>
		[Test]
		public void TankDepleted()
		{
			var model = new Model();
			var result = SafetyAnalysis.AnalyzeHazard(model, model.Tank.IsDepleted);

			result.SaveCounterExamples("counter examples/pressure tank/dcca/tank depleted");
			Console.WriteLine(result);

			result.IsComplete.Should().BeTrue();
			result.MinimalCriticalSets.ShouldAllBeEquivalentTo(new[]
			{
				// The tank depleted hazard has two singleton minimial critical set
				new[] { model.Sensor.SuppressIsEmpty },
				new[] { model.Pump.SuppressPumping }
			});
		}
	}
}