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

namespace Elbtunnel.Sensors
{
	using Vehicles;
	using SafetySharp.Modeling;

	/// <summary>
	///   A common base class for sensors that detect vehicles.
	/// </summary>
	public abstract class VehicleDetector : Component
	{
		/// <summary>
		///   Represents a false detection, i.e., a vehicle is detected even though none is present.
		/// </summary>
		[Hidden]
		public Fault FalseDetection;

		/// <summary>
		///   Represents a misdetection, i.e., a vehicle does not detect even though it hould have been detected.
		/// </summary>
		[Hidden]
		public Fault MisDetection;

		/// <summary>
		///   The number of vehicles that can potentially be detected.
		/// </summary>
		[Hidden]
		public int VehicleCount;

		/// <summary>
		///   Indicates whether the light barrier detected a vehicle.
		/// </summary>
		public virtual bool IsVehicleDetected
		{
			get
			{
				for (var i = 0; i < VehicleCount; ++i)
				{
					if (CheckVehicle(i))
						return true;
				}

				return false;
			}
		}

		/// <summary>
		///   Gets a value indicating whether the light barrier detects the vehicle with the given position and speed.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		protected abstract bool CheckVehicle(int vehicleIndex);

		/// <summary>
		///   Gets the position of the vehicle with the given <paramref name="vehicleIndex" />; the vehicle's position lies between
		///   <paramref name="begin" /> and <paramref name="end" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		/// <param name="begin">Returns the vehicle's minimum position.</param>
		/// <param name="end">Returns the vehicle's maximum position.</param>
		public extern void GetVehiclePosition(int vehicleIndex, out int begin, out int end);

		/// <summary>
		///   Gets the speed of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		public extern VehicleKind GetVehicleKind(int vehicleIndex);

		/// <summary>
		///   Gets the lane of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		public extern Lane GetVehicleLane(int vehicleIndex);

		/// <summary>
		///   Represents a false detection, i.e., a vehicle is detected even though none is present.
		/// </summary>
		[FaultEffect(Fault = nameof(FalseDetection))]
		public abstract class FalseDetectionEffect : VehicleDetector
		{
			public override bool IsVehicleDetected => true;
		}

		/// <summary>
		///   Represents a misdetection, i.e., a vehicle does not detect even though it hould have been detected.
		/// </summary>
		[FaultEffect(Fault = nameof(MisDetection))]
		public abstract class MisdetectionEffect : VehicleDetector
		{
			public override bool IsVehicleDetected => false;
		}
	}
}