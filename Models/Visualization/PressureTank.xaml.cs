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
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;

	public partial class PressureTank
	{
		private const double MaxSpeed = 32;
		private const double MinSpeed = 0.25;
		private readonly Controller _controller;
		private readonly Storyboard _pressureLevelStoryboard;
		private readonly Storyboard _pumpingStoryboard;
		private readonly Storyboard _sensorAlertStoryboard;
		private readonly RealTimeSimulator _simulator;
		private readonly Storyboard _timerAlertStoryboard;
		private readonly Pump _pump;
		private readonly Sensor _sensor;
		private double _speed = 1;
		private readonly Tank _tank;
		private readonly Timer _timer;

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
			_simulator = new RealTimeSimulator(Model.Create(new Specification()), stepDelay: 1000);
			_simulator.SimulationStateChanged += (o, e) => UpdateSimulationButtonVisibilities();
			_simulator.ModelStateChanged += (o, e) => UpdateModelState();

			// Extract the components
			_tank = (Tank)_simulator.Model.RootComponents[0];
			_controller = (Controller)_simulator.Model.RootComponents[1];
			_pump = _controller.Pump;
			_timer = _controller.Timer;
			_sensor = _controller.Sensor;

			_pump.SuppressPumping.OccurrenceKind = OccurrenceKind.Never;
			_timer.SuppressTimeout.OccurrenceKind = OccurrenceKind.Never;
			_sensor.SuppressIsFull.OccurrenceKind = OccurrenceKind.Never;
			_sensor.SuppressIsEmpty.OccurrenceKind = OccurrenceKind.Never;

			// Initialize the visualization state
			UpdateSimulationButtonVisibilities();
			UpdateModelState();

			TimerAlert.Opacity = 0;
			SensorAlert.Opacity = 0;

			ChangeSpeed(8);
		}

		private void OnStop(object sender, RoutedEventArgs e)
		{
			if (_simulator.State != SimulationState.Stopped)
				_simulator.Stop();
		}

		private void OnRun(object sender, RoutedEventArgs e)
		{
			_simulator.Run();
		}

		private void OnPause(object sender, RoutedEventArgs e)
		{
			_simulator.Pause();
		}

		private void OnStep(object sender, RoutedEventArgs e)
		{
			_simulator.Step();
		}

		private void OnIncreaseSpeed(object sender, RoutedEventArgs e)
		{
			ChangeSpeed(_speed * 2);
		}

		private void OnDecreaseSpeed(object sender, RoutedEventArgs e)
		{
			ChangeSpeed(_speed / 2);
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

		private void ChangeSpeed(double speed)
		{
			speed = Math.Min(MaxSpeed, Math.Max(MinSpeed, speed));

			if (Math.Abs(speed - _speed) > 0.001)
			{
				_simulator.StepDelay = (int)Math.Round(1000 / speed);
				_speed = speed;
			}

			SimulationSpeed.Text = $"Speed: {_speed}x";
		}

		private void UpdateSimulationButtonVisibilities()
		{
			switch (_simulator.State)
			{
				case SimulationState.Stopped:
					StopButton.Visibility = Visibility.Collapsed;
					StartButton.Visibility = Visibility.Visible;
					PauseButton.Visibility = Visibility.Collapsed;
					StepButton.Visibility = Visibility.Visible;
					break;
				case SimulationState.Paused:
					StopButton.Visibility = Visibility.Visible;
					StartButton.Visibility = Visibility.Visible;
					PauseButton.Visibility = Visibility.Collapsed;
					StepButton.Visibility = Visibility.Visible;
					break;
				case SimulationState.Running:
					StopButton.Visibility = Visibility.Visible;
					StartButton.Visibility = Visibility.Collapsed;
					PauseButton.Visibility = Visibility.Visible;
					StepButton.Visibility = Visibility.Collapsed;
					break;
			}

			UpdateModelState();
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