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

namespace Elbtunnel.Controllers
{
	public sealed class MainControlRemovedCounterTolerant : MainControl
	{
		/// <summary>
		///   Updates the internal state of the component.
		/// </summary>
		public override void Update()
		{
			base.Update();

			if (GetNumberOfEnteringVehicles() > 0)
				Timer.Start();

			var active = !Timer.HasElapsed;
			if (!active || !PositionDetector.IsVehicleDetected)
				return;

			if (LeftDetector.IsVehicleDetected)
			{
				// Here we detected a vehicle on the left lane. This is one of the undesired cases.
				IsVehicleLeavingOnRightLane = true;
				IsVehicleLeavingOnLeftLane = true;
			}
			else if (RightDetector.IsVehicleDetected)
			{
				// Here we detected a vehicle on the right lane.
				IsVehicleLeavingOnRightLane = true;
				IsVehicleLeavingOnLeftLane = false;
			}
			else
			{
				// Here we detected a vehicle on neither the left lane nor the right lane.
				// Just in case we emit a signal that a vehicle to monitor might have passed.
				// Without counter this case is equivalent to the case before.
				IsVehicleLeavingOnRightLane = true;
				IsVehicleLeavingOnLeftLane = false;
			}
		}
	}
}