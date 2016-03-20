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

namespace SafetySharp.CaseStudies.HeightControl
{
	using System.Linq;
	using Actuators;
	using Analysis;
	using Controllers;
	using Modeling;
	using Sensors;
	using Vehicles;

	/// <summary>
	///   Represents the specification of the Elbtunnel case study.
	/// </summary>
	public class DesignImprovedDetectionOfPreControl_Specification : ModelBase
	{
		public const int PreControlPosition = 3;
		public const int MainControlPosition = 6;
		public const int EndControlPosition = 9;
		public const int Timeout = 4;
		public const int TunnelPosition = 12;
		public const int MaxSpeed = 2;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public DesignImprovedDetectionOfPreControl_Specification(Vehicle[] vehicles = null)
		{
			vehicles = vehicles ?? new[]
			{
				new Vehicle { Kind = VehicleKind.OverheightTruck},
				new Vehicle { Kind = VehicleKind.OverheightTruck},
				new Vehicle { Kind = VehicleKind.Truck}
			};

			var lightBarrier1 = new LightBarrier
			{
				Position = PreControlPosition,
				Misdetection = { Name = "MissDetectionLB1" },
				FalseDetection = { Name = "FalseDetectionLB1" },
				VehicleCount = vehicles.Length
			};

			var preDetectorLeft = new OverheadDetector
			{
				Lane = Lane.Left,
				Position = PreControlPosition,
				Misdetection = { Name = "MissDetectionPODL" },
				FalseDetection = { Name = "FalseDetectionPODL" },
				VehicleCount = vehicles.Length
			};

			var preDetectorRight = new OverheadDetector
			{
				Lane = Lane.Right,
				Position = PreControlPosition,
				Misdetection = { Name = "MissDetectionPODR" },
				FalseDetection = { Name = "FalseDetectionPODR" },
				VehicleCount = vehicles.Length
			};

			var lightBarrier2 = new LightBarrier
			{
				Position = MainControlPosition,
				Misdetection = { Name = "MissDetectionLB2" },
				FalseDetection = { Name = "FalseDetectionLB2" },
				VehicleCount = vehicles.Length
			};

			var detectorLeft = new OverheadDetector
			{
				Lane = Lane.Left,
				Position = MainControlPosition,
				Misdetection = { Name = "MissDetectionODL" },
				FalseDetection = { Name = "FalseDetectionODL" },
				VehicleCount = vehicles.Length
			};

			var detectorRight = new OverheadDetector
			{
				Lane = Lane.Right,
				Position = MainControlPosition,
				Misdetection = { Name = "MissDetectionODR" },
				FalseDetection = { Name = "FalseDetectionODR" },
				VehicleCount = vehicles.Length
			};

			var detectorFinal = new OverheadDetector
			{
				Lane = Lane.Left,
				Position = EndControlPosition,
				Misdetection = { Name = "MissDetectionODF" },
				FalseDetection = { Name = "FalseDetectionODF" },
				VehicleCount = vehicles.Length
			};

			HeightControl = new HeightControl
			{
				PreControl = new PreControlImprovedDetection
				{
					PositionDetector = lightBarrier1,
					LeftDetector = preDetectorLeft,
					RightDetector = preDetectorRight
				},
				MainControl = new MainControlOriginal
				{
					LeftDetector = detectorLeft,
					RightDetector = detectorRight,
					PositionDetector = lightBarrier2,
					Timer = new Timer()
				},
				EndControl = new EndControlOriginal
				{
					Detector = detectorFinal,
					Timer = new Timer()
				},
				TrafficLights = new TrafficLights()
			};

			Vehicles = new VehicleCollection(vehicles);

			Component.Bind(nameof(Vehicles.IsTunnelClosed), nameof(HeightControl.TrafficLights.IsRed));

			Bind(lightBarrier1);
			Bind(preDetectorLeft);
			Bind(preDetectorRight);
			Bind(lightBarrier2);
			Bind(detectorLeft);
			Bind(detectorRight);
			Bind(detectorFinal);
		}

		/// <summary>
		///   Gets the height control that monitors the vehicles and closes the tunnel, if necessary.
		/// </summary>
		[Root(Role.System)]
		public HeightControl HeightControl { get; }

		/// <summary>
		///   Gets the monitored vehicles.
		/// </summary>
		[Root(Role.Environment)]
		public VehicleCollection Vehicles { get; }

		/// <summary>
		///   Represents the hazard of an over-height vehicle colliding with the tunnel entrance on the left lane.
		/// </summary>
		public Formula Collision =>
			Vehicles.Vehicles.Skip(1).Aggregate<Vehicle, Formula>(Vehicles.Vehicles[0].IsCollided, (f, v) => f || v.IsCollided);



		/// <summary>
		///   Represents the hazard of an alarm even when no over-height vehicle is on the right lane.
		/// </summary>
		public Formula FalseAlarm =>
			HeightControl.TrafficLights.IsRed &&
			Vehicles.Vehicles.All(vehicle => vehicle.Lane == Lane.Right || vehicle.Kind != VehicleKind.OverheightTruck);

		/// <summary>
		///   Binds the <paramref name="detector" /> to the <see cref="Vehicles" />.
		/// </summary>
		private void Bind(VehicleDetector detector)
		{
			Component.Bind(nameof(detector.GetVehicleKind), nameof(Vehicles.GetVehicleKind));
			Component.Bind(nameof(detector.GetVehiclePosition), nameof(Vehicles.GetVehiclePosition));
			Component.Bind(nameof(detector.GetVehicleLane), nameof(Vehicles.GetVehicleLane));
		}
	}
}