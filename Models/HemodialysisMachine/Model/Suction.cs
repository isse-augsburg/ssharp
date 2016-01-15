using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using Utilities.BidirectionalFlow;

	enum SuctionType
	{
		SourceDependentSuction,
		CustomSuction
	}

	class Suction : IElement<Suction>
	{
		public SuctionType SuctionType = SuctionType.SourceDependentSuction;
		public int CustomSuctionValue = 0;

		public void CopyValuesFrom(Suction from)
		{
			SuctionType = from.SuctionType;
			CustomSuctionValue = from.CustomSuctionValue;
		}
	}
}
