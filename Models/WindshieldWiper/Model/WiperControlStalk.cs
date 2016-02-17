using SafetySharp.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.Model
{
	public class WiperControlStalk : Component
	{
		public extern void SendRequest(WiperRequest request);
	}
}
