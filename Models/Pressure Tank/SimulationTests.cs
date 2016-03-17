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

namespace SafetySharp.CaseStudies.PressureTank
{
	using Analysis;
	using FluentAssertions;
	using Modeling;
	using NUnit.Framework;

	/// <summary>
	///   Contains a set of tests that simulate the case study to validate certain aspects of its behavior.
	/// </summary>
	public class SimulationTests
	{
		/// <summary>
		///   Simulates a path where no faults occur with the expectation that the tank does not rupture.
		/// </summary>
		[Test]
		public void TankDoesNotRuptureWhenNoFaultsOccur()
		{
			var model = new Model();
			model.Faults.SuppressActivations();

			var simulator = new Simulator(model);
			simulator.FastForward(steps: 120);

			model.Tank.IsRuptured.Should().BeFalse();
		}

		/// <summary>
		///   Simulates a path where only the sensor's 'is full' fault occurs with the expectation that the tank does not rupture.
		/// </summary>
		[Test]
		public void TankDoesNotRuptureWhenSensorDoesNotReportTankFull()
		{
			var model = new Model();
			model.Faults.SuppressActivations();
			model.Sensor.SuppressIsFull.ForceActivation();

			var simulator = new Simulator(model);
			simulator.FastForward(steps: 120);

			model.Tank.IsRuptured.Should().BeFalse();
		}

		/// <summary>
		///   Simulates a path where the sensor's 'is full' and the timer's 'timeout' faults with the expectation that the
		///   tank does in fact rupture.
		/// </summary>
		[Test]
		public void TankRupturesWhenSensorDoesNotReportTankFullAndTimerDoesNotTimeout()
		{
			var model = new Model();
			model.Faults.SuppressActivations();
			model.Sensor.SuppressIsFull.ForceActivation();
			model.Timer.SuppressTimeout.ForceActivation();

			// Check that the tank is still not ruptured after 120 steps
			var simulator = new Simulator(model);
			simulator.FastForward(steps: 120);

			model.Tank.IsRuptured.Should().BeFalse();

			// Check that the tank does not rupture in the first 120 steps
			simulator.Reset();
			for (var i = 0; i < 120; ++i)
			{
				simulator.SimulateStep();
				model.Tank.IsRuptured.Should().BeFalse();
			}
		}
	}
}