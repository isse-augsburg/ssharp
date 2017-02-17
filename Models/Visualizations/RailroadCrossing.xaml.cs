// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using System.Windows.Controls;
	using CaseStudies.RailroadCrossing.Modeling;
	using CaseStudies.RailroadCrossing.Modeling.Plants;
	using CaseStudies.RailroadCrossing.Modeling.Controllers;
	using Infrastructure;
	using Modeling;
	using Analysis;
	using ISSE.SafetyChecking.Formula;

	public partial class RailroadCrossing
	{
		private bool _hazard;
		private Func<bool> _hazardFormula;

		public RailroadCrossing()
		{
			InitializeComponent();

			// Initialize the simulation environment
			var model = new Model();

			SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.Reset += (o, e) => OnModelStateReset();
			SimulationControls.Rewound += (o, e) => OnRewound();
			SimulationControls.SetModel(model, model.PossibleCollision);

			// Initialize the visualization state
			UpdateModelState();
			SimulationControls.ChangeSpeed(16);
		}

		private Barrier Barrier => ((Model)SimulationControls.Model).Barrier;
		private RadioChannel Channel => ((Model)SimulationControls.Model).Channel;
		private CrossingController CrossingController => ((Model)SimulationControls.Model).CrossingController;
		private Train Train => ((Model)SimulationControls.Model).Train;
		private TrainController TrainController => ((Model)SimulationControls.Model).TrainController;

		private void OnModelStateReset()
		{
			OnRewound();
			_hazardFormula = SimulationControls.RuntimeModel.Compile(((Model)SimulationControls.Model).PossibleCollision);

			if (SimulationControls.Simulator.IsReplay)
				return;

			TrainController.Brakes.BrakesFailure.Activation = FaultBrakes.IsChecked.ToOccurrenceKind();
			TrainController.Odometer.OdometerPositionOffset.Activation = FaultOdometerPosition.IsChecked.ToOccurrenceKind();
			TrainController.Odometer.OdometerSpeedOffset.Activation = FaultOdometerSpeed.IsChecked.ToOccurrenceKind();
			CrossingController.Sensor.BarrierSensorFailure.Activation = FaultBarrierSensor.IsChecked.ToOccurrenceKind();
			CrossingController.Motor.BarrierMotorStuck.Activation = FaultBarrierMotor.IsChecked.ToOccurrenceKind();
			CrossingController.TrainSensor.ErroneousTrainDetection.Activation = FaultTrainSensor.IsChecked.ToOccurrenceKind();
			Channel.MessageDropped.Activation = FaultMessage.IsChecked.ToOccurrenceKind();
		}

		private void OnRewound()
		{
			_hazard = false;
			Collision.Visibility = _hazard.ToVisibility();
			LastMessage.Text = "None";
		}

		private void UpdateModelState()
		{
			Canvas.SetLeft(TrainElement, Train.Position / 2.0 - TrainElement.Width + TrainElement.Width);
			Canvas.SetLeft(BarrierElement, Model.CrossingPosition / 2.0 + TrainElement.Width);
			Canvas.SetLeft(DangerSpot, Model.CrossingPosition / 2.0 + DangerSpot.Width);

			BarrierRotation.Angle = Barrier.Angle * 8;
			TrainPosition.Text = Train.Position.ToString();
			LastMessage.Text = Channel.Receive().ToString();

			FaultBrakes.IsChecked = TrainController.Brakes.BrakesFailure.IsActivated;
			FaultOdometerPosition.IsChecked = TrainController.Odometer.OdometerPositionOffset.IsActivated;
			FaultOdometerSpeed.IsChecked = TrainController.Odometer.OdometerSpeedOffset.IsActivated;
			FaultBarrierSensor.IsChecked = CrossingController.Sensor.BarrierSensorFailure.IsActivated;
			FaultBarrierMotor.IsChecked = CrossingController.Motor.BarrierMotorStuck.IsActivated;
			FaultTrainSensor.IsChecked = CrossingController.TrainSensor.ErroneousTrainDetection.IsActivated;
			FaultMessage.IsChecked = Channel.MessageDropped.IsActivated;

			_hazard |= _hazardFormula?.Invoke() ?? false;
			Collision.Visibility = _hazard.ToVisibility();

			MessageFailure.Visibility = FaultMessage.IsChecked.ToVisibility();
			TrainFailure.Visibility = (FaultBrakes.IsChecked || FaultOdometerPosition.IsChecked || FaultOdometerSpeed.IsChecked).ToVisibility();
			CrossingFailure.Visibility = (FaultBarrierSensor.IsChecked || FaultBarrierMotor.IsChecked || FaultTrainSensor.IsChecked).ToVisibility();
		}

		private void OnBrakesFailure(object sender, RoutedEventArgs e)
		{
			TrainController.Brakes.BrakesFailure.ToggleActivationMode();
		}

		private void OnPositionOffset(object sender, RoutedEventArgs e)
		{
			TrainController.Odometer.OdometerPositionOffset.ToggleActivationMode();
		}

		private void OnSpeedOffset(object sender, RoutedEventArgs e)
		{
			TrainController.Odometer.OdometerSpeedOffset.ToggleActivationMode();
		}

		private void OnBarrierSensorFailure(object sender, RoutedEventArgs e)
		{
			CrossingController.Sensor.BarrierSensorFailure.ToggleActivationMode();
		}

		private void OnBarrierMotorFailure(object sender, RoutedEventArgs e)
		{
			CrossingController.Motor.BarrierMotorStuck.ToggleActivationMode();
		}

		private void OnTrainDetected(object sender, RoutedEventArgs e)
		{
			CrossingController.TrainSensor.ErroneousTrainDetection.ToggleActivationMode();
		}

		private void OnDropMessages(object sender, RoutedEventArgs e)
		{
			Channel.MessageDropped.ToggleActivationMode();
		}
	}
}