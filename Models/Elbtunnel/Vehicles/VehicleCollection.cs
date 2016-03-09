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

namespace Elbtunnel.Vehicles
{
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents a collection of vehicles.
	/// </summary>
	public class VehicleCollection : Component
	{
		/// <summary>
		///   The vehicles contained in the collection.
		/// </summary>
		[Hidden(HideElements = true)]
		public readonly Vehicle[] Vehicles;
		
		/// <summary>
		///   Represents a fault where the driver disregards traffic rules
		/// </summary>
		public readonly Fault DisregardTrafficRules = new TransientFault();

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public VehicleCollection(params Vehicle[] vehicles)
		{
			Vehicles = vehicles;

			foreach (var vehicle in Vehicles)
			{
				DisregardTrafficRules.AddEffect<Vehicle.DisregardTrafficRulesEffect>(vehicle);
				Bind(nameof(vehicle.IsTunnelClosed), nameof(ForwardIsTunnelClosed));
			}
		}

		// TODO: Remove once port forwardings are supported
		private bool ForwardIsTunnelClosed => IsTunnelClosed;

		/// <summary>
		///   Informs the vehicle whether the tunnel is closed.
		/// </summary>
		public extern bool IsTunnelClosed { get; }

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public override void Update()
		{
			Update(Vehicles);
		}

		/// <summary>
		///   Gets the position of the vehicle with the given <paramref name="vehicleIndex" />; the vehicle's position lies between
		///   <paramref name="begin" /> and <paramref name="end" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		/// <param name="begin">Returns the vehicle's minimum position.</param>
		/// <param name="end">Returns the vehicle's maximum position.</param>
		public void GetVehiclePosition(int vehicleIndex, out int begin, out int end)
		{
			var vehicle = Vehicles[vehicleIndex];
			begin = vehicle.Position - vehicle.Speed;
			end = vehicle.Position;
		}

		/// <summary>
		///   Gets the lane of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		public Lane GetVehicleLane(int vehicleIndex)
		{
			return Vehicles[vehicleIndex].Lane;
		}

		/// <summary>
		///   Gets the speed of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		public VehicleKind GetVehicleKind(int vehicleIndex)
		{
			return Vehicles[vehicleIndex].Kind;
		}
	}
}