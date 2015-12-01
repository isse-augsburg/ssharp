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
	using Actuators;
	using SafetySharp.Modeling;

	/// <summary>
	///   Represents the height control of the Elbtunnel.
	/// </summary>
	public class HeightControl : Component, IInitializable
	{
		/// <summary>
		///   The end-control step of the height control.
		/// </summary>
		[Hidden]
		public EndControl EndControl;

		/// <summary>
		///   The main-control step of the height control.
		/// </summary>
		[Hidden]
		public MainControl MainControl;

		/// <summary>
		///   The pre-control step of the height control.
		/// </summary>
		[Hidden]
		public PreControl PreControl;

		/// <summary>
		///   The traffic lights that are used to signal that the tunnel is closed.
		/// </summary>
		[Hidden]
		public TrafficLights TrafficLights;

		/// <summary>
		///   Performs the nondeterministic initialization.
		/// </summary>
		void IInitializable.Initialize()
		{
			Bind(nameof(MainControl.GetNumberOfEnteringVehicles), nameof(PreControl.GetNumberOfPassingVehicles));
			Bind(nameof(EndControl.VehicleEntering), nameof(MainControl.IsVehicleLeavingOnRightLane));
		}

		/// <summary>
		///   Updates the internal state of the component.
		/// </summary>
		public override void Update()
		{
			Update(PreControl, MainControl, EndControl);

			if (MainControl.IsVehicleLeavingOnLeftLane || EndControl.IsCrashPotentiallyImminent)
				TrafficLights.SwitchToRed();
		}
	}
}