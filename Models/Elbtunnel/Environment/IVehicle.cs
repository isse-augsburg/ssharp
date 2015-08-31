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

namespace Elbtunnel.Environment
{
	using SafetySharp.Modeling;

	/// <summary>
	///   A common interface for vehciles using the tunnel.
	/// </summary>
	public interface IVehicle : IComponent
    {
        /// <summary>
        ///   Gets the minimal position of the vehicle in the current step.
        /// </summary>
        // TODO: Use a property once supported by the S# compiler.
        [Provided]
        int GetPositionMin();

        /// <summary>
        ///   Gets the maximal position of the vehicle in the current step.
        /// </summary>
        // TODO: Use a property once supported by the S# compiler.
        [Provided]
        int GetPositionMax();

        /// <summary>
        ///   Checks, if the desired next position of a vehicle is currently vacant and undisturbed.
        /// </summary>
        /// <param name="desiredLane">The desired lane a vehicle wants to occupy.</param>
        /// <param name="desiredPosition">The desired position a vehicle wants to occupy.</param>
        [Required]
        bool CheckIfPositionIsVacant(Lane desiredLane, int desiredPosition);

        /// <summary>
        ///   Gets the current lane of the vehicle.
        /// </summary>
        // TODO: Use a property once supported by the S# compiler.
        [Provided]
		Lane GetLane();

		/// <summary>
		///   Gets the kind the vehicle.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		[Provided]
		VehicleKind GetKind();

		/// <summary>
		///   Informs the vehicle whether the tunnel is closed.
		/// </summary>
		// TODO: Use a property once supported by the S# compiler.
		[Required]
		bool IsTunnelClosed();
    }
}