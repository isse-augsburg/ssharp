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

namespace SafetySharp.CaseStudies.Visualizations.Infrastructure
{
	using System;
	using System.Windows;
	using Analysis;
	using Microsoft.Win32;
	using Modeling;
	using Runtime;

	public partial class SimulationControls
	{
		private Formula[] _formulas;
		private ModelBase _model;
		private double _speed;
		public double MaxSpeed = 32;
		public double MinSpeed = 0.25;

		public SimulationControls()
		{
			InitializeComponent();
			CloseCounterExampleButton.Visibility = Visibility.Hidden;
		}

		public RealTimeSafetySharpSimulator Simulator { get; private set; }
		public ModelBase Model => Simulator.Model;
		public SafetySharpRuntimeModel RuntimeModel => Simulator.RuntimeModel;

		public int StepDelay { get; set; } = 1000;
		public bool ReplayingCounterExample => Simulator?.IsReplay ?? false;

		public event EventHandler Reset;
		public event EventHandler Rewound;
		public event EventHandler ModelStateChanged;

		private void SetSimulator(SafetySharpSimulator simulator)
		{
			if (Simulator != null)
			{
				Simulator.ModelStateChanged -= OnModelStateChanged;
				Simulator.Pause();
			}

			Simulator = new RealTimeSafetySharpSimulator(simulator, (int)Math.Round(1000 / _speed));
			Simulator.ModelStateChanged += OnModelStateChanged;
			UpdateSimulationButtonVisibilities();

			OnReset(null, null);
		}

		public void SetModel(ModelBase model, params Formula[] formulas)
		{
			_formulas = formulas;
			_model = model;

			foreach (var fault in _model.Faults)
				fault.Activation = Activation.Suppressed;

			SetSimulator(new SafetySharpSimulator(_model, formulas));
		}

		private void OnModelStateChanged(object sender, EventArgs e)
		{
			ModelStateChanged?.Invoke(sender, e);
			UpdateSimulationButtonVisibilities();

			if (Simulator.IsCompleted)
				EndOfCounterExample.Visibility = Visibility.Visible;
			else
				EndOfCounterExample.Visibility = Visibility.Hidden;
		}

		private void UpdateSimulationButtonVisibilities()
		{
			if (Simulator.CanFastForward)
				FastForwardButton.Enable();
			else
				FastForwardButton.Disable();

			if (Simulator.CanRewind)
				RewindButton.Enable();
			else
				RewindButton.Disable();

			if (Simulator.IsRunning)
			{
				StartButton.Visibility = Visibility.Collapsed;
				PauseButton.Visibility = Visibility.Visible;
			}
			else
			{
				StartButton.Visibility = Visibility.Visible;
				PauseButton.Visibility = Visibility.Collapsed;
			}
		}

		private void OnReset(object sender, RoutedEventArgs e)
		{
			Reset?.Invoke(this, EventArgs.Empty);

			Simulator.Reset();
			EndOfCounterExample.Visibility = Visibility.Hidden;
		}

		private void OnRun(object sender, RoutedEventArgs e)
		{
			Simulator.Run();
		}

		private void OnPause(object sender, RoutedEventArgs e)
		{
			Simulator.Pause();
		}

		private void OnStep(object sender, RoutedEventArgs e)
		{
			Simulator.FastForward(1);
		}

		private void OnStepBack(object sender, RoutedEventArgs e)
		{
			Simulator.Rewind(1);
			Rewound?.Invoke(this, EventArgs.Empty);
		}

		private void OnRewind(object sender, RoutedEventArgs e)
		{
			Simulator.Rewind(10);
			Rewound?.Invoke(this, EventArgs.Empty);
		}

		private void OnFastForward(object sender, RoutedEventArgs e)
		{
			Simulator.FastForward(10);
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
			var counterExampleSerialization = new SafetySharpCounterExampleSerialization();
			var dialog = new OpenFileDialog
			{
				AddExtension = true,
				CheckFileExists = true,
				CheckPathExists = true,
				DefaultExt = ".ltsmin",
				Filter = $"S# Counter Examples (*{counterExampleSerialization.FileExtension})|*{counterExampleSerialization.FileExtension}",
				Title = "Open S# Counter Example",
				Multiselect = false
			};

			if (dialog.ShowDialog() != true)
				return;

			try
			{
				var simulator = new SafetySharpSimulator(counterExampleSerialization.Load(dialog.FileName));

				SetSimulator(simulator);
				CloseCounterExampleButton.Visibility = Visibility.Visible;
				
			}
			catch (Exception ex)
			{
				var message = "An incompatible change has been made to the model since the counter example has been generated: " + ex.Message;
				MessageBox.Show(message, "Failed to Load Counter Example", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OnCounterExampleClosed(object sender, RoutedEventArgs e)
		{
			SetSimulator(new SafetySharpSimulator(_model, _formulas));
			CloseCounterExampleButton.Visibility = Visibility.Hidden;
		}
	}
}