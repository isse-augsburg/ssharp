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
		// Order of Provided Port call (determined by flowConnectors)
		// 1. Suction of DialyzingFluid is calculated
		// 2. Element of DialyzingFluid is calculated
		// 3. Suction of Blood is calculated
		// 4. Element of Blood is calculated

		public BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment();
		public DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		public int IncomingSuctionRateOnDialyzingFluidSide = 0;
		public int IncomingQuantityOfDialyzingFluid = 0; //Amount of BloodUnits we can clean.
		public QualitativeTemperature IncomingFluidTemperature;
		
		[Provided]
		public void SetDialyzingFluidFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			IncomingSuctionRateOnDialyzingFluidSide = incomingSuction.CustomSuctionValue;
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction;
		}

		[Provided]
		public void SetDialyzingFluidFlow(DialyzingFluid outgoing, DialyzingFluid incoming)
		{
			IncomingFluidTemperature = incoming.Temperature;
			IncomingQuantityOfDialyzingFluid = incoming.Quantity;
			outgoing.CopyValuesFrom(incoming);
			outgoing.Quantity = IncomingSuctionRateOnDialyzingFluidSide;
		}

		[Provided]
		public void SetBloodFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		[Provided]
		public void SetBloodFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
			outgoing.Temperature = IncomingFluidTemperature;
			// First step: Filtrate Blood
			if (IncomingQuantityOfDialyzingFluid >= outgoing.SmallWasteProducts)
			{
				outgoing.SmallWasteProducts = 0;
			}
			else
			{
				outgoing.SmallWasteProducts += IncomingQuantityOfDialyzingFluid;
			}
			// Second step: Ultra Filtration
			// To satisfy the incoming suction rate we must take the fluid from the blood.
			// The ultrafiltrationRate is the amount of fluid we take from the blood-side.
			var ultrafiltrationRate = IncomingSuctionRateOnDialyzingFluidSide - IncomingQuantityOfDialyzingFluid;
			
			if (ultrafiltrationRate < outgoing.BigWasteProducts)
			{
				outgoing.BigWasteProducts -= ultrafiltrationRate;
			}
			else
			{
				// Remove water instead of BigWasteProducts
				// Assume Water >= (ultrafiltrationRate - outgoing.BigWasteProducts)
				outgoing.Water -= (ultrafiltrationRate - outgoing.BigWasteProducts);
				outgoing.BigWasteProducts = 0;
			}
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingBackward), nameof(SetDialyzingFluidFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingForward), nameof(SetDialyzingFluidFlow));
			Bind(nameof(BloodFlow.SetOutgoingBackward), nameof(SetBloodFlowSuction));
			Bind(nameof(BloodFlow.SetOutgoingForward), nameof(SetBloodFlow));
		}
	}
}
