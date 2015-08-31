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
	using System;
	using System.Diagnostics;
	using SafetySharp.Modeling;
	using Sensors;
	using SharedComponents;

	public class MainControlOriginal : Component, IMainControl
	{
		/// <summary>
		///   The sensor that detects high vehicles on the left lane.
		/// </summary>
		protected readonly IVehicleDetector LeftDetector;

        /// <summary>
        ///   The sensor that detects overheight vehicles on any lane.
        /// </summary>
        protected readonly IVehicleDetector PositionDetector;

        /// <summary>
        ///   The sensor that detects high vehicles on the right lane.
        /// </summary>
        protected readonly IVehicleDetector RightDetector;

        /// <summary>
        ///   The timer that is used to deactivate the main-control automatically.
        /// </summary>
        protected readonly Timer Timer;

        /// <summary>
        ///   The number of high vehicles currently in the main-control area.
        /// </summary>
        [Range(0, 4, OverflowBehavior.Clamp)]
        public int Count;

        /// <summary>
        ///   Indicates whether a vehicle has been detected on the left lane.
        /// </summary>
        protected bool VehicleOnLeftLane;

        /// <summary>
        ///   Indicates whether a vehicle has been detected on the right lane.
        /// </summary>
        protected bool VehicleToMonitorPassing;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="positionDetector">The sensor that detects overheight vehicles on any lane.</param>
		/// <param name="leftDetector">The sensor that detects high vehicles on the left lane.</param>
		/// <param name="rightDetector">The sensor that detects high vehicles on the right lane.</param>
		/// <param name="timeout">The amount of time after which the main-control is deactivated.</param>
		public MainControlOriginal(IVehicleDetector positionDetector, IVehicleDetector leftDetector, IVehicleDetector rightDetector,
								   int timeout)
		{
			Timer = new Timer(timeout);
			PositionDetector = positionDetector;
			LeftDetector = leftDetector;
			RightDetector = rightDetector;
		}

		/// <summary>
		///   Indicates whether an vehicle leaving the main-control area.
		/// </summary>
		public bool IsVehicleToMonitorPassing()
		{
			return VehicleToMonitorPassing;
		}

		/// <summary>
		///   Indicates whether an vehicle leaving the main-control area on the left lane has been detected.
		///   This might trigger the alarm.
		/// </summary>
		public bool IsVehicleLeavingOnLeftLane()
		{
			return VehicleOnLeftLane;
		}

        /// <summary>
        ///   Gets the number of vehicles that entered the area in front of the main control during the current system step.
        /// </summary>
        public extern int GetNumberOfEnteringVehicles();

		/// <summary>
		///   Updates the internal state of the component.
		/// </summary>
		public override void Update()
		{
			var numberOfHVs = GetNumberOfEnteringVehicles();
			if (numberOfHVs > 0)
			{
				Count += numberOfHVs;
				Timer.Start();
			}

			var active = Count != 0;
			var onlyRightTriggered = !LeftDetector.IsVehicleDetected() && RightDetector.IsVehicleDetected();

            // We assume the worst case: If the vehicle was not on the right lane, it was on the left lane
            // (even if it was a false detection of the position detector).
            VehicleOnLeftLane = PositionDetector.IsVehicleDetected() && !onlyRightTriggered && active;
			VehicleToMonitorPassing = PositionDetector.IsVehicleDetected() && onlyRightTriggered && active;

		    if (VehicleOnLeftLane)
		        Count--;

            if (VehicleToMonitorPassing && Count>0)
                Count--;

            if (Timer.HasElapsed())
				Count = 0;

            if (Count < 0)
                Count = 0;

            if (Count == 0)
				Timer.Stop();

			if (Count > 5)
				Count = 5;
		}
	}
}