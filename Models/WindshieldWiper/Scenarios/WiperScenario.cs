using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.Scenarios
{
	using DataStructures;
	using SafetySharp.Modeling;

	public abstract class WiperScenario : Component
	{
		public virtual extern void WiperControlStalkSendRequest(WiperRequest request);
	}
}
