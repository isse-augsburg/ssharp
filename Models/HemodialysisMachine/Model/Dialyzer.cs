using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;

	class Dialyzer : Component
	{
		public BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment();
		public DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		[Provided]
		public void SetDialyzingFluidFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetDialyzingFluidFlowSuction(ref int outgoingSuction, int incomingSuction)
		{
			outgoingSuction = incomingSuction;
		}

		[Provided]
		public void SetBloodFlow(Blood outgoingElement, Blood incomingElement)
		{
			outgoingElement.CopyValuesFrom(incomingElement);
		}

		[Provided]
		public void SetBloodFlowSuction(ref int outgoingSuction, int incomingSuction)
		{
			outgoingSuction = incomingSuction;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingSuction), nameof(SetDialyzingFluidFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingElement), nameof(SetDialyzingFluidFlow));
			Bind(nameof(BloodFlow.SetOutgoingSuction), nameof(SetBloodFlowSuction));
			Bind(nameof(BloodFlow.SetOutgoingElement), nameof(SetBloodFlow));
		}



		void Diffuse()
		{
			
		}

		void Update()
		{
			
		}
	}
}
