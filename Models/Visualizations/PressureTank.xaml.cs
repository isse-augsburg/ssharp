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

namespace SafetySharp.CaseStudies.Visualizations
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media.Animation;
	using CaseStudies.PressureTank.Analysis;
	using CaseStudies.PressureTank.Modeling;
	using Infrastructure;
	using Modeling;

	public partial class PressureTank
	{
		private readonly Storyboard _pressureLevelStoryboard;
		private readonly Storyboard _pumpingStoryboard;
		private readonly Storyboard _sensorAlertStoryboard;
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
			SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.Reset += (o, e) => OnModelStateReset();
			SimulationControls.SetModel(new Model());

			// Initialize the visualization state
			UpdateModelState();

			TimerAlert.Opacity = 0;
			SensorAlert.Opacity = 0;
			SimulationControls.MaxSpeed = 64;
			SimulationControls.ChangeSpeed(8);
		}

		private Pump Pump => Controller.Pump;
		private PressureSensor Sensor => Controller.Sensor;
		private Timer Timer => Controller.Timer;
		private Tank Tank => SimulationControls.Model.RootComponents.OfType<Tank>().Single();
		private Controller Controller => SimulationControls.Model.RootComponents.OfType<Controller>().Single();

		private void OnSuppressPumping(object sender, RoutedEventArgs e)
		{
			Pump.SuppressPumping.ToggleActivationMode();
		}

		private void OnSuppressTimeout(object sender, RoutedEventArgs e)
		{
			Timer.SuppressTimeout.ToggleActivationMode();
		}

		private void OnSuppressFull(object sender, RoutedEventArgs e)
		{
			Sensor.SuppressIsFull.ToggleActivationMode();
		}

		private void OnSuppressEmpty(object sender, RoutedEventArgs e)
		{
			Sensor.SuppressIsEmpty.ToggleActivationMode();
		}

		private void OnModelStateReset()
		{
			if (SimulationControls.Simulator.IsReplay)
				return;

			Sensor.SuppressIsFull.Activation = SuppressFull.IsChecked.ToOccurrenceKind();
			Sensor.SuppressIsEmpty.Activation = SuppressEmpty.IsChecked.ToOccurrenceKind();
			Timer.SuppressTimeout.Activation = SuppressTimeout.IsChecked.ToOccurrenceKind();
			Pump.SuppressPumping.Activation = SuppressPumping.IsChecked.ToOccurrenceKind();
		}

		private void UpdateModelState()
		{
			// Timer
			CountDown.Text = Timer.RemainingTime.ToString();
			CountDown.Visibility = Timer.IsActive.ToVisibility();
			SuppressTimeout.IsChecked = Timer.SuppressTimeout.IsActivated;
			TimerFailure.Visibility = SuppressTimeout.IsChecked.ToVisibility();

			if (Timer.HasElapsed)
				_timerAlertStoryboard.Begin();

			// Tank
			var pressureLevel = Math.Round(Tank.PressureLevel / (double)Model.PressureLimit * 100);
			_pressureLevelStoryboard.Seek(TimeSpan.FromMilliseconds(Math.Max(0, 10 * pressureLevel)));
			PressureLevel.Text = $"{pressureLevel}%";
			PressureLevel.Visibility = (!Tank.IsRuptured).ToVisibility();
			TankRupture.Visibility = Tank.IsRuptured.ToVisibility();

			// Sensor
			SuppressFull.IsChecked = Sensor.SuppressIsFull.IsActivated;
			SuppressEmpty.IsChecked = Sensor.SuppressIsEmpty.IsActivated;
			SensorFailure.Visibility = (SuppressFull.IsChecked || SuppressEmpty.IsChecked).ToVisibility();

			if ((Sensor.IsFull || Sensor.IsEmpty))
				_sensorAlertStoryboard.Begin();

			// Controller
			switch (Controller.StateMachine.State)
			{
				case Controller.State.Inactive:
					ControllerScreen.Text = "Inactive";
					break;
				case Controller.State.Filling:
					ControllerScreen.Text = "Filling";
					break;
				case Controller.State.StoppedBySensor:
					ControllerScreen.Text = "Stopped: Sensor";
					break;
				case Controller.State.StoppedByTimer:
					ControllerScreen.Text = "Stopped: Timer";
					break;
			}

			// Pump
			if (!Pump.IsEnabled)
				_pumpingStoryboard.Pause();
			else
				_pumpingStoryboard.Resume();

			SuppressPumping.IsChecked = Pump.SuppressPumping.IsActivated;
			PumpFailure.Visibility = SuppressPumping.IsChecked.ToVisibility();
		}
	}
}