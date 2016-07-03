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
		[Subcomponent]
		public readonly VehicleDetector LeftDetector = new OverheadDetector(Model.MainControlPosition, Lane.Left);

		/// <summary>
		///   The sensor that detects overheight vehicles on any lane.
		/// </summary>
		[Subcomponent]
		public readonly VehicleDetector PositionDetector = new LightBarrier(Model.MainControlPosition);

		/// <summary>
		///   The sensor that detects high vehicles on the right lane.
		/// </summary>
		[Subcomponent]
		public readonly VehicleDetector RightDetector = new OverheadDetector(Model.MainControlPosition, Lane.Right);

		/// <summary>
		///   The timer that is used to deactivate the main-control automatically.
		/// </summary>
		[Subcomponent]
		public readonly Timer Timer = new Timer();

		/// <summary>
		///   Invoked when the an overheight vehicle leaves the main-control area on the right lane.
		/// </summary>
		public extern void ActivateEndControl();

		/// <summary>
		///   Closes the tunnel when a collision is potentially imminent.
		/// </summary>
		public extern void CloseTunnel();

		/// <summary>
		///   Invoked when the given number of vehicles enters the main-control area.
		/// </summary>
		public abstract void VehiclesEntering(int vehicleCount);

		/// <summary>
		///   Updates the state of the component.
		/// </summary>
		public override void Update()
		{
			Update(LeftDetector, RightDetector, PositionDetector, Timer);
		}
	}
}