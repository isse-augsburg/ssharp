// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model
{
	using Modeling;

	public class Dialyzer : Component
	{
		// Order of Provided Port call (determined by flowConnectors)
		// 1. Suction of DialyzingFluid is calculated
		// 2. Element of DialyzingFluid is calculated
		// 3. Suction of Blood is calculated
		// 4. Element of Blood is calculated

		public BloodFlowInToOut BloodFlow = new BloodFlowInToOut();
		public DialyzingFluidFlowInToOut DialyzingFluidFlow = new DialyzingFluidFlowInToOut();

		[Range(0, 8, OverflowBehavior.Error)]
		public int IncomingSuctionRateOnDialyzingFluidSide = 0;
		[Range(0, 8, OverflowBehavior.Error)]
		public int IncomingQuantityOfDialyzingFluid = 0; //Amount of BloodUnits we can clean.

		public QualitativeTemperature IncomingFluidTemperature;

		public bool MembraneIntact = true;


		[Provided]
		public void SetDialyzingFluidFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			//Assume incomingSuction.SuctionType == SuctionType.CustomSuction;
			if (fromSuccessor.SuctionType==SuctionType.SourceDependentSuction)
				throw new Exception("Model Bug");
			IncomingSuctionRateOnDialyzingFluidSide = fromSuccessor.CustomSuctionValue;
			toPredecessor.CustomSuctionValue = 0;
			toPredecessor.SuctionType = SuctionType.SourceDependentSuction;
		}

		[Provided]
		public void SetDialyzingFluidFlow(DialyzingFluid  toSuccessor, DialyzingFluid  fromPredecessor)
		{
			IncomingFluidTemperature = fromPredecessor.Temperature;
			IncomingQuantityOfDialyzingFluid = fromPredecessor.Quantity;
			toSuccessor.CopyValuesFrom(fromPredecessor);
			toSuccessor.Quantity = IncomingSuctionRateOnDialyzingFluidSide;
			toSuccessor.WasUsed = true;
		}

		[Provided]
		public void SetBloodFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		[Provided]
		public void SetBloodFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			if (fromPredecessor.Water > 0 || fromPredecessor.BigWasteProducts > 0)
			{
				toSuccessor.CopyValuesFrom(fromPredecessor);
				toSuccessor.Temperature = IncomingFluidTemperature;
				// First step: Filtrate Blood
				if (IncomingQuantityOfDialyzingFluid >= toSuccessor.SmallWasteProducts)
				{
					toSuccessor.SmallWasteProducts = 0;
				}
				else
				{
					toSuccessor.SmallWasteProducts -= IncomingQuantityOfDialyzingFluid;
				}
				// Second step: Ultra Filtration
				// To satisfy the incoming suction rate we must take the fluid from the blood.
				// The ultrafiltrationRate is the amount of fluid we take from the blood-side.
				var ultrafiltrationRate = IncomingSuctionRateOnDialyzingFluidSide - IncomingQuantityOfDialyzingFluid;
				if (ultrafiltrationRate >= 0)
				{
					if (ultrafiltrationRate < toSuccessor.BigWasteProducts)
					{
						toSuccessor.BigWasteProducts -= ultrafiltrationRate;
					}
					else
					{
						// Remove water instead of BigWasteProducts
						// Assume Water >= (ultrafiltrationRate - toSuccessor.BigWasteProducts)
						toSuccessor.Water -= (ultrafiltrationRate - toSuccessor.BigWasteProducts);
						toSuccessor.BigWasteProducts = 0;
					}
				}
			}
			else
			{
				toSuccessor.CopyValuesFrom(fromPredecessor);
			}
			if (!MembraneIntact)
			{
				toSuccessor.ChemicalCompositionOk = false;
			}
		}

		public override void Update()
		{
			
		}

		protected override void CreateBindings()
		{
			DialyzingFluidFlow.UpdateBackward = SetDialyzingFluidFlowSuction;
			DialyzingFluidFlow.UpdateForward = SetDialyzingFluidFlow;
			BloodFlow.UpdateBackward = SetBloodFlowSuction;
			BloodFlow.UpdateForward = SetBloodFlow;
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
