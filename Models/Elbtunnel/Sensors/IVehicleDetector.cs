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
	using SafetySharp.Modeling;

	/// <summary>
	///   A common interface for sensors that detect vehicles.
	/// </summary>
	public interface IVehicleDetector : IComponent
	{
		/// <summary>
		///   Indicates whether the sensor detected a vehicle.
		/// </summary>
		[Provided]
		bool IsVehicleDetected();

		/// <summary>
		///   Gets the minimal position of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		// TODO: Replace this port by an array-based version once S# supports arrays.
		[Required]
		int GetVehiclePositionMin(int vehicleIndex);
        
        /// <summary>
        ///   Gets the maximal position of the vehicle with the given <paramref name="vehicleIndex" />.
        /// </summary>
        /// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
        // TODO: Replace this port by an array-based version once S# supports arrays.
        [Required]
        int GetVehiclePositionMax(int vehicleIndex);

		/// <summary>
		///   Gets the speed of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		// TODO: Replace this port by an array-based version once S# supports arrays.
		[Required]
		VehicleKind GetVehicleKind(int vehicleIndex);

		/// <summary>
		///   Gets the lane of the vehicle with the given <paramref name="vehicleIndex" />.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		// TODO: Replace this port by an array-based version once S# supports arrays.
		[Required]
		Lane GetVehicleLane(int vehicleIndex);
	}
}