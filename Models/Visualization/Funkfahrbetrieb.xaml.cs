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
	using System.Windows;
	using System.Windows.Controls;
	using global::Funkfahrbetrieb;
	using global::Funkfahrbetrieb.Context;
	using global::Funkfahrbetrieb.CrossingController;
	using global::Funkfahrbetrieb.TrainController;
	using Infrastructure;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Reflection;

	public partial class Funkfahrbetrieb
	{
		private readonly Barrier _barrier;
		private readonly RadioChannel _channel;
		private readonly CrossingControl _crossingControl;
		private readonly RealTimeSimulator _simulator;
		private readonly Train _train;
		private readonly TrainControl _trainControl;
		private bool _hazard;

		public Funkfahrbetrieb()
		{
			InitializeComponent();

			// Initialize the simulation environment
			var specification = new Specification();
			var model = Model.Create(specification);
			foreach (var fault in model.GetFaults())
				fault.OccurrenceKind = OccurrenceKind.Never;

			_simulator = new RealTimeSimulator(model, 1000, specification.PossibleCollision);
			_simulator.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.SetSimulator(_simulator);
			SimulationControls.Reset += (o, e) => OnModelStateReset();

			// Extract the components
			_train = (Train)_simulator.Model.RootComponents[2];
			_barrier = (Barrier)_simulator.Model.RootComponents[3];
			_crossingControl = (CrossingControl)_simulator.Model.RootComponents[0];
			_trainControl = (TrainControl)_simulator.Model.RootComponents[1];
			_channel = (RadioChannel)_simulator.Model.RootComponents[4];

			// Initialize the visualization state
			UpdateModelState();
			SimulationControls.ChangeSpeed(32);
		}

		private void OnModelStateReset()
		{
			_hazard = false;
			LastMessage.Text = "None";

			_trainControl.Brakes.BrakesFailure.OccurrenceKind = FaultBrakes.IsChecked.ToOccurrenceKind();
			_trainControl.Odometer.OdometerPositionOffset.OccurrenceKind = FaultOdometerPosition.IsChecked.ToOccurrenceKind();
			_trainControl.Odometer.OdometerSpeedOffset.OccurrenceKind = FaultOdometerSpeed.IsChecked.ToOccurrenceKind();
			_crossingControl.Sensor.BarrierSensorFailure.OccurrenceKind = FaultBarrierSensor.IsChecked.ToOccurrenceKind();
			_crossingControl.Motor.BarrierMotorStuck.OccurrenceKind = FaultBarrierMotor.IsChecked.ToOccurrenceKind();
			_crossingControl.TrainSensor.ErroneousTrainDetection.OccurrenceKind = FaultTrainSensor.IsChecked.ToOccurrenceKind();
			_channel.MessageDropped.OccurrenceKind = FaultMessage.IsChecked.ToOccurrenceKind();
		}

		private void UpdateModelState()
		{
			Canvas.SetLeft(Train, _train.Position / 2.0 - Train.Width + Train.Width);
			Canvas.SetLeft(Barrier, Specification.CrossingPosition / 2.0 + Train.Width);

			BarrierRotation.Angle = _barrier.Angle * 8;
			TrainPosition.Text = _train.Position.ToString();
			LastMessage.Text = _channel.Receive().ToString();

			FaultBrakes.IsChecked = _trainControl.Brakes.BrakesFailure.IsOccurring;
			FaultOdometerPosition.IsChecked = _trainControl.Odometer.OdometerPositionOffset.IsOccurring;
			FaultOdometerSpeed.IsChecked = _trainControl.Odometer.OdometerSpeedOffset.IsOccurring;
			FaultBarrierSensor.IsChecked = _crossingControl.Sensor.BarrierSensorFailure.IsOccurring;
			FaultBarrierMotor.IsChecked = _crossingControl.Motor.BarrierMotorStuck.IsOccurring;
			FaultTrainSensor.IsChecked = _crossingControl.TrainSensor.ErroneousTrainDetection.IsOccurring;
			FaultMessage.IsChecked = _channel.MessageDropped.IsOccurring;

			_hazard |= _simulator.Model.CheckStateLabel(0);
			Collision.Visibility = _hazard.ToVisibility();

			MessageFailure.Visibility = FaultMessage.IsChecked.ToVisibility();
			TrainFailure.Visibility = (FaultBrakes.IsChecked || FaultOdometerPosition.IsChecked || FaultOdometerSpeed.IsChecked).ToVisibility();
			CrossingFailure.Visibility = (FaultBarrierSensor.IsChecked || FaultBarrierMotor.IsChecked || FaultTrainSensor.IsChecked).ToVisibility();
		}

		private void OnBrakesFailure(object sender, RoutedEventArgs e)
		{
			_trainControl.Brakes.BrakesFailure.ToggleOccurrence();
		}

		private void OnPositionOffset(object sender, RoutedEventArgs e)
		{
			_trainControl.Odometer.OdometerPositionOffset.ToggleOccurrence();
		}

		private void OnSpeedOffset(object sender, RoutedEventArgs e)
		{
			_trainControl.Odometer.OdometerSpeedOffset.ToggleOccurrence();
		}

		private void OnBarrierSensorFailure(object sender, RoutedEventArgs e)
		{
			_crossingControl.Sensor.BarrierSensorFailure.ToggleOccurrence();
		}

		private void OnBarrierMotorFailure(object sender, RoutedEventArgs e)
		{
			_crossingControl.Motor.BarrierMotorStuck.ToggleOccurrence();
		}

		private void OnTrainDetected(object sender, RoutedEventArgs e)
		{
			_crossingControl.TrainSensor.ErroneousTrainDetection.ToggleOccurrence();
		}

		private void OnDropMessages(object sender, RoutedEventArgs e)
		{
			_channel.MessageDropped.ToggleOccurrence();
		}
	}
}