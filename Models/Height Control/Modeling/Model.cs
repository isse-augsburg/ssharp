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

namespace SafetySharp.CaseStudies.HeightControl.Modeling
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Controllers;
	using ISSE.SafetyChecking.Formula;
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
		public const int Timeout = 5;
		public const int TunnelPosition = 12;
		public const int MaxSpeed = 2;
		public const int MinSpeed = 1;
		public const int MaxVehicles = 3;

		[Hidden]
		public bool CheckOnlyUntilVehiclesCompleted = false;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public Model(PreControl preControl, MainControl mainControl, EndControl endControl, Vehicle[] vehicles = null)
		{
			vehicles = vehicles ?? new[]
			{
				new Vehicle { Kind = VehicleKind.OverheightVehicle },
				new Vehicle { Kind = VehicleKind.OverheightVehicle },
				new Vehicle { Kind = VehicleKind.HighVehicle }
			};

			VehicleSet = new VehicleSet(vehicles);

			SetupController(preControl);
			SetupController(mainControl);
			SetupController(endControl);

			HeightControl = new HeightControl(preControl, mainControl, endControl);
			Bind(nameof(VehicleSet.IsTunnelClosed), nameof(HeightControl.TrafficLights.IsRed));
		}

		/// <summary>
		///   Gets the height control that monitors the vehicles and closes the tunnel, if necessary.
		/// </summary>
		[Root(RootKind.Controller)]
		public HeightControl HeightControl { get; }

		/// <summary>
		///   Gets the set of monitored vehicles.
		/// </summary>
		[Root(RootKind.Plant)]
		public VehicleSet VehicleSet { get; }

		/// <summary>
		///   Gets the monitored vehicles.
		/// </summary>
		public Vehicle[] Vehicles => VehicleSet.Vehicles;

		/// <summary>
		///   Represents the hazard of an overheight vehicle colliding with the tunnel entrance on the left lane.
		/// </summary>
		public Formula Collision
		{
			get
			{
				Formula vehiclesAtEnd = CheckOnlyUntilVehiclesCompleted && VehicleSet.AllVehiclesCompleted;
				return
					!vehiclesAtEnd &&
					Vehicles.Any(vehicle =>
						vehicle.Position == TunnelPosition &&
						vehicle.Lane == Lane.Left &&
						vehicle.Kind == VehicleKind.OverheightVehicle);
			}
		}

		/// <summary>
		///   Represents the hazard of an alarm even when no overheight vehicle is on the right lane.
		/// </summary>
		public Formula FalseAlarm
		{
			get
			{
				Formula vehiclesAtEnd = CheckOnlyUntilVehiclesCompleted && VehicleSet.AllVehiclesCompleted;
				return
					! vehiclesAtEnd &&
					HeightControl.TrafficLights.IsRed &&
					!Vehicles.Any(vehicle => vehicle.Lane == Lane.Left && vehicle.Kind == VehicleKind.OverheightVehicle);
			}
		}

		/// <summary>
		///   Initializes a model of the original design.
		/// </summary>
		public static Model CreateOriginal(Vehicle[] vehicles = null) =>
			new Model(new PreControlOriginal(), new MainControlOriginal(), new EndControlOriginal(), vehicles);

		/// <summary>
		///   Initializes a new variant with the given control types.
		/// </summary>
		private static Model CreateVariant(Type preControlType, Type mainControlType, Type endControlType)
		{
			var preControl = (PreControl)Activator.CreateInstance(preControlType);
			var mainControl = (MainControl)Activator.CreateInstance(mainControlType);
			var endControl = (EndControl)Activator.CreateInstance(endControlType);

			return new Model(preControl, mainControl, endControl);
		}

		/// <summary>
		///   Binds the <paramref name="controller" />'s detectors to the <see cref="VehicleSet" /> and sets up the detector's
		///   fault names.
		/// </summary>
		private void SetupController(Component controller)
		{
			foreach (var detector in controller.GetSubcomponents().OfType<VehicleDetector>())
			{
				Bind(nameof(detector.ObserveVehicles), nameof(VehicleSet.ObserveVehicles));

				var name = detector.ToString();
				detector.FalseDetection.Name = $"FalseDetection{name}";
				detector.Misdetection.Name = $"Misdetection{name}";
			}
		}

		/// <summary>
		///   Creates the model variants for all combinations of different pre-, main-, and end-controls.
		/// </summary>
		public static IEnumerable<Model> CreateVariants()
		{
			return from preControl in GetVariants<PreControl>()
				   from mainControl in GetVariants<MainControl>()
				   from endControl in GetVariants<EndControl>()
				   where IsRealisiticCombination(preControl, mainControl)
				   select CreateVariant(preControl, mainControl, endControl);
		}

		/// <summary>
		///   Checks whether the given combination of control types is realistic.
		/// </summary>
		private static bool IsRealisiticCombination(Type preControl, Type mainControl)
		{
			var mainControlHasCounter = mainControl != typeof(MainControlNoCounter) && mainControl != typeof(MainControlNoCounterTolerant);
			return preControl != typeof(PreControlOverheadDetectors) || mainControlHasCounter;
		}

		/// <summary>
		///   Gets all variants of the control of type <typeparamref name="T" />.
		/// </summary>
		private static IEnumerable<Type> GetVariants<T>()
			where T : class
		{
			return from type in typeof(T).Assembly.GetTypes()
				   where type.IsSubclassOf(typeof(T)) && !type.IsAbstract
				   select type;
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