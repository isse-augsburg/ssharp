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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.Model
{
	using DataStructures;
	using SafetySharp.Modeling;

	public enum WiperState
	{
		Active,
		Inactive,
		Error
	}

	public enum WiperConfig
	{
		Installed,
		NotInstalled
	}

	public class WiperEcu : Component
	{
		// external
		public extern VehicleStatus GetVehicleStatus();
		public WiperActuator WiperActuator;

		// internal
		public WiperSpeed WiperSpeedInternal { get; set; } = 0;
		public WiperConfig WiperConfig;
		public WiperState WiperState;

		public WiperEcu(WiperActuator wiperActuator)
		{
			WiperActuator = wiperActuator;
			if (WiperActuator == null)
			{
				WiperConfig = WiperConfig.NotInstalled;
			}
			else
			{
				WiperConfig = WiperConfig.Installed;
			}
		}

		public void ReceiveRequest(WiperRequest request)
		{
			switch (request)
			{
				case WiperRequest.Fast:
					if (GetVehicleStatus()==VehicleStatus.Running &&
						WiperConfig != WiperConfig.Installed &&
						WiperState != WiperState.Error)
					{
						SetWiperSpeed (WiperSpeed.Fast);
						WiperState = WiperState.Active;
					}
					break;
				case WiperRequest.Slow:
					if (GetVehicleStatus() == VehicleStatus.Running &&
						WiperConfig != WiperConfig.Installed &&
						WiperState != WiperState.Error)
					{
						SetWiperSpeed(WiperSpeed.Slow);
						WiperState = WiperState.Active;
					}
					break;
				case WiperRequest.Increase:
					if (WiperState == WiperState.Active)
					{
						AddToCurrentSpeed(1);
					}
					break;
				case WiperRequest.Off:
					if (WiperState== WiperState.Active)
					{
						SetWiperSpeed(WiperSpeed.Off);
						WiperState = WiperState.Inactive;
					}
					break;
				default:
					throw new InvalidScenarioException();
					//goto case WiperRequest.Off;
			}
		}

		public void UpdateWiperSpeedOfActuator()
		{
			if (WiperConfig == WiperConfig.Installed)
			{
				WiperActuator.WiperSpeed = WiperSpeedInternal;
			}
		}

		public void SetWiperSpeed(WiperSpeed wiperSpeed)
		{
			WiperSpeedInternal = wiperSpeed;
			UpdateWiperSpeedOfActuator();
		}

		public void AddToCurrentSpeed(int difference)
		{
			WiperSpeedInternal += difference;
			UpdateWiperSpeedOfActuator();
		}
	}
}
