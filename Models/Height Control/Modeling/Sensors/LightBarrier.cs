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

namespace SafetySharp.CaseStudies.HeightControl.Modeling.Sensors
{
	using SafetySharp.Modeling;
	using Vehicles;

	/// <summary>
	///   Represents a light barrier that detects overheight vehicles at a specific position on any of the lanes.
	/// </summary>
	public sealed class LightBarrier : VehicleDetector
	{
		/// <summary>
		///   The position of the light barrier. When an overheight vehicle passes this position, it is detected by the light barrier.
		/// </summary>
		[Hidden]
		public int Position;

		/// <summary>
		///   Gets a value indicating whether the light barrier detects the vehicle with the given position and speed.
		/// </summary>
		/// <param name="vehicleIndex">The index of the vehicle that should be checked.</param>
		protected override bool CheckVehicle(int vehicleIndex)
		{
			if (GetVehicleKind(vehicleIndex) != VehicleKind.OverheightTruck)
				return false;

			int begin, end;
			GetVehiclePosition(vehicleIndex, out begin, out end);

			return begin <= Position && end > Position;
		}
	}
}