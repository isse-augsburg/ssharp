using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using Utilities.BidirectionalFlow;

	class Suction : IElement<Suction>
	{
		public int Value = 0;
		public bool Anything = false;

		public void CopyValuesFrom(Suction from)
		{
			Value = from.Value;
			Anything = from.Anything;
		}
	}
}
