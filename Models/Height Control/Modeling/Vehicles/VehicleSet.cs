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

namespace SafetySharp.CaseStudies.HeightControl.Modeling.Vehicles
{
	using System.Linq;
	using SafetySharp.Modeling;
	using Sensors;

	/// <summary>
	///   Represents a set of vehicles.
	/// </summary>
	public sealed class VehicleSet : Component
	{
		/// <summary>
		///   Allows high vehicles to drive on the left lane.
		/// </summary>
		public readonly Fault LeftHV = new TransientFault();

		/// <summary>
		///   Allows overheight vehicles to drive on the left lane.
		/// </summary>
		public readonly Fault LeftOHV = new TransientFault();

		/// <summary>
		///   Allows all kinds of vehicles to drive slower than expected.
		/// </summary>
		public readonly Fault SlowTraffic = new TransientFault();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public VehicleSet(Vehicle[] vehicles)
		{
			Vehicles = vehicles;

			foreach (var vehicle in Vehicles)
				Bind(nameof(vehicle.IsTunnelClosed), nameof(ForwardIsTunnelClosed));

			LeftOHV.AddEffects<Vehicle.DriveLeftEffect>(vehicles.Where(vehicle => vehicle.Kind == VehicleKind.OverheightVehicle));
			LeftHV.AddEffects<Vehicle.DriveLeftEffect>(vehicles.Where(vehicle => vehicle.Kind == VehicleKind.HighVehicle));
			SlowTraffic.AddEffects<Vehicle.SlowTrafficEffect>(vehicles);

			for (var i = 0; i < Vehicles.Length; ++i)
			{
				for (var j = i + 1; j < Vehicles.Length; ++j)
				{
					AddSensorConstraint(Vehicles[i], Vehicles[j], Model.PreControlPosition);
					AddSensorConstraint(Vehicles[i], Vehicles[j], Model.MainControlPosition);
					AddSensorConstraint(Vehicles[i], Vehicles[j], Model.EndControlPosition);
				}
			}
		}

		/// <summary>
		///   The vehicles contained in the set.
		/// </summary>
		[Hidden(HideElements = true), Subcomponent]
		public Vehicle[] Vehicles { get; }

		// TODO: Remove once S# supports port forwardings
		private bool ForwardIsTunnelClosed => IsTunnelClosed;

		/// <summary>
		///   Informs the vehicles contained in the set whether the tunnel is closed.
		/// </summary>
		public extern bool IsTunnelClosed { get; }

		/// <summary>
		///   Adds a state constraint ensuring that no two vehicles pass the same sensor on the same lane at the same time.
		/// </summary>
		private void AddSensorConstraint(Vehicle vehicle1, Vehicle vehicle2, int position)
		{
			AddStateConstraint(() => !vehicle1.IsAtPosition(position) || !vehicle2.IsAtPosition(position) || vehicle1.Lane != vehicle2.Lane);
		}

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public override void Update()
		{
			Update(Vehicles);
		}

		/// <summary>
		///   Checks whether the <paramref name="detector" /> detects any vehicles.
		/// </summary>
		/// <param name="detector">The detector that should observe the vehicles.</param>
		public bool ObserveVehicles(VehicleDetector detector)
		{
			// Ideally, we'd just use the following line instead of the for-loop below; however, it generates
			// a delegate and probably an interator each time the method is called, therefore increasing the 
			// pressure on the garbage collector; roughly 250 million heap allocations would be required to
			// check the case study's original design. All in all, model checking times increase noticeably, in 
			// some cases by 40% or more...

			// return Vehicles.Any(detector.DetectsVehicle);

			foreach (var vehicle in Vehicles)
			{
				if (detector.DetectsVehicle(vehicle))
					return true;
			}

			return false;
		}
	}
}