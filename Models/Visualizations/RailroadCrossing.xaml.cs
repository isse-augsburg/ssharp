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
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using CaseStudies.RailroadCrossing;
	using CaseStudies.RailroadCrossing.ModelElements.Context;
	using CaseStudies.RailroadCrossing.ModelElements.CrossingController;
	using CaseStudies.RailroadCrossing.ModelElements.TrainController;
	using Infrastructure;
	using Modeling;

	public partial class RailroadCrossing
	{
		private bool _hazard;

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

		private Barrier Barrier => SimulationControls.Model.RootComponents.OfType<Barrier>().Single();
		private RadioChannel Channel => SimulationControls.Model.RootComponents.OfType<RadioChannel>().Single();
		private CrossingControl CrossingControl => SimulationControls.Model.RootComponents.OfType<CrossingControl>().Single();
		private Train Train => SimulationControls.Model.RootComponents.OfType<Train>().Single();
		private TrainControl TrainControl => SimulationControls.Model.RootComponents.OfType<TrainControl>().Single();

		private void OnModelStateReset()
		{
			OnRewound();

			if (SimulationControls.Simulator.IsReplay)
				return;

			TrainControl.Brakes.BrakesFailure.Activation = FaultBrakes.IsChecked.ToOccurrenceKind();
			TrainControl.Odometer.OdometerPositionOffset.Activation = FaultOdometerPosition.IsChecked.ToOccurrenceKind();
			TrainControl.Odometer.OdometerSpeedOffset.Activation = FaultOdometerSpeed.IsChecked.ToOccurrenceKind();
			CrossingControl.Sensor.BarrierSensorFailure.Activation = FaultBarrierSensor.IsChecked.ToOccurrenceKind();
			CrossingControl.Motor.BarrierMotorStuck.Activation = FaultBarrierMotor.IsChecked.ToOccurrenceKind();
			CrossingControl.TrainSensor.ErroneousTrainDetection.Activation = FaultTrainSensor.IsChecked.ToOccurrenceKind();
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

			FaultBrakes.IsChecked = TrainControl.Brakes.BrakesFailure.IsActivated;
			FaultOdometerPosition.IsChecked = TrainControl.Odometer.OdometerPositionOffset.IsActivated;
			FaultOdometerSpeed.IsChecked = TrainControl.Odometer.OdometerSpeedOffset.IsActivated;
			FaultBarrierSensor.IsChecked = CrossingControl.Sensor.BarrierSensorFailure.IsActivated;
			FaultBarrierMotor.IsChecked = CrossingControl.Motor.BarrierMotorStuck.IsActivated;
			FaultTrainSensor.IsChecked = CrossingControl.TrainSensor.ErroneousTrainDetection.IsActivated;
			FaultMessage.IsChecked = Channel.MessageDropped.IsActivated;

			_hazard |= SimulationControls.Model.CheckStateFormula(0);
			Collision.Visibility = _hazard.ToVisibility();

			MessageFailure.Visibility = FaultMessage.IsChecked.ToVisibility();
			TrainFailure.Visibility = (FaultBrakes.IsChecked || FaultOdometerPosition.IsChecked || FaultOdometerSpeed.IsChecked).ToVisibility();
			CrossingFailure.Visibility = (FaultBarrierSensor.IsChecked || FaultBarrierMotor.IsChecked || FaultTrainSensor.IsChecked).ToVisibility();
		}

		private void OnBrakesFailure(object sender, RoutedEventArgs e)
		{
			TrainControl.Brakes.BrakesFailure.ToggleActivationMode();
		}

		private void OnPositionOffset(object sender, RoutedEventArgs e)
		{
			TrainControl.Odometer.OdometerPositionOffset.ToggleActivationMode();
		}

		private void OnSpeedOffset(object sender, RoutedEventArgs e)
		{
			TrainControl.Odometer.OdometerSpeedOffset.ToggleActivationMode();
		}

		private void OnBarrierSensorFailure(object sender, RoutedEventArgs e)
		{
			CrossingControl.Sensor.BarrierSensorFailure.ToggleActivationMode();
		}

		private void OnBarrierMotorFailure(object sender, RoutedEventArgs e)
		{
			CrossingControl.Motor.BarrierMotorStuck.ToggleActivationMode();
		}

		private void OnTrainDetected(object sender, RoutedEventArgs e)
		{
			CrossingControl.TrainSensor.ErroneousTrainDetection.ToggleActivationMode();
		}

		private void OnDropMessages(object sender, RoutedEventArgs e)
		{
			Channel.MessageDropped.ToggleActivationMode();
		}
	}
}