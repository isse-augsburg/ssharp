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

namespace SafetySharp.CaseStudies.HeightControl.Modeling
{
	using System.Linq;
	using Controllers;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Sensors;
	using Vehicles;

	/// <summary>
	///   Represents a base class for all variants of the height control case study model.
	/// </summary>
	public abstract class Model : ModelBase
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
		protected Model(Vehicle[] vehicles = null)
		{
			vehicles = vehicles ?? new[]
			{
				new Vehicle { Kind = VehicleKind.OverheightTruck },
				new Vehicle { Kind = VehicleKind.OverheightTruck },
				new Vehicle { Kind = VehicleKind.Truck }
			};

			Vehicles = new VehicleCollection(vehicles);

			LightBarrierPre = new LightBarrier { Position = PreControlPosition };
			LightBarrierMain = new LightBarrier { Position = MainControlPosition };

			DetectorMainLeft = new OverheadDetector { Lane = Lane.Left, Position = MainControlPosition };
			DetectorMainRight = new OverheadDetector { Lane = Lane.Right, Position = MainControlPosition };
			DetectorEndLeft = new OverheadDetector { Lane = Lane.Left, Position = EndControlPosition };

			Setup(LightBarrierPre, "LB-Pre");
			Setup(LightBarrierMain, "LB-Main");
			Setup(DetectorMainLeft, "OD-Main-Left");
			Setup(DetectorMainRight, "OD-Main-Right");
			Setup(DetectorEndLeft, "OD-End-Left");
		}

		/// <summary>
		///   Gets the height control that monitors the vehicles and closes the tunnel, if necessary.
		/// </summary>
		[Root(Role.System)]
		public HeightControl HeightControl { get; protected set; }

		/// <summary>
		///   Gets the monitored vehicles.
		/// </summary>
		[Root(Role.Environment)]
		public VehicleCollection Vehicles { get; }

		protected LightBarrier LightBarrierPre { get; }
		protected LightBarrier LightBarrierMain { get; }
		protected OverheadDetector DetectorMainLeft { get; }
		protected OverheadDetector DetectorMainRight { get; }
		protected OverheadDetector DetectorEndLeft { get; }

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
		///   Represents the hazard of an alarm even when no high vehicle and no over-height vehicle is on the right lane.
		/// </summary>
		public Formula FalseAlarmWhenAllConformToTrafficLaws =>
			HeightControl.TrafficLights.IsRed &&
			Vehicles.Vehicles.All(
				vehicle => vehicle.Lane == Lane.Right || !(vehicle.Kind == VehicleKind.OverheightTruck || vehicle.Kind == VehicleKind.Truck));

		/// <summary>
		///   Invoked when the model should initialize bindings between its components.
		/// </summary>
		protected override void CreateBindings()
		{
			Bind(nameof(Vehicles.IsTunnelClosed), nameof(HeightControl.TrafficLights.IsRed));
		}

		/// <summary>
		///   Binds the <paramref name="detector" /> to the <see cref="Vehicles" /> and sets up the <paramref name="detector" />'s
		///   fault names.
		/// </summary>
		protected void Setup(VehicleDetector detector, string faultSuffix)
		{
			Component.Bind(nameof(detector.GetVehicleKind), nameof(Vehicles.GetVehicleKind));
			Component.Bind(nameof(detector.GetVehiclePosition), nameof(Vehicles.GetVehiclePosition));
			Component.Bind(nameof(detector.GetVehicleLane), nameof(Vehicles.GetVehicleLane));

			detector.FalseDetection.Name = $"FalseDetection{faultSuffix}";
			detector.Misdetection.Name = $"Misdetection{faultSuffix}";
			detector.VehicleCount = Vehicles.Vehicles.Length;
		}
	}
}