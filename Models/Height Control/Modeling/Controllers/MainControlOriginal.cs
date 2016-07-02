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

	public sealed class MainControlOriginal : MainControl
	{
		/// <summary>
		///   Gets the number of high vehicles currently in the main-control area.
		/// </summary>
		[Range(0, Model.MaxVehicles, OverflowBehavior.Clamp)]
		public int Count { get; private set; }

		/// <summary>
		///   Updates the internal state of the component.
		/// </summary>
		public override void Update()
		{
			base.Update();

			var numberOfHVs = NumberOfEnteringVehicles;
			if (numberOfHVs > 0)
			{
				Count += numberOfHVs;
				Timer.Start();
			}

			if (Count != 0)
			{
				var position = PositionDetector.IsVehicleDetected;
				var left = LeftDetector.IsVehicleDetected;
				var right = RightDetector.IsVehicleDetected;

				// We assume the worst case: If the vehicle was not seen on the right lane, it is assumed to be on the left lane
				IsVehicleLeavingOnLeftLane = position && (left || !right);
				IsVehicleLeavingOnRightLane = position && right;
			}

			if (IsVehicleLeavingOnLeftLane)
				Count--;

			if (IsVehicleLeavingOnRightLane)
				Count--;

			if (Timer.HasElapsed)
				Count = 0;
			else if (Count <= 0)
				Timer.Stop();
		}
	}
}