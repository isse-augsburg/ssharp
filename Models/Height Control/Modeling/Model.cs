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
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using Controllers;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Sensors;
	using Vehicles;

	/// <summary>
	///   Represents a base class for all variants of the height control case study model.
	/// </summary>
	public sealed class Model : ModelBase
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
		private Model(Vehicle[] vehicles = null)
		{
			vehicles = vehicles ?? new[]
			{
				new Vehicle { Kind = VehicleKind.OverheightTruck },
				new Vehicle { Kind = VehicleKind.OverheightTruck },
				new Vehicle { Kind = VehicleKind.Truck }
			};

			Vehicles = new VehicleCollection(vehicles);
		}

		/// <summary>
		///   Gets the height control that monitors the vehicles and closes the tunnel, if necessary.
		/// </summary>
		[Root(Role.System)]
		public HeightControl HeightControl { get; private set; }

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
		///   Represents the hazard of an alarm even when no high vehicle and no over-height vehicle is on the right lane.
		/// </summary>
		public Formula FalseAlarmWhenAllConformToTrafficLaws =>
			HeightControl.TrafficLights.IsRed &&
			Vehicles.Vehicles.All(
				vehicle => vehicle.Lane == Lane.Right || !(vehicle.Kind == VehicleKind.OverheightTruck || vehicle.Kind == VehicleKind.Truck));

		/// <summary>
		///   Initializes a model of the original design.
		/// </summary>
		public static Model CreateOriginal(Vehicle[] vehicles = null)
			=> CreateVariant(typeof(PreControlOriginal), typeof(MainControlOriginal), typeof(EndControlOriginal), vehicles);

		/// <summary>
		///   Initializes a new variant with the given control types.
		/// </summary>
		private static Model CreateVariant(Type preControlType, Type mainControlType, Type endControlType, Vehicle[] vehicles = null)
		{
			var preControl = (PreControl)Activator.CreateInstance(preControlType);
			var mainControl = (MainControl)Activator.CreateInstance(mainControlType);
			var endControl = (EndControl)Activator.CreateInstance(endControlType);
			var model = new Model(vehicles);

			model.SetupController(preControl);
			model.SetupController(mainControl);
			model.SetupController(endControl);

			model.HeightControl = new HeightControl
			{
				PreControl = preControl,
				MainControl = mainControl,
				EndControl = endControl,
				TrafficLights = new TrafficLights()
			};

			return model;
		}

		/// <summary>
		///   Invoked when the model should initialize bindings between its components.
		/// </summary>
		protected override void CreateBindings()
		{
			Bind(nameof(Vehicles.IsTunnelClosed), nameof(HeightControl.TrafficLights.IsRed));
		}

		/// <summary>
		///   Binds the <paramref name="controller" />'s detectors to the <see cref="Vehicles" /> and sets up the detector's fault
		///   names.
		/// </summary>
		private void SetupController(Component controller)
		{
			foreach (var detector in GetDetectors(controller))
			{
				Component.Bind(nameof(detector.GetVehicleKind), nameof(Vehicles.GetVehicleKind));
				Component.Bind(nameof(detector.GetVehiclePosition), nameof(Vehicles.GetVehiclePosition));
				Component.Bind(nameof(detector.GetVehicleLane), nameof(Vehicles.GetVehicleLane));

				var name = detector.ToString();
				detector.FalseDetection.Name = $"FalseDetection{name}";
				detector.Misdetection.Name = $"Misdetection{name}";
				detector.VehicleCount = Vehicles.Vehicles.Length;
			}
		}

		/// <summary>
		///   Creates the model variants for all combinations of different pre-, main-, and end-controls.
		/// </summary>
		public static IEnumerable<Model> CreateVariants()
		{
			var preControls = GetVariants<PreControl>().ToArray();
			var mainControls = GetVariants<MainControl>().ToArray();
			var endControls = GetVariants<EndControl>().ToArray();

			return from preControl in preControls
				   from mainControl in mainControls
				   from endControl in endControls
				   select CreateVariant(preControl, mainControl, endControl);
		}

		/// <summary>
		///   Gets all variants of the control of type <typeparamref name="T" />.
		/// </summary>
		private static IEnumerable<Type> GetVariants<T>()
			where T : class
		{
			return from type in Assembly.GetExecutingAssembly().GetTypes()
				   where type.IsSubclassOf(typeof(T)) && !type.IsAbstract
				   select type;
		}

		/// <summary>
		///   Gets all vehicle detector instances stored in any of the <paramref name="component" />'s fields.
		/// </summary>
		private static IEnumerable<VehicleDetector> GetDetectors(Component component)
		{
			return from field in component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				   where field.FieldType.IsAssignableFrom(typeof(VehicleDetector))
				   select (VehicleDetector)field.GetValue(component);
		}

		/// <summary>
		///   Gets a name for the <paramref name="position." />
		/// </summary>
		public static string GetPositionName(int position)
		{
			switch (position)
			{
				case EndControlPosition:
					return "End";
				case MainControlPosition:
					return "Main";
				case PreControlPosition:
					return "Pre";
				default:
					return "Unknown";
			}
		}
	}
}