using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.Model
{
	using SafetySharp.Modeling;

	public class WiperActuator : Component
	{
		public WiperSpeed WiperSpeed { get; set; } = 0;
	}
}
