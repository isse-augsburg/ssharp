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
	///   Represents the original design of the end-control.
	/// </summary>
	public class EndControlOriginal : EndControl
	{
		/// <summary>
		///   Indicates whether the end-control is currently active.
		/// </summary>
		private bool _active;

		/// <summary>
		///   The sensor that is used to detect vehicles in the end-control area.
		/// </summary>
		[Hidden]
		public VehicleDetector Detector;

		/// <summary>
		///   The timer that is used to deactivate the end-control automatically.
		/// </summary>
		[Hidden]
		public Timer Timer;

		/// <summary>
		///   Gets a value indicating whether a crash is potentially imminent.
		/// </summary>
		public override bool IsCrashPotentiallyImminent => _active && Detector.IsVehicleDetected;

		/// <summary>
		///   Updates the internal state of the component.
		/// </summary>
		public override void Update()
		{
			if (VehicleEntering)
			{
				_active = true;
				Timer.Start();
			}

			if (Timer.HasElapsed)
				_active = false;
		}
	}
}