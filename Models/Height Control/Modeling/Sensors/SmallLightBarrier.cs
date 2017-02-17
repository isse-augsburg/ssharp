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

namespace SafetySharp.CaseStudies.HeightControl.Modeling.Sensors
{
	using Vehicles;

	/// <summary>
	///   Represents a light barrier that detects overheight vehicles at a specific position on a specific lanes.
	/// </summary>
	public sealed class SmallLightBarrier : VehicleDetector
	{
		/// <summary>
		///   The lane of the detector.
		/// </summary>
		private readonly Lane _lane;

		/// <summary>
		///   The position of the detector.
		/// </summary>
		private readonly int _position;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="position">The position of the detector.</param>
		/// <param name="lane">The lane of the detector.</param>
		public SmallLightBarrier(int position, Lane lane)
		{
			_position = position;
			_lane = lane;
		}

		/// <summary>
		///   Gets a value indicating whether the detector detects the <paramref name="vehicle" />.
		/// </summary>
		/// <param name="vehicle">The vehicle that should be checked.</param>
		public override bool DetectsVehicle(Vehicle vehicle)
			=> vehicle.Kind == VehicleKind.OverheightVehicle && vehicle.Lane == _lane && vehicle.IsAtPosition(_position);


		/// <summary>
		///   Returns a string that represents the current object.
		/// </summary>
		public override string ToString() => $"SLB-{Model.GetPositionName(_position)}-{_lane}";
	}
}