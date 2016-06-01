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
	using System.Windows;
	using System.Windows.Media.Animation;
	using CaseStudies.CircuitBasedPressureTank.Modeling;
	using Infrastructure;
	using Modeling;

	public partial class CircuitBasedPressureTank
	{
		private Model _model;

		public CircuitBasedPressureTank()
		{
			InitializeComponent();

			// Initialize visualization resources
			PowerSourceControlCircuit.IsActive = () => _model.Circuits.PowerSourceControlCircuit.IsPowered();
			PowerSourceMotorCircuit.IsActive = () => _model.Circuits.PowerSourceMotorCircuit.IsPowered();
			LoadCircuitContactK1.IsClosed = () => _model.Circuits.K1.ContactIsClosed;
			NamedElementK1.IsActive = () => _model.Circuits.K1.ControlCircuit.IsPowered();
			LoadCircuitContactK2.IsClosed = () => _model.Circuits.K2.ContactIsClosed;
			NamedElementK2.IsActive = () => _model.Circuits.K2.ControlCircuit.IsPowered();
			LoadCircuitContactTimer.IsClosed =  () => _model.Circuits.Timer.ContactIsClosed;
			NamedElementTimer.IsActive = () => _model.Circuits.Timer.ControlCircuit.IsPowered();
			LoadCircuitContactSensor.IsClosed = () => _model.Circuits.Sensor.ContactIsClosed;
			NamedElementSensor.IsActive = () => _model.Circuits.Sensor.IsFull;
			Switch.IsPushed = () => _model.Circuits.Switch.SwitchIsPushed;
			Pump.IsPowered = () => _model.Circuits.Pump.MainCircuit.IsPowered();
			Pump.IsPumping = () => _model.Circuits.Pump.IsEnabled();

			// Initialize the simulation environment
			SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.Reset += (o, e) => OnModelStateReset();
			SimulationControls.SetModel(new Model());

			// Initialize the visualization state
			UpdateModelState();
			
			SimulationControls.MaxSpeed = 64;
			SimulationControls.ChangeSpeed(8);

			NamedElementK1.NameOfElement = "K1";
			NamedElementK2.NameOfElement = "K2";
			NamedElementSensor.NameOfElement = "S";
			NamedElementTimer.NameOfElement = "T";
		}

		private void OnModelStateReset()
		{
			_model = (Model)SimulationControls.Model;

			if (SimulationControls.Simulator.IsReplay)
				return;

			//_model.Sensor.SuppressIsFull.Activation = SuppressFull.IsChecked.ToOccurrenceKind();
			//_model.Sensor.SuppressIsEmpty.Activation = SuppressEmpty.IsChecked.ToOccurrenceKind();
			//_model.Timer.SuppressTimeout.Activation = SuppressTimeout.IsChecked.ToOccurrenceKind();
			//_model.Pump.SuppressPumping.Activation = SuppressPumping.IsChecked.ToOccurrenceKind();
		}

		private void UpdateModelState()
		{
			// Timer
			CountDown.Text = _model.Circuits.Timer.RemainingTime().ToString();
			//CountDown.Visibility = _model.Circuits.Timer.IsActive.ToVisibility();
			//SuppressTimeout.IsChecked = _model.Timer.SuppressTimeout.IsActivated;
			
			// Tank
			var pressureLevel = Math.Round(_model.Tank.PressureLevel / (double)Model.PressureLimit * 100);
			//_pressureLevelStoryboard.Seek(TimeSpan.FromMilliseconds(Math.Max(0, 10 * pressureLevel)));
			PressureLevel.Text = $"{pressureLevel}%";
			PressureLevel.Visibility = (!_model.Tank.IsRuptured).ToVisibility();
			TankRupture.Visibility = _model.Tank.IsRuptured.ToVisibility();

			PowerSourceControlCircuit.Update();
			PowerSourceMotorCircuit.Update();
			LoadCircuitContactK1.Update();
			NamedElementK1.Update();
			LoadCircuitContactK2.Update();
			NamedElementK2.Update();
			LoadCircuitContactTimer.Update();
			NamedElementTimer.Update();
			LoadCircuitContactSensor.Update();
			NamedElementSensor.Update();
			Switch.Update();
			Pump.Update();
		}
	}
}