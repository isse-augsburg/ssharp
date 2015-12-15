using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine
{
	class Dialyzer : IBloodFlowIn, IBloodFlowOut
	{
		void Diffuse()
		{
			
		}

		void Update()
		{
			
		}

		public Func<BloodUnit> FlowUnitBefore { get; set; }
		public BloodUnit FlowUnitAfterwards()
		{
			return FlowUnitBefore();
		}
	}
}
