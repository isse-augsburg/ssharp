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

namespace Visualization.Infrastructure
{
	using System;
	using System.Windows;
	using SafetySharp.Runtime;

	public partial class SimulationControls
	{
		private RealTimeSimulator _simulator;
		private double _speed;

		public double MaxSpeed = 32;
		public double MinSpeed = 0.25;

		public event EventHandler Reset;

		public SimulationControls()
		{
			InitializeComponent();
		}

		public void SetSimulator(RealTimeSimulator simulator)
		{
			_simulator = simulator;

			simulator.SimulationStateChanged += (o, e) => UpdateSimulationButtonVisibilities();
			UpdateSimulationButtonVisibilities();
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
		}

		private void OnStop(object sender, RoutedEventArgs e)
		{
			if (_simulator.State == SimulationState.Stopped)
				return;

			_simulator.Stop();
			Reset?.Invoke(this, EventArgs.Empty);
		}

		private void OnRun(object sender, RoutedEventArgs e)
		{
			_simulator.Run();
			Reset?.Invoke(this, EventArgs.Empty);
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

		public void ChangeSpeed(double speed)
		{
			speed = Math.Min(MaxSpeed, Math.Max(MinSpeed, speed));

			if (Math.Abs(speed - _speed) > 0.001)
			{
				_simulator.StepDelay = (int)Math.Round(1000 / speed);
				_speed = speed;
			}

			SimulationSpeed.Text = $"Speed: {_speed}x";
		}
	}
}