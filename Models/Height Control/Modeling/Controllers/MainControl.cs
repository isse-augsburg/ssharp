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

namespace SafetySharp.CaseStudies.HeightControl.Modeling.Controllers
{
	using SafetySharp.Modeling;
	using Sensors;
	using Vehicles;

	public abstract class MainControl : Component
	{
		/// <summary>
		///   The sensor that detects high vehicles on the left lane.
		/// </summary>
		public readonly VehicleDetector LeftDetector = new OverheadDetector(Model.MainControlPosition, Lane.Left);

		/// <summary>
		///   The sensor that detects overheight vehicles on any lane.
		/// </summary>
		public readonly VehicleDetector PositionDetector = new LightBarrier(Model.MainControlPosition);

		/// <summary>
		///   The sensor that detects high vehicles on the right lane.
		/// </summary>
		public readonly VehicleDetector RightDetector = new OverheadDetector(Model.MainControlPosition, Lane.Right);

		/// <summary>
		///   The timer that is used to deactivate the main-control automatically.
		/// </summary>
		public readonly Timer Timer = new Timer();

		/// <summary>
		///   Indicates whether an vehicle leaving the main-control area.
		/// </summary>
		[Hidden]
		public bool IsVehicleLeavingOnRightLane { get; protected set; }

		/// <summary>
		///   Indicates whether an vehicle leaving the main-control area on the left lane has been detected.
		///   This might trigger the alarm.
		/// </summary>
		[Hidden]
		public bool IsVehicleLeavingOnLeftLane { get; protected set; }

		/// <summary>
		///   Gets the number of vehicles that entered the area in front of the main control during the current system step.
		/// </summary>
		public extern int GetNumberOfEnteringVehicles();

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public override void Update()
		{
			IsVehicleLeavingOnRightLane = false;
			IsVehicleLeavingOnLeftLane = false;

			Update(LeftDetector, RightDetector, PositionDetector, Timer);
		}
	}
}