using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;

	public class Dialyzer : Component
	{
		// Order of Provided Port call (determined by flowConnectors)
		// 1. Suction of DialyzingFluid is calculated
		// 2. Element of DialyzingFluid is calculated
		// 3. Suction of Blood is calculated
		// 4. Element of Blood is calculated

		public BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment();
		public DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment();

		[Range(0, 8, OverflowBehavior.Error)]
		public int IncomingSuctionRateOnDialyzingFluidSide = 0;
		[Range(0, 8, OverflowBehavior.Error)]
		public int IncomingQuantityOfDialyzingFluid = 0; //Amount of BloodUnits we can clean.

		public QualitativeTemperature IncomingFluidTemperature;

		public bool MembraneIntact = true;


		[Provided]
		public void SetDialyzingFluidFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			if (incomingSuction.SuctionType==SuctionType.SourceDependentSuction)
				throw new Exception("Model Bug");
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
			outgoing.WasUsed = true;
		}

		[Provided]
		public void SetBloodFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		[Provided]
		public void SetBloodFlow(Blood outgoing, Blood incoming)
		{
			if (incoming.Water > 0 || incoming.BigWasteProducts > 0)
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
					outgoing.SmallWasteProducts -= IncomingQuantityOfDialyzingFluid;
				}
				// Second step: Ultra Filtration
				// To satisfy the incoming suction rate we must take the fluid from the blood.
				// The ultrafiltrationRate is the amount of fluid we take from the blood-side.
				var ultrafiltrationRate = IncomingSuctionRateOnDialyzingFluidSide - IncomingQuantityOfDialyzingFluid;
				if (ultrafiltrationRate >= 0)
				{
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
			}
			else
			{
				outgoing.CopyValuesFrom(incoming);
			}
			if (!MembraneIntact)
			{
				outgoing.ChemicalCompositionOk = false;
			}
		}

		public override void Update()
		{
			
		}

		protected override void CreateBindings()
		{
			Bind(nameof(DialyzingFluidFlow.SetOutgoingBackward), nameof(SetDialyzingFluidFlowSuction));
			Bind(nameof(DialyzingFluidFlow.SetOutgoingForward), nameof(SetDialyzingFluidFlow));
			Bind(nameof(BloodFlow.SetOutgoingBackward), nameof(SetBloodFlowSuction));
			Bind(nameof(BloodFlow.SetOutgoingForward), nameof(SetBloodFlow));
		}


		public readonly Fault DialyzerMembraneRupturesFault = new TransientFault();

		[FaultEffect(Fault = nameof(DialyzerMembraneRupturesFault))]
		public class DialyzerMembraneRupturesFaultEffect : Dialyzer
		{
			public override void Update()
			{
				base.Update();
				MembraneIntact = false;
			}
		}
	}
}
