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
		
		private void OnSuppressPumping(object sender, RoutedEventArgs e)
		{
			_model.Circuits.Pump.SuppressPumping.ToggleActivationMode();
		}

		private void OnSwitchStuck(object sender, RoutedEventArgs e)
		{
			//TODO: = _model.Circuits.Switch..ToggleActivationMode();
		}

		private void OnRelayK1Stuck(object sender, RoutedEventArgs e)
		{
			_model.Circuits.K1.StuckFault.ToggleActivationMode();
		}

		private void OnRelayK2Stuck(object sender, RoutedEventArgs e)
		{
			_model.Circuits.K2.StuckFault.ToggleActivationMode();
		}

		private void OnSensorRelayStuck(object sender, RoutedEventArgs e)
		{
			//TODO: _model.Circuits.Sensor..ToggleActivationMode();
		}

		private void OnSensorSupressIsFull(object sender, RoutedEventArgs e)
		{
			_model.Circuits.Sensor.SuppressIsFull.ToggleActivationMode();
		}

		private void OnTimerRelayStuck(object sender, RoutedEventArgs e)
		{
			_model.Circuits.Timer.StuckFault.ToggleActivationMode();
		}

		private void OnModelStateReset()
		{
			_model = (Model)SimulationControls.Model;

			if (SimulationControls.Simulator.IsReplay)
				return;
			
			_model.Circuits.Pump.SuppressPumping.Activation = SuppressPumping.IsChecked.ToOccurrenceKind();
			//TODO: = _model.Circuits.Switch..Activation = SwitchStuck.IsChecked.ToOccurrenceKind();
			_model.Circuits.K1.StuckFault.Activation = RelayK1Stuck.IsChecked.ToOccurrenceKind();
			_model.Circuits.K2.StuckFault.Activation = RelayK2Stuck.IsChecked.ToOccurrenceKind();
			//TODO: _model.Circuits.Sensor..Activation = SensorRelayStuck.IsChecked.ToOccurrenceKind();
			_model.Circuits.Sensor.SuppressIsFull.Activation = SensorSupressIsFull.IsChecked.ToOccurrenceKind();
			_model.Circuits.Timer.StuckFault.Activation = TimerRelayStuck.IsChecked.ToOccurrenceKind();
		}

		private void UpdateModelState()
		{
			// Failures in Context menus and Indicators
			SuppressPumping.IsChecked = _model.Circuits.Pump.SuppressPumping.IsActivated;
			SwitchStuck.IsChecked = false; //TODO: = _model.Circuits.Switch..IsActivated;
			RelayK1Stuck.IsChecked = _model.Circuits.K1.StuckFault.IsActivated;
			RelayK2Stuck.IsChecked = _model.Circuits.K2.StuckFault.IsActivated;
			SensorRelayStuck.IsChecked = false; //TODO: _model.Circuits.Sensor..IsActivated;
			SensorSupressIsFull.IsChecked = _model.Circuits.Sensor.SuppressIsFull.IsActivated;
			TimerRelayStuck.IsChecked = _model.Circuits.Timer.StuckFault.IsActivated;
			
			TimerFailure.Visibility = (TimerRelayStuck.IsChecked).ToVisibility();
			SensorFailure.Visibility = (SensorRelayStuck.IsChecked || SensorSupressIsFull.IsChecked).ToVisibility();
			PumpFailure.Visibility = (SuppressPumping.IsChecked).ToVisibility();
			RelayK1Failure.Visibility = (RelayK1Stuck.IsChecked).ToVisibility();
			RelayK2Failure.Visibility = (RelayK2Stuck.IsChecked).ToVisibility();
			SwitchFailure.Visibility = (SwitchStuck.IsChecked).ToVisibility();
			
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