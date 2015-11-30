// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace Visualization
{
	using System;
	using System.Windows;
	using System.Windows.Media.Animation;
	using global::PressureTank;
	using Infrastructure;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Reflection;

	public partial class PressureTank
	{
		private readonly Controller _controller;
		private readonly Storyboard _pressureLevelStoryboard;
		private readonly Pump _pump;
		private readonly Storyboard _pumpingStoryboard;
		private readonly Sensor _sensor;
		private readonly Storyboard _sensorAlertStoryboard;
		private readonly RealTimeSimulator _simulator;
		private readonly Tank _tank;
		private readonly Timer _timer;
		private readonly Storyboard _timerAlertStoryboard;

		public PressureTank()
		{
			InitializeComponent();

			// Initialize visualization resources
			_pumpingStoryboard = (Storyboard)Resources["RotatePump"];
			_pumpingStoryboard.Begin();

			_pressureLevelStoryboard = (Storyboard)Resources["PressureLevel"];
			_pressureLevelStoryboard.Begin();
			_pressureLevelStoryboard.Pause();

			_timerAlertStoryboard = (Storyboard)Resources["TimerEvent"];
			_sensorAlertStoryboard = (Storyboard)Resources["SensorEvent"];

			// Initialize the simulation environment
			var model = Model.Create(new Specification());
			foreach (var fault in model.GetFaults())
				fault.OccurrenceKind = OccurrenceKind.Never;

			_simulator = new RealTimeSimulator(model, stepDelay: 1000);
			_simulator.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.SetSimulator(_simulator);
			SimulationControls.Reset += (o, e) => OnModelStateReset();

			// Extract the components
			_tank = (Tank)_simulator.Model.RootComponents[0];
			_controller = (Controller)_simulator.Model.RootComponents[1];
			_pump = _controller.Pump;
			_timer = _controller.Timer;
			_sensor = _controller.Sensor;

			// Initialize the visualization state
			UpdateModelState();

			TimerAlert.Opacity = 0;
			SensorAlert.Opacity = 0;
			SimulationControls.ChangeSpeed(8);
		}

		private void OnSuppressPumping(object sender, RoutedEventArgs e)
		{
			_pump.SuppressPumping.ToggleOccurrence();
		}

		private void OnSuppressTimeout(object sender, RoutedEventArgs e)
		{
			_timer.SuppressTimeout.ToggleOccurrence();
		}

		private void OnSuppressFull(object sender, RoutedEventArgs e)
		{
			_sensor.SuppressIsFull.ToggleOccurrence();
		}

		private void OnSuppressEmpty(object sender, RoutedEventArgs e)
		{
			_sensor.SuppressIsEmpty.ToggleOccurrence();
		}

		private void OnModelStateReset()
		{
			_sensor.SuppressIsFull.OccurrenceKind = SuppressFull.IsChecked.ToOccurrenceKind();
			_sensor.SuppressIsEmpty.OccurrenceKind = SuppressEmpty.IsChecked.ToOccurrenceKind();
			_timer.SuppressTimeout.OccurrenceKind = SuppressTimeout.IsChecked.ToOccurrenceKind();
			_pump.SuppressPumping.OccurrenceKind = SuppressPumping.IsChecked.ToOccurrenceKind();
		}

		private void UpdateModelState()
		{
			// Timer
			CountDown.Text = _timer.RemainingTime.ToString();
			CountDown.Visibility = _timer.IsActive.ToVisibility();
			SuppressTimeout.IsChecked = _timer.SuppressTimeout.IsOccurring;
			TimerFailure.Visibility = SuppressTimeout.IsChecked.ToVisibility();

			if (_timer.HasElapsed)
				_timerAlertStoryboard.Begin();

			// Tank
			var pressureLevel = Math.Round(_tank.PressureLevel / (double)Specification.MaxPressure * 100);
			_pressureLevelStoryboard.Seek(TimeSpan.FromMilliseconds(10 * pressureLevel));
			PressureLevel.Text = $"{pressureLevel}%";
			PressureLevel.Visibility = (!_tank.IsRuptured).ToVisibility();
			TankRupture.Visibility = _tank.IsRuptured.ToVisibility();

			// Sensor
			SuppressFull.IsChecked = _sensor.SuppressIsFull.IsOccurring;
			SuppressEmpty.IsChecked = _sensor.SuppressIsEmpty.IsOccurring;
			SensorFailure.Visibility = (SuppressFull.IsChecked || SuppressEmpty.IsChecked).ToVisibility();

			if ((_sensor.IsFull || _sensor.IsEmpty) && _simulator.State != SimulationState.Stopped)
				_sensorAlertStoryboard.Begin();

			// Controller
			switch (_controller.StateMachine.State)
			{
				case global::PressureTank.Controller.State.Inactive:
					ControllerScreen.Text = "Inactive";
					break;
				case global::PressureTank.Controller.State.Filling:
					ControllerScreen.Text = "Filling";
					break;
				case global::PressureTank.Controller.State.StoppedBySensor:
					ControllerScreen.Text = "Stopped: Sensor";
					break;
				case global::PressureTank.Controller.State.StoppedByTimer:
					ControllerScreen.Text = "Stopped: Timer";
					break;
			}

			// Pump
			if (!_pump.IsEnabled || _simulator.State == SimulationState.Stopped)
				_pumpingStoryboard.Pause();
			else
				_pumpingStoryboard.Resume();

			SuppressPumping.IsChecked = _pump.SuppressPumping.IsOccurring;
			PumpFailure.Visibility = SuppressPumping.IsChecked.ToVisibility();
		}
	}
}