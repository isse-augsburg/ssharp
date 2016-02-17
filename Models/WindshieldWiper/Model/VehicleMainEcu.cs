using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.Model
{
	using SafetySharp.Modeling;

	public enum VehicleStatus
	{
		Running,
		Off
	}

	public class VehicleMainEcu : Component
	{
		public VehicleStatus VehicleStatus;

		public VehicleStatus GetVehicleStatus()
		{
			return VehicleStatus;
		}
	}
}
