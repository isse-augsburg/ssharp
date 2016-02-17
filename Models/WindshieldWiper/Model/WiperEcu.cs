using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.Model
{
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
