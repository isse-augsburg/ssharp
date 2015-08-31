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

namespace Elbtunnel.Controllers
{
	using SafetySharp.Modeling;
	using Sensors;

	/// <summary>
	///   Represents a more sophisticated pre-control of the Elbtunnel height control that uses additional sensors to detect
	///   vehicles entering the height control area.
	/// </summary>
	public class PreControlImprovedDetection : Component, IPreControl
	{
		/// <summary>
		///   The sensor that detects vehicles on the left lane.
		/// </summary>
		private readonly IVehicleDetector _leftDetector;

		/// <summary>
		///   The sensor that detects vehicles on any lane.
		/// </summary>
		private readonly IVehicleDetector _positionDetector;

		/// <summary>
		///   The sensor that detects vehicles on the right lane.
		/// </summary>
		private readonly IVehicleDetector _rightDetector;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="positionDetector">The sensor that detects vehicles on any lane.</param>
		/// <param name="leftDetector">The sensor that detects vehicles on the left lane.</param>
		/// <param name="rightDetector">The sensor that detects vehicles on the right lane.</param>
		public PreControlImprovedDetection(IVehicleDetector positionDetector, IVehicleDetector leftDetector, IVehicleDetector rightDetector)
		{
			_positionDetector = positionDetector;
			_leftDetector = leftDetector;
			_rightDetector = rightDetector;
		}

        /// <summary>
        ///   Gets the number of vehicles that passed the pre-control during the current system step.
        /// </summary>
        public int GetNumberOfPassingVehicles()
		{
			if (_positionDetector.IsVehicleDetected() && _leftDetector.IsVehicleDetected() && _rightDetector.IsVehicleDetected())
				return 2;

			if (_positionDetector.IsVehicleDetected() && (_leftDetector.IsVehicleDetected() || _rightDetector.IsVehicleDetected()))
				return 1;

			return 0;
		}
	}
}