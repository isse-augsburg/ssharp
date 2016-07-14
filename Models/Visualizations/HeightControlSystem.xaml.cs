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
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Animation;
	using System.Windows.Shapes;
	using HeightControl.Modeling;
	using HeightControl.Modeling.Controllers;
	using HeightControl.Modeling.Sensors;
	using HeightControl.Modeling.Vehicles;
	using Infrastructure;
	using Modeling;

	public partial class HeightControlSystem
	{
		private readonly Storyboard _lb1Storyboard;
		private readonly Storyboard _lb2Storyboard;
		private readonly Storyboard _odfStoryboard;
		private readonly Storyboard _odlStoryboard;
		private readonly Storyboard _odrStoryboard;
		private readonly List<Tuple<Vehicle, Canvas, bool>> _vehicles = new List<Tuple<Vehicle, Canvas, bool>>();
		private Canvas _draggedVehicle;
		private Point _position;

		public HeightControlSystem()
		{
			InitializeComponent();

			// Initialize visualization resources
			_lb1Storyboard = (Storyboard)Resources["EventLb1"];
			_lb2Storyboard = (Storyboard)Resources["EventLb2"];
			_odlStoryboard = (Storyboard)Resources["EventOdl"];
			_odrStoryboard = (Storyboard)Resources["EventOdr"];
			_odfStoryboard = (Storyboard)Resources["EventOdf"];

			// Initialize the simulation environment
			var specification = new Model(
				new PreControlOriginal(),
				new MainControlOriginal(),
				new EndControlOriginal(),
				Enumerable
					.Range(0, 9).Select(_ => new VisualizationVehicle { Kind = VehicleKind.HighVehicle })
					.Concat(Enumerable.Range(0, 9).Select(_ => new VisualizationVehicle { Kind = VehicleKind.OverheightVehicle }))
					.ToArray());

			SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.Reset += (o, e) => OnModelStateReset();
			SimulationControls.Rewound += (o, e) => OnRewound();
			SimulationControls.SetModel(specification);

			// Initialize the visualization state
			OnModelStateReset();
			UpdateModelState();
			SimulationControls.ChangeSpeed(2);

			AlertLb1.Opacity = 0;
			AlertLb2.Opacity = 0;
			AlertOdl.Opacity = 0;
			AlertOdr.Opacity = 0;
			AlertOdf.Opacity = 0;
		}

		private Vehicle[] Vehicles => ((Model)SimulationControls.Model).Vehicles;
		private EndControlOriginal EndControl => (EndControlOriginal)HeightControl.EndControl;
		private HeightControl HeightControl => ((Model)SimulationControls.Model).HeightControl;
		private MainControlOriginal MainControl => (MainControlOriginal)HeightControl.MainControl;
		private PreControl PreControl => HeightControl.PreControl;

		private void OnModelStateReset()
		{
			foreach (var vehicle in _vehicles)
				LayoutRoot.Children.Remove(vehicle.Item2);

			foreach (var vehicle in _vehicles.Select(v => v.Item1).OfType<VisualizationVehicle>())
				vehicle.NextSpeed = 0;

			_vehicles.Clear();

			var highCount = 0;
			var overHighCount = 0;
			foreach (var vehicle in Vehicles)
			{
				int count;
				if (vehicle.Kind == VehicleKind.OverheightVehicle)
				{
					++overHighCount;
					count = overHighCount;
				}
				else
				{
					++highCount;
					count = highCount;
				}

				var panel = CreateVehicleUIElement(vehicle.Kind, count);
				var info = Tuple.Create(vehicle, panel, false);
				_vehicles.Add(info);

				LayoutRoot.Children.Add(panel);
				UpdateVehicle(info);
			}

			OnRewound();

			if (SimulationControls.Simulator.IsReplay)
				return;

			PreControl.PositionDetector.Misdetection.Activation = MisdetectionLb1.IsChecked.ToOccurrenceKind();
			MainControl.PositionDetector.Misdetection.Activation = MisdetectionLb2.IsChecked.ToOccurrenceKind();
			MainControl.LeftDetector.Misdetection.Activation = MisdetectionOdl.IsChecked.ToOccurrenceKind();
			MainControl.RightDetector.Misdetection.Activation = MisdetectionOdr.IsChecked.ToOccurrenceKind();
			EndControl.LeftDetector.Misdetection.Activation = MisdetectionOdf.IsChecked.ToOccurrenceKind();
		}

		private void OnRewound()
		{
			Message.Text = String.Empty;
			HazardIndicator.Visibility = Visibility.Collapsed;
		}

		private void UpdateModelState()
		{
			if (PreControl.PositionDetector.IsVehicleDetected)
				_lb1Storyboard.Begin();

			if (MainControl.PositionDetector.IsVehicleDetected)
				_lb2Storyboard.Begin();

			if (MainControl.LeftDetector.IsVehicleDetected)
				_odlStoryboard.Begin();

			if (MainControl.RightDetector.IsVehicleDetected)
				_odrStoryboard.Begin();

			if (EndControl.LeftDetector.IsVehicleDetected)
				_odfStoryboard.Begin();

			MisdetectionLb1.IsChecked = PreControl.PositionDetector.Misdetection.IsActivated;
			MisdetectionLb2.IsChecked = MainControl.PositionDetector.Misdetection.IsActivated;
			MisdetectionOdl.IsChecked = MainControl.LeftDetector.Misdetection.IsActivated;
			MisdetectionOdr.IsChecked = MainControl.RightDetector.Misdetection.IsActivated;
			MisdetectionOdf.IsChecked = EndControl.LeftDetector.Misdetection.IsActivated;

			if (!SimulationControls.ReplayingCounterExample)
			{
				PreControl.PositionDetector.FalseDetection.Activation = Activation.Suppressed;
				MainControl.PositionDetector.FalseDetection.Activation = Activation.Suppressed;
				MainControl.LeftDetector.FalseDetection.Activation = Activation.Suppressed;
				MainControl.RightDetector.FalseDetection.Activation = Activation.Suppressed;
				EndControl.LeftDetector.FalseDetection.Activation = Activation.Suppressed;
			}

			SetFaultAdornment(FaultLb1, PreControl.PositionDetector);
			SetFaultAdornment(FaultLb2, MainControl.PositionDetector);
			SetFaultAdornment(FaultOdl, MainControl.LeftDetector);
			SetFaultAdornment(FaultOdr, MainControl.RightDetector);
			SetFaultAdornment(FaultOdf, EndControl.LeftDetector);

			var isMainControlActive = MainControl.Count > 0;
			MainControlNumOhvLabel.Visibility = isMainControlActive.ToVisibility();
			MainControlNumOhv.Visibility = isMainControlActive.ToVisibility();
			MainControlTimeLabel.Visibility = isMainControlActive.ToVisibility();
			MainControlTime.Visibility = isMainControlActive.ToVisibility();
			MainControlNumOhv.Text = MainControl.Count.ToString();
			MainControlTime.Text = MainControl.Timer.RemainingTime.ToString();

			EndControlTimeLabel.Visibility = EndControl.IsActive.ToVisibility();
			EndControlTime.Visibility = EndControl.IsActive.ToVisibility();
			EndControlTime.Text = EndControl.Timer.RemainingTime.ToString();

			const double inactiveOpacity = 0.1;
			Lb2.Opacity = isMainControlActive ? 1 : inactiveOpacity;
			Odl.Opacity = isMainControlActive ? 1 : inactiveOpacity;
			Odr.Opacity = isMainControlActive ? 1 : inactiveOpacity;
			Odf.Opacity = EndControl.IsActive ? 1 : inactiveOpacity;

			foreach (var vehicle in _vehicles)
				UpdateVehicle(vehicle);

			if (HeightControl.TrafficLights.IsRed)
			{
				var falseAlarm = !_vehicles.Any(v => v.Item1.Lane == Lane.Left && v.Item1.Kind == VehicleKind.OverheightVehicle);

				Message.Text = falseAlarm ? "False Alarm" : "Tunnel Closed";
				HazardIndicator.Visibility = falseAlarm.ToVisibility();
			}

			if (_vehicles.Any(
				v => v.Item1.Lane == Lane.Left && v.Item1.Position >= Model.TunnelPosition && v.Item1.Kind == VehicleKind.OverheightVehicle))
			{
				HazardIndicator.Visibility = Visibility.Visible;
				Message.Text = "Collision";
			}
		}

		private static void SetFaultAdornment(UIElement adornment, VehicleDetector detector)
		{
			var isOccurring = detector.Misdetection.IsActivated || detector.FalseDetection.IsActivated;
			adornment.Visibility = isOccurring.ToVisibility();
		}

		private void OnMisdetectionLb1(object sender, RoutedEventArgs e)
		{
			PreControl.PositionDetector.Misdetection.ToggleActivationMode();
		}

		private void OnMisdetectionLb2(object sender, RoutedEventArgs e)
		{
			MainControl.PositionDetector.Misdetection.ToggleActivationMode();
		}

		private void OnMisdetectionOdl(object sender, RoutedEventArgs e)
		{
			MainControl.LeftDetector.Misdetection.ToggleActivationMode();
		}

		private void OnMisdetectionOdr(object sender, RoutedEventArgs e)
		{
			MainControl.RightDetector.Misdetection.ToggleActivationMode();
		}

		private void OnMisdetectionOdf(object sender, RoutedEventArgs e)
		{
			EndControl.LeftDetector.Misdetection.ToggleActivationMode();
		}

		private void OnFalseDetectionLb1(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				PreControl.PositionDetector.FalseDetection.Activation = Activation.Forced;
		}

		private void OnFalseDetectionLb2(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				MainControl.PositionDetector.FalseDetection.Activation = Activation.Forced;
		}

		private void OnFalseDetectionOdl(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				MainControl.LeftDetector.FalseDetection.Activation = Activation.Forced;
		}

		private void OnFalseDetectionOdr(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				MainControl.RightDetector.FalseDetection.Activation = Activation.Forced;
		}

		private void OnFalseDetectionOdf(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				EndControl.LeftDetector.FalseDetection.Activation = Activation.Forced;
		}

		private static void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
		{
			var dx = e.TotalManipulation.Translation.X;
			var dy = e.TotalManipulation.Translation.Y;
			var vehicle = (VisualizationVehicle)((FrameworkElement)e.Source).DataContext;

			HandleVehicleInput(dx, dy, vehicle);
			e.Handled = true;
		}

		private static void HandleVehicleInput(double dx, double dy, Vehicle vehicle)
		{
			if (Math.Abs(dx) < 10 && Math.Abs(dy) < 10)
				return;

			var visualizationVehicle = (VisualizationVehicle)vehicle;
			if (Math.Abs(dx) > Math.Abs(dy))
				visualizationVehicle.NextSpeed += dx > 0 ? 1 : -1;
			else
				visualizationVehicle.NextLane = dy < 0 ? Lane.Left : Lane.Right;
		}

		private void SpawnOhvLeft(object sender, RoutedEventArgs e)
		{
			Spawn(VehicleKind.OverheightVehicle, Lane.Left);
		}

		private void SpawnHvLeft(object sender, RoutedEventArgs e)
		{
			Spawn(VehicleKind.HighVehicle, Lane.Left);
		}

		private void SpawnOhvRight(object sender, RoutedEventArgs e)
		{
			Spawn(VehicleKind.OverheightVehicle, Lane.Right);
		}

		private void SpawnHvRight(object sender, RoutedEventArgs e)
		{
			Spawn(VehicleKind.HighVehicle, Lane.Right);
		}

		private void Spawn(VehicleKind kind, Lane lane)
		{
			var vehicle = _vehicles.FirstOrDefault(v => v.Item1 is VisualizationVehicle && v.Item1.Kind == kind && !v.Item3);

			if (vehicle == null)
				return;

			var visualizationVehicle = (VisualizationVehicle)vehicle.Item1;
			visualizationVehicle.NextLane = lane;
			visualizationVehicle.Update();
			visualizationVehicle.NextSpeed = 1;

			_vehicles.Remove(vehicle);
			_vehicles.Add(Tuple.Create(vehicle.Item1, vehicle.Item2, true));
		}

		private static void UpdateVehicle(Tuple<Vehicle, Canvas, bool> vehicle)
		{
			if (vehicle.Item1.Position == 0)
			{
				Canvas.SetLeft(vehicle.Item2, -100000);
				return;
			}

			var leftLane = vehicle.Item1.Kind != VehicleKind.OverheightVehicle ? 225 : 223;
			var rightLane = vehicle.Item1.Kind != VehicleKind.OverheightVehicle ? 304 : 302;

			Canvas.SetLeft(vehicle.Item2, vehicle.Item1.Position * 60 - vehicle.Item2.Width * 1.5);
			Canvas.SetTop(vehicle.Item2, vehicle.Item1.Lane == Lane.Left ? leftLane : rightLane);
		}

		private Canvas CreateVehicleUIElement(VehicleKind kind, int index)
		{
			var width = kind == VehicleKind.OverheightVehicle ? 50 : 35;
			var height = kind == VehicleKind.OverheightVehicle ? 30 : 25;

			var canvas = new Canvas
			{
				DataContext = this,
				Width = width,
				Height = height,
				Children =
				{
					new Rectangle
					{
						Width = width,
						Height = height,
						Fill = new SolidColorBrush(kind == VehicleKind.OverheightVehicle ? Colors.DarkRed : Colors.BlueViolet)
					},
					new TextBlock
					{
						Width = width,
						Height = height,
						TextAlignment = TextAlignment.Center,
						Text = (kind == VehicleKind.OverheightVehicle ? "OHV" : "HV") + index
					}
				}
			};

			Panel.SetZIndex(canvas, -1);
			canvas.ManipulationCompleted += OnManipulationCompleted;
			canvas.PreviewMouseDown += OnVehicleMouseDown;
			canvas.PreviewMouseUp += OnVehicleMouseUp;

			return canvas;
		}

		private void OnVehicleMouseDown(object sender, MouseButtonEventArgs e)
		{
			_draggedVehicle = (Canvas)sender;
			_position = e.GetPosition(this);

			e.Handled = true;
			_draggedVehicle.CaptureMouse();
		}

		private void OnVehicleMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (_draggedVehicle == null)
				return;

			var position = e.GetPosition(this);
			var dx = position.X - _position.X;
			var dy = position.Y - _position.Y;

			var vehicle = _vehicles.Where(v => Equals(v.Item2, _draggedVehicle)).Select(v => v.Item1).Single();

			HandleVehicleInput(dx, dy, vehicle);
			e.Handled = true;

			_draggedVehicle.ReleaseMouseCapture();
			_draggedVehicle = null;
		}
	}
}