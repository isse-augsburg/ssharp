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
		public void SetDialyzingFluidFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		[Provided]
		public void SetBloodFlow(Blood outgoingElement, Blood incomingElement)
		{
			outgoingElement.CopyValuesFrom(incomingElement);
		}

		[Provided]
		public void SetBloodFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingBackward), nameof(SetDialyzingFluidFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingForward), nameof(SetDialyzingFluidFlow));
			Bind(nameof(BloodFlow.SetOutgoingBackward), nameof(SetBloodFlowSuction));
			Bind(nameof(BloodFlow.SetOutgoingForward), nameof(SetBloodFlow));
		}



		private void Diffuse()
		{
			
		}

		public override void Update()
		{
			
		}
	}
}
