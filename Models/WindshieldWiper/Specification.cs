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

namespace Wiper
{
	using System.Linq;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Wiper.Model;

	/// <summary>
	///   Represents the specification of the Windshield Wiper case study.
	/// </summary>
	public class Specification
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public Specification()
		{
			WiperActuator = new WiperActuator();
			WiperEcu = new WiperEcu(WiperActuator)
			{
				WiperSpeedInternal = 0,
				WiperConfig = WiperConfig.Installed,
				WiperState = WiperState.Inactive
			};

			WiperControlStalk = new WiperControlStalk();
			VehicleMainEcu = new VehicleMainEcu();

			Component.Bind(nameof(WiperEcu.GetVehicleStatus), nameof(VehicleMainEcu.GetVehicleStatus));
		}

		[Root(Role.SystemOfInterest)]
		public WiperEcu WiperEcu { get; }

		[Root(Role.SystemOfInterest)]
		public WiperActuator WiperActuator { get; }

		[Root(Role.SystemContext)]
		public WiperControlStalk WiperControlStalk { get; }

		[Root(Role.SystemContext)]
		public VehicleMainEcu VehicleMainEcu { get; }

		[Hazard]
		public Formula InvalidScenario =>
			false;

	}
}