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
	public class HeightControl : Component
	{
		/// <summary>
		///   The end-control step of the height control.
		/// </summary>
		private readonly IEndControl _endControl;

		/// <summary>
		///   The main-control step of the height control.
		/// </summary>
		private readonly IMainControl _mainControl;

		/// <summary>
		///   The pre-control step of the height control.
		/// </summary>
		private readonly IPreControl _preControl;

		/// <summary>
		///   The traffic lights that are used to signal that the tunnel is closed.
		/// </summary>
		private readonly TrafficLights _trafficLights;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="preControl">The pre-control step of the height control.</param>
		/// <param name="mainControl">The main-control step of the height control.</param>
		/// <param name="endControl">The end-control step of the height control.</param>
		/// <param name="trafficLights">The traffic lights that are used to signal that the tunnel is closed.</param>
		public HeightControl(IPreControl preControl, IMainControl mainControl, IEndControl endControl, TrafficLights trafficLights)
		{
			_preControl = preControl;
			_mainControl = mainControl;
			_endControl = endControl;
			_trafficLights = trafficLights;

			Bind(_mainControl.RequiredPorts.GetNumberOfEnteringVehicles = _preControl.ProvidedPorts.GetNumberOfPassingVehicles);
			Bind(_endControl.RequiredPorts.VehicleEntering = _mainControl.ProvidedPorts.IsVehicleToMonitorPassing);
		}

		/// <summary>
		///   Updates the internal state of the component.
		/// </summary>
		public override void Update()
		{
			if (_mainControl.IsVehicleLeavingOnLeftLane() || _endControl.IsCrashPotentiallyImminent())
				_trafficLights.SwitchToRed();
		}
	}
}