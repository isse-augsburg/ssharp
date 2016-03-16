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

namespace SafetySharp.CaseStudies.PressureTank.Tests
{
	using System;
	using Analysis;
	using FluentAssertions;
	using Modeling;
	using NUnit.Framework;
	using Runtime.Reflection;
	using static Analysis.Tl;

	/// <summary>
	///   Contains a set of tests that model check the case study.
	/// </summary>
	public class ModelChecking : TestBase
	{
		/// <summary>
		///   Conducts a safety analysis (DCCA) for the case study. It prints a summary of the analysis and writes out witnesses for
		///   minimal critical fault sets to disk that can be replayed using the case study's visualization.
		/// </summary>
		[Test]
		public void RuptureDcca()
		{
			var analysis = new SafetyAnalysis();
			var result = analysis.ComputeMinimalCriticalSets(_model, _specification.Rupture);

			result.SaveCounterExamples("counter examples/pressure tank/dcca");
			Console.WriteLine(result);

			result.IsComplete.Should().BeTrue();
			result.MinimalCriticalSets.ShouldBeEquivalentTo(new[]
			{
				// The case study has only one single minimial critical set consisting of the following to faults
				new[] { _sensor.SuppressIsFull, _timer.SuppressTimeout }
			});
		}

		/// <summary>
		///   Simply enumerates all states of the case study by checking a valid formula; 'true', in this case. The test's primary
		///   intent is to support model checking efficiency measurements: Valid formulas represent the worst case for S# as all
		///   reachable states and transitions have to be computed.
		/// </summary>
		[Test]
		public void EnumerateAllStates()
		{
			var result = ModelChecker.CheckInvariant(_model, true);
			result.FormulaHolds.Should().BeTrue();
		}

		/// <summary>
		///   Uses LTL and LTSMin to check an LTL formula: When no faults occur, the tank should never rupture.
		/// </summary>
		[Test]
		public void TankDoesNotRuptureWhenNoFaultsOccurLTL()
		{
			Formula noFaults =
				!_sensor.SuppressIsEmpty.IsActivated &&
				!_sensor.SuppressIsFull.IsActivated &&
				!_pump.SuppressPumping.IsActivated &&
				!_timer.SuppressTimeout.IsActivated;

			var result = ModelChecker.Check(_model, G(noFaults).Implies(!F(_tank.IsRuptured)));
			result.FormulaHolds.Should().BeTrue();
		}

		/// <summary>
		///   Similar to 'TankDoesNotRuptureWhenNoFaultsOccurLTL', the test asserts that when no faults occur, the tank should never
		///   rupture. However, fault activations are supporessed in the model instead of by using LTL, which is more efficient.
		/// </summary>
		[Test]
		public void TankDoesNotRuptureWhenNoFaultsOccurModel()
		{
			_model.SuppressAllFaultActivations();

			var result = ModelChecker.CheckInvariant(_model, !_tank.IsRuptured);
			result.FormulaHolds.Should().BeTrue();
		}

		/// <summary>
		///   Checks that the tank never ruptures when only the sensor's 'is full' suppression fault are activated.
		/// </summary>
		[Test]
		public void TankDoesNotRuptureWhenSensorDoesNotReportTankFull()
		{
			_model.SuppressAllFaultActivations();
			_sensor.SuppressIsFull.Activation = Activation.Forced;

			// Setting the fault's activation to Activation.Forced means that it is always activated when it can be activated;
			// setting it to Activation.Nondeterministic instead allows the fault to be activated nondeterministically.
			// We use the former since we specifically want to check only those situations in which the fault is activated;
			// while these situations are obviously also included when Activation.Nondeterministic is used, forced activation
			// can be checked more efficiently. This optimization is made purely for reasons of illustration, 
			// of course, as the case study is so simple that model checking times are completely irrelevant.

			var result = ModelChecker.CheckInvariant(_model, !_tank.IsRuptured);
			result.FormulaHolds.Should().BeTrue();
		}

		/// <summary>
		///   Checks that the tank ruptures when the sensor's 'is full' and the timer's 'timeout' suppression faults are activated. A
		///   witness (= a counter example for the check that the tank never ruptures) is stored on disk so that it can be replayed
		///   using the visualization.
		/// </summary>
		[Test]
		public void TankRupturesWhenSensorDoesNotReportTankFullAndTimerDoesNotTimeout()
		{
			_model.SuppressAllFaultActivations();
			_sensor.SuppressIsFull.Activation = Activation.Forced;
			_timer.SuppressTimeout.Activation = Activation.Forced;

			var result = ModelChecker.CheckInvariant(_model, !_tank.IsRuptured);
			result.FormulaHolds.Should().BeFalse();

			result.CounterExample.Should().NotBeNull();
			result.CounterExample.Save("counter examples/pressure tank/tank rupture when sensor and timer fail");
		}

		/// <summary>
		///   Checks that the tank can indeed rupture. A witness (= a counter example for the check that the tank never ruptures) is
		///   stored on disk so that it can be replayed using the visualization.
		/// </summary>
		[Test]
		public void TankRupturesShouldBePossible()
		{
			var result = ModelChecker.CheckInvariant(_model, !_tank.IsRuptured);
			result.FormulaHolds.Should().BeFalse();

			result.CounterExample.Should().NotBeNull();
			result.CounterExample.Save("counter examples/pressure tank/possible tank rupture");
		}

		/// <summary>
		///   Checks that the timeout never elapsed when the sensor is always working correctly.
		/// </summary>
		[Test]
		public void TimerNeverOpensWhenSensorIsAlwaysWorking()
		{
			_model.SuppressAllFaultActivations();

			var result = ModelChecker.CheckInvariant(_model, !_timer.HasElapsed);
			result.FormulaHolds.Should().BeTrue();
		}

		/// <summary>
		///   Checks that the timeout indeed elapses at some point when the sensor is not always working correctly. A witness (= a
		///   counter example for the check that the timeout never elapses) is stored on disk so that it can be replayed using the
		///   visualization.
		/// </summary>
		[Test]
		public void TimerOpensWhenSensorIsNotWorking()
		{
			_model.SuppressAllFaultActivations();
			_sensor.SuppressIsFull.Activation = Activation.Forced;

			var result = ModelChecker.CheckInvariant(_model, !_timer.HasElapsed);
			result.FormulaHolds.Should().BeFalse();

			result.CounterExample.Should().NotBeNull();
			result.CounterExample.Save("counter examples/pressure tank/possible timeout");
		}
	}
}