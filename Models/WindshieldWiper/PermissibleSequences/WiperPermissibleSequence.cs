using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.PermissibleSequences
{
	using Model;

	public abstract class WiperPermissibleSequence : PermissibleSequence
	{
		protected bool IsMessageFromUserToWiper(Message message)
		{
			return (message.Source.Equals("usr") && message.Target.Equals("wiper"));
		}

		protected bool IsMessageFromWiperToActuator(Message message)
		{
			return (message.Source.Equals("wiper") && message.Target.Equals("act"));
		}

		protected bool IsVehicleRunning(CurrentState state)
		{
			return state.VehicleMainEcu.VehicleStatus == VehicleStatus.Running;
		}

		protected bool IsWiperInstalled(CurrentState state)
		{
			return state.WiperEcu.WiperConfig == WiperConfig.Installed;
		}

		protected bool WiperControllerInErrorState(CurrentState state)
		{
			return state.WiperEcu.WiperState == WiperState.Error;
		}
	}
}
