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

namespace Elbtunnel
{
	using System.Linq;
	using Actuators;
	using Controllers;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Sensors;
	using Vehicles;

	/// <summary>
	///   Represents the specification of the Elbtunnel case study.
	/// </summary>
	public class Specification
	{
		public const int TunnelPosition = 19;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public Specification(Vehicle[] vehicles = null)
		{
			vehicles = vehicles ?? new[]
			{
				new Vehicle(VehicleKind.OverheightTruck),
				new Vehicle(VehicleKind.OverheightTruck),
				new Vehicle(VehicleKind.Truck)
			};

			var lightBarrier1 = new LightBarrier
			{
				Position = 5,
				Misdetection = { Name = "MissDetectionLB1" },
				FalseDetection = { Name = "FalseDetectionLB1" },
				VehicleCount = vehicles.Length
			};

			var lightBarrier2 = new LightBarrier
			{
				Position = 10,
				Misdetection = { Name = "MissDetectionLB2" },
				FalseDetection = { Name = "FalseDetectionLB2" },
				VehicleCount = vehicles.Length
			};

			var detectorLeft = new OverheadDetector
			{
				Lane = Lane.Left,
				Position = 10,
				Misdetection = { Name = "MissDetectionODL" },
				FalseDetection = { Name = "FalseDetectionODL" },
				VehicleCount = vehicles.Length
			};

			var detectorRight = new OverheadDetector
			{
				Lane = Lane.Right,
				Position = 10,
				Misdetection = { Name = "MissDetectionODR" },
				FalseDetection = { Name = "FalseDetectionODR" },
				VehicleCount = vehicles.Length
			};

			var detectorFinal = new OverheadDetector
			{
				Lane = Lane.Left,
				Position = 15,
				Misdetection = { Name = "MissDetectionODF" },
				FalseDetection = { Name = "FalseDetectionODF" },
				VehicleCount = vehicles.Length
			};

			HeightControl = new HeightControl
			{
				PreControl = new PreControlOriginal
				{
					Detector = lightBarrier1
				},
				MainControl = new MainControlOriginal
				{
					LeftDetector = detectorLeft,
					RightDetector = detectorRight,
					PositionDetector = lightBarrier2,
					Timer = new Timer { Timeout = 6 }
				},
				EndControl = new EndControlOriginal
				{
					Detector = detectorFinal,
					Timer = new Timer { Timeout = 6 }
				},
				TrafficLights = new TrafficLights()
			};

			Vehicles = new VehicleCollection(vehicles);

			Component.Bind(nameof(Vehicles.IsTunnelClosed), nameof(HeightControl.TrafficLights.IsRed));

			Bind(lightBarrier1);
			Bind(lightBarrier2);
			Bind(detectorLeft);
			Bind(detectorRight);
			Bind(detectorFinal);
		}

		/// <summary>
		///   Gets the height control that monitors the vehicles and closes the tunnel, if necessary.
		/// </summary>
		[Root]
		public HeightControl HeightControl { get; }

		/// <summary>
		///   Gets the monitored vehicles.
		/// </summary>
		[Root]
		public VehicleCollection Vehicles { get; }

		/// <summary>
		///   Represents the hazard of an over-height vehicle colliding with the tunnel entrance on the left lane.
		/// </summary>
		[Hazard]
		public Formula Collision
		{
			get
			{
				var vehiclesAboutToCollide =
					Vehicles.Vehicles.Skip(1).Aggregate(VehicleCollided(Vehicles.Vehicles[0]), (f, v) => f || VehicleCollided(v));

				return vehiclesAboutToCollide && !HeightControl.TrafficLights.IsRed;
			}
		}

		private void Bind(VehicleDetector detector)
		{
			Component.Bind(nameof(detector.GetVehicleKind), nameof(Vehicles.GetVehicleKind));
			Component.Bind(nameof(detector.GetVehiclePosition), nameof(Vehicles.GetVehiclePosition));
			Component.Bind(nameof(detector.GetVehicleLane), nameof(Vehicles.GetVehicleLane));
		}

		private static Formula VehicleCollided(Vehicle vehicle)
		{
			return vehicle.Kind == VehicleKind.OverheightTruck && vehicle.Position >= TunnelPosition && vehicle.Lane == Lane.Left;
		}
	}
}