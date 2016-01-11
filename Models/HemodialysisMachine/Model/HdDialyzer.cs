using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	class Dialyzer
	{
		public BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment(In => In);
		public DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment(In => In);

		void Diffuse()
		{
			
		}

		void Update()
		{
			
		}
	}
}
