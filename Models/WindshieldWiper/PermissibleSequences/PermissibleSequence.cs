using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wiper.PermissibleSequences
{
	using SafetySharp.Modeling;

	public abstract class PermissibleSequence : Component
	{
		public abstract bool IsFailed();
	}
}
