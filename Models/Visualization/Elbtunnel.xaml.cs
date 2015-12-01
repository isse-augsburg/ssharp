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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Media.Animation;
	using System.Windows.Shapes;
	using global::Elbtunnel;
	using global::Elbtunnel.Controllers;
	using global::Elbtunnel.Sensors;
	using global::Elbtunnel.Vehicles;
	using Infrastructure;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Reflection;

	public partial class Elbtunnel
	{
		private readonly EndControlOriginal _endControl;
		private readonly HeightControl _heightControl;
		private readonly Queue<Tuple<VisualizationVehicle, Canvas>> _highVehicles = new Queue<Tuple<VisualizationVehicle, Canvas>>();
		private readonly Storyboard _lb1Storyboard;
		private readonly Storyboard _lb2Storyboard;
		private readonly MainControlOriginal _mainControl;
		private readonly Storyboard _odfStoryboard;
		private readonly Storyboard _odlStoryboard;
		private readonly Storyboard _odrStoryboard;
		private readonly Queue<Tuple<VisualizationVehicle, Canvas>> _overHighVehicles = new Queue<Tuple<VisualizationVehicle, Canvas>>();
		private readonly PreControlOriginal _preControl;
		private readonly RealTimeSimulator _simulator;
		private readonly List<Tuple<VisualizationVehicle, Canvas>> _vehicles = new List<Tuple<VisualizationVehicle, Canvas>>();
		private Canvas _draggedVehicle;
		private Point _position;

		public Elbtunnel()
		{
			InitializeComponent();

			// Initialize visualization resources
			_lb1Storyboard = (Storyboard)Resources["EventLb1"];
			_lb2Storyboard = (Storyboard)Resources["EventLb2"];
			_odlStoryboard = (Storyboard)Resources["EventOdl"];
			_odrStoryboard = (Storyboard)Resources["EventOdr"];
			_odfStoryboard = (Storyboard)Resources["EventOdf"];

			// Initialize the simulation environment
			var specification = new Specification(Enumerable
				.Range(0, 9).Select(_ => new VisualizationVehicle(VehicleKind.Truck))
				.Concat(Enumerable.Range(0, 9).Select(_ => new VisualizationVehicle(VehicleKind.OverheightTruck)))
				.ToArray());
			var model = Model.Create(specification);
			foreach (var fault in model.GetFaults())
				fault.OccurrenceKind = OccurrenceKind.Never;

			_simulator = new RealTimeSimulator(model, 1000, specification.Collision);
			_simulator.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.SetSimulator(_simulator);
			SimulationControls.Reset += (o, e) => OnModelStateReset();

			// Extract the components
			_heightControl = (HeightControl)_simulator.Model.RootComponents[0];
			_preControl = (PreControlOriginal)_heightControl.PreControl;
			_mainControl = (MainControlOriginal)_heightControl.MainControl;
			_endControl = (EndControlOriginal)_heightControl.EndControl;

			var vehicles = (VehicleCollection)_simulator.Model.RootComponents[1];
			foreach (var vehicle in vehicles.Vehicles.Cast<VisualizationVehicle>())
			{
				if (vehicle.Kind == VehicleKind.Truck)
					_highVehicles.Enqueue(Tuple.Create(vehicle, CreateVehicleUIElement(vehicle.Kind, _highVehicles.Count + 1)));
				else
					_overHighVehicles.Enqueue(Tuple.Create(vehicle, CreateVehicleUIElement(vehicle.Kind, _overHighVehicles.Count + 1)));
			}

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

		private void OnModelStateReset()
		{
			_preControl.Detector.Misdetection.OccurrenceKind = MisdetectionLb1.IsChecked.ToOccurrenceKind();
			_mainControl.PositionDetector.Misdetection.OccurrenceKind = MisdetectionLb2.IsChecked.ToOccurrenceKind();
			_mainControl.LeftDetector.Misdetection.OccurrenceKind = MisdetectionOdl.IsChecked.ToOccurrenceKind();
			_mainControl.RightDetector.Misdetection.OccurrenceKind = MisdetectionOdr.IsChecked.ToOccurrenceKind();
			_endControl.Detector.Misdetection.OccurrenceKind = MisdetectionOdf.IsChecked.ToOccurrenceKind();

			foreach (var vehicle in _vehicles)
			{
				if (vehicle.Item1.Kind == VehicleKind.Truck)
					_highVehicles.Enqueue(vehicle);
				else
					_overHighVehicles.Enqueue(vehicle);

				LayoutRoot.Children.Remove(vehicle.Item2);
			}

			_vehicles.Clear();
			Message.Text = String.Empty;
			HazardIndicator.Visibility = Visibility.Collapsed;
		}

		private void UpdateModelState()
		{
			if (_preControl.Detector.IsVehicleDetected)
				_lb1Storyboard.Begin();

			if (_mainControl.PositionDetector.IsVehicleDetected)
				_lb2Storyboard.Begin();

			if (_mainControl.LeftDetector.IsVehicleDetected)
				_odlStoryboard.Begin();

			if (_mainControl.RightDetector.IsVehicleDetected)
				_odrStoryboard.Begin();

			if (_endControl.Detector.IsVehicleDetected)
				_odfStoryboard.Begin();

			MisdetectionLb1.IsChecked = _preControl.Detector.Misdetection.IsOccurring;
			MisdetectionLb2.IsChecked = _mainControl.PositionDetector.Misdetection.IsOccurring;
			MisdetectionOdl.IsChecked = _mainControl.LeftDetector.Misdetection.IsOccurring;
			MisdetectionOdr.IsChecked = _mainControl.RightDetector.Misdetection.IsOccurring;
			MisdetectionOdf.IsChecked = _endControl.Detector.Misdetection.IsOccurring;

			_preControl.Detector.FalseDetection.OccurrenceKind = OccurrenceKind.Never;
			_mainControl.PositionDetector.FalseDetection.OccurrenceKind = OccurrenceKind.Never;
			_mainControl.LeftDetector.FalseDetection.OccurrenceKind = OccurrenceKind.Never;
			_mainControl.RightDetector.FalseDetection.OccurrenceKind = OccurrenceKind.Never;
			_endControl.Detector.FalseDetection.OccurrenceKind = OccurrenceKind.Never;

			SetFaultAdornment(FaultLb1, _preControl.Detector);
			SetFaultAdornment(FaultLb2, _mainControl.PositionDetector);
			SetFaultAdornment(FaultOdl, _mainControl.LeftDetector);
			SetFaultAdornment(FaultOdr, _mainControl.RightDetector);
			SetFaultAdornment(FaultOdf, _endControl.Detector);

			var isMainControlActive = _mainControl.Count > 0;
			MainControlNumOhvLabel.Visibility = isMainControlActive.ToVisibility();
			MainControlNumOhv.Visibility = isMainControlActive.ToVisibility();
			MainControlTimeLabel.Visibility = isMainControlActive.ToVisibility();
			MainControlTime.Visibility = isMainControlActive.ToVisibility();
			MainControlNumOhv.Text = _mainControl.Count.ToString();
			MainControlTime.Text = _mainControl.Timer.RemainingTime.ToString();

			EndControlTimeLabel.Visibility = _endControl.IsActive.ToVisibility();
			EndControlTime.Visibility = _endControl.IsActive.ToVisibility();
			EndControlTime.Text = _endControl.Timer.RemainingTime.ToString();

			const double inactiveOpacity = 0.1;
			Lb2.Opacity = isMainControlActive ? 1 : inactiveOpacity;
			Odl.Opacity = isMainControlActive ? 1 : inactiveOpacity;
			Odr.Opacity = isMainControlActive ? 1 : inactiveOpacity;
			Odf.Opacity = _endControl.IsActive ? 1 : inactiveOpacity;

			foreach (var vehicle in _vehicles)
				UpdateVehicle(vehicle);

			if (_heightControl.TrafficLights.IsRed)
			{
				var falseAlarm = !_vehicles.Any(v => v.Item1.Lane == Lane.Left && v.Item1.Kind == VehicleKind.OverheightTruck);

				Message.Text = falseAlarm ? "False Alarm" : "Tunnel Closed";
				HazardIndicator.Visibility = falseAlarm.ToVisibility();
			}

			if (_vehicles.Any(v => v.Item1.Lane == Lane.Left && v.Item1.Position >= Specification.TunnelPosition && v.Item1.Kind == VehicleKind.OverheightTruck))
			{
				HazardIndicator.Visibility = Visibility.Visible;
				Message.Text = "Collision";
			}
		}

		private static void SetFaultAdornment(UIElement adornment, VehicleDetector detector)
		{
			var isOccurring = detector.Misdetection.IsOccurring || detector.FalseDetection.IsOccurring;
			adornment.Visibility = isOccurring.ToVisibility();
		}

		private void OnMisdetectionLb1(object sender, RoutedEventArgs e)
		{
			_preControl.Detector.Misdetection.ToggleOccurrence();
		}

		private void OnMisdetectionLb2(object sender, RoutedEventArgs e)
		{
			_mainControl.PositionDetector.Misdetection.ToggleOccurrence();
		}

		private void OnMisdetectionOdl(object sender, RoutedEventArgs e)
		{
			_mainControl.LeftDetector.Misdetection.ToggleOccurrence();
		}

		private void OnMisdetectionOdr(object sender, RoutedEventArgs e)
		{
			_mainControl.RightDetector.Misdetection.ToggleOccurrence();
		}

		private void OnMisdetectionOdf(object sender, RoutedEventArgs e)
		{
			_endControl.Detector.Misdetection.ToggleOccurrence();
		}

		private void OnFalseDetectionLb1(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				_preControl.Detector.FalseDetection.OccurrenceKind = OccurrenceKind.Always;
		}

		private void OnFalseDetectionLb2(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				_mainControl.PositionDetector.FalseDetection.OccurrenceKind = OccurrenceKind.Always;
		}

		private void OnFalseDetectionOdl(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				_mainControl.LeftDetector.FalseDetection.OccurrenceKind = OccurrenceKind.Always;
		}

		private void OnFalseDetectionOdr(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				_mainControl.RightDetector.FalseDetection.OccurrenceKind = OccurrenceKind.Always;
		}

		private void OnFalseDetectionOdf(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
				_endControl.Detector.FalseDetection.OccurrenceKind = OccurrenceKind.Always;
		}

		private static void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
		{
			var dx = e.TotalManipulation.Translation.X;
			var dy = e.TotalManipulation.Translation.Y;
			var vehicle = (VisualizationVehicle)((FrameworkElement)e.Source).DataContext;

			HandleVehicleInput(dx, dy, vehicle);
			e.Handled = true;
		}

		private static void HandleVehicleInput(double dx, double dy, VisualizationVehicle vehicle)
		{
			if (Math.Abs(dx) < 10 && Math.Abs(dy) < 10)
				return;

			if (Math.Abs(dx) > Math.Abs(dy))
				vehicle.NextSpeed += dx > 0 ? 1 : -1;
			else
				vehicle.NextLane = dy < 0 ? Lane.Left : Lane.Right;
		}

		private void SpawnOhvLeft(object sender, RoutedEventArgs e)
		{
			Spawn(_overHighVehicles, Lane.Left);
		}

		private void SpawnHvLeft(object sender, RoutedEventArgs e)
		{
			Spawn(_highVehicles, Lane.Left);
		}

		private void SpawnOhvRight(object sender, RoutedEventArgs e)
		{
			Spawn(_overHighVehicles, Lane.Right);
		}

		private void SpawnHvRight(object sender, RoutedEventArgs e)
		{
			Spawn(_highVehicles, Lane.Right);
		}

		private void Spawn(Queue<Tuple<VisualizationVehicle, Canvas>> vehicles, Lane lane)
		{
			if (vehicles.Count == 0 || _simulator.State == SimulationState.Stopped)
				return;

			var vehicle = vehicles.Dequeue();
			LayoutRoot.Children.Add(vehicle.Item2);

			vehicle.Item1.NextLane = lane;
			vehicle.Item1.Update();
			vehicle.Item1.NextSpeed = 1;

			_vehicles.Add(vehicle);
			UpdateVehicle(vehicle);
		}

		private static void UpdateVehicle(Tuple<VisualizationVehicle, Canvas> vehicle)
		{
			var leftLane = vehicle.Item1.Kind != VehicleKind.OverheightTruck ? 223 : 225;
			var rightLane = vehicle.Item1.Kind != VehicleKind.OverheightTruck ? 302 : 304;

			Canvas.SetLeft(vehicle.Item2, vehicle.Item1.Position * 35);
			Canvas.SetTop(vehicle.Item2, vehicle.Item1.Lane == Lane.Left ? leftLane : rightLane);
		}

		private Canvas CreateVehicleUIElement(VehicleKind kind, int index)
		{
			var width = kind == VehicleKind.OverheightTruck ? 50 : 35;
			var height = kind == VehicleKind.OverheightTruck ? 25 : 30;

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
						Fill = new SolidColorBrush(kind == VehicleKind.OverheightTruck ? Colors.DarkRed : Colors.BlueViolet)
					},
					new TextBlock
					{
						Width = width,
						Height = height,
						TextAlignment = TextAlignment.Center,
						Text = (kind == VehicleKind.OverheightTruck ? "OHV" : "HV") + index
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