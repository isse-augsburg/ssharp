﻿// The MIT License (MIT)
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
	using System.Windows.Media.Animation;
	using Microsoft.Win32;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Reflection;

	public partial class SimulationControls
	{
		private readonly Storyboard _counterExampleEndStoryboard;
		private Formula[] _formulas;
		private Model _model;
		private double _speed;
		public double MaxSpeed = 32;
		public double MinSpeed = 0.25;

		public SimulationControls()
		{
			InitializeComponent();

			CloseCounterExampleButton.Visibility = Visibility.Collapsed;
			EndOfCounterExample.Opacity = 0;
			_counterExampleEndStoryboard = (Storyboard)Resources["CounterExampleEnd"];
		}

		public RealTimeSimulator Simulator { get; private set; }
		public RuntimeModel Model => Simulator.Model;
		public SimulationState State => Simulator.State;

		public int StepDelay { get; set; } = 1000;

		public event EventHandler Reset;
		public event EventHandler ModelStateChanged;

		private void SetSimulator(Simulator simulator)
		{
			if (Simulator != null)
			{
				Simulator.ModelStateChanged -= OnModelStateChanged;
				Simulator.Completed -= OnSimulationCompleted;
			}

			Simulator = new RealTimeSimulator(simulator, (int)Math.Round(1000 / _speed));
			Simulator.ModelStateChanged += OnModelStateChanged;
			Simulator.SimulationStateChanged += (o, e) => UpdateSimulationButtonVisibilities();
			UpdateSimulationButtonVisibilities();
		}

		public void SetSpecification(object specification, params Formula[] formulas)
		{
			_formulas = formulas;
			_model = SafetySharp.Analysis.Model.Create(specification);

			foreach (var fault in _model.GetFaults())
				fault.ActivationMode = ActivationMode.Suppressed;

			SetSimulator(new Simulator(_model, formulas));
		}

		private void OnModelStateChanged(object sender, EventArgs e)
		{
			ModelStateChanged?.Invoke(sender, e);
		}

		private void OnSimulationCompleted(object sender, EventArgs e)
		{
			_counterExampleEndStoryboard.Begin();
		}

		private void UpdateSimulationButtonVisibilities()
		{
			switch (Simulator.State)
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
			if (Simulator.State == SimulationState.Stopped)
				return;

			Simulator.Stop();
			Reset?.Invoke(this, EventArgs.Empty);
			_counterExampleEndStoryboard.Stop();
		}

		private void OnRun(object sender, RoutedEventArgs e)
		{
			Simulator.Run();
			Reset?.Invoke(this, EventArgs.Empty);
		}

		private void OnPause(object sender, RoutedEventArgs e)
		{
			Simulator.Pause();
		}

		private void OnStep(object sender, RoutedEventArgs e)
		{
			Simulator.Step();
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
				Simulator.StepDelay = (int)Math.Round(1000 / speed);
				_speed = speed;
			}

			SimulationSpeed.Text = $"Speed: {_speed}x";
		}

		private void OnCounterExample(object sender, RoutedEventArgs e)
		{
			var dialog = new OpenFileDialog
			{
				AddExtension = true,
				CheckFileExists = true,
				CheckPathExists = true,
				DefaultExt = ".ltsmin",
				Filter = $"S# Counter Examples (*{CounterExample.FileExtension})|*{CounterExample.FileExtension}",
				Title = "Open S# Counter Example",
				Multiselect = false
			};

			if (dialog.ShowDialog() != true)
				return;

			try
			{
				var simulator = new Simulator(CounterExample.Load(dialog.FileName));
				simulator.Completed += OnSimulationCompleted;

				SetSimulator(simulator);
				CloseCounterExampleButton.Visibility = Visibility.Visible;

				ModelStateChanged?.Invoke(this, EventArgs.Empty);
				Reset?.Invoke(this, EventArgs.Empty);
				_counterExampleEndStoryboard.Stop();
			}
			catch (Exception ex)
			{
				var message = "An incompatible change has been made to the model since the counter example has been generated: " + ex.Message;
				MessageBox.Show(message, "Failed to Load Counter Example", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OnCounterExampleClosed(object sender, RoutedEventArgs e)
		{
			SetSimulator(new Simulator(_model, _formulas));
			CloseCounterExampleButton.Visibility = Visibility.Hidden;

			ModelStateChanged?.Invoke(this, EventArgs.Empty);
			Reset?.Invoke(this, EventArgs.Empty);
			_counterExampleEndStoryboard.Stop();
		}
	}
}