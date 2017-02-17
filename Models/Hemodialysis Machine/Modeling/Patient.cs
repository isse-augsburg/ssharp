// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling
{
	using SafetySharp.Modeling;

	public class Patient : Component
	{
		// Patient is the source and the sink of blood
		public readonly BloodFlowSource ArteryFlow = new BloodFlowSource();
		public readonly BloodFlowSink VeinFlow = new BloodFlowSink();

		[Range(0, 100, OverflowBehavior.Error)]
		public int Water = 50;
		[Range(0, 16, OverflowBehavior.Error)]
		public int SmallWasteProducts = 10;
		[Range(0, 8, OverflowBehavior.Error)]
		public int BigWasteProducts = 3; //Only removable by ultrafiltration

		public bool IsConnected = true;

		[Provided]
		public Blood CreateBlood()
		{
			Blood outgoingBlood;
			var incomingSuction = ArteryFlow.Outgoing.Backward;
			var hasSuction = incomingSuction.SuctionType == SuctionType.CustomSuction && incomingSuction.CustomSuctionValue > 0;
			if (hasSuction && IsConnected)
			{
				var totalUnitsToDeliver = ArteryFlow.Outgoing.Backward.CustomSuctionValue;
				var bigWasteUnitsToDeliver = totalUnitsToDeliver / 2;
				if (BigWasteProducts >= bigWasteUnitsToDeliver)
				{
					outgoingBlood.BigWasteProducts = bigWasteUnitsToDeliver;
					var waterUnitsToDeliver = totalUnitsToDeliver - bigWasteUnitsToDeliver;
					outgoingBlood.Water = waterUnitsToDeliver;
				} 
				else
				{
					outgoingBlood.BigWasteProducts = BigWasteProducts; // Deliver rest of unfiltrated blood or none
					var waterUnitsToDeliver = totalUnitsToDeliver - outgoingBlood.BigWasteProducts;
					outgoingBlood.Water = waterUnitsToDeliver;
				}
				if (SmallWasteProducts >= outgoingBlood.Water)
				{
					outgoingBlood.SmallWasteProducts = outgoingBlood.Water;
				}
				else
				{
					outgoingBlood.SmallWasteProducts = SmallWasteProducts; // Deliver rest of unfiltrated blood or none
				}
				Water -= outgoingBlood.Water;
				SmallWasteProducts -= outgoingBlood.SmallWasteProducts;
				BigWasteProducts -= outgoingBlood.BigWasteProducts;
				outgoingBlood.HasHeparin = true;
				outgoingBlood.ChemicalCompositionOk = true;
				outgoingBlood.GasFree = true;
				outgoingBlood.Pressure = QualitativePressure.GoodPressure;
				outgoingBlood.Temperature = QualitativeTemperature.BodyHeat;
			}
			else
			{
				outgoingBlood.Water = 0;
				outgoingBlood.BigWasteProducts = 0;
				outgoingBlood.SmallWasteProducts = 0;
				outgoingBlood.HasHeparin = true;
				outgoingBlood.ChemicalCompositionOk = true;
				outgoingBlood.GasFree = true;
				outgoingBlood.Pressure = QualitativePressure.NoPressure;
				outgoingBlood.Temperature = QualitativeTemperature.BodyHeat;
			}
			return outgoingBlood;
		}

		[Provided]
		public Suction CreateBloodSuction()
		{
			Suction outgoingSuction;
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction;
			outgoingSuction.CustomSuctionValue = 0;
			return outgoingSuction;
		}

		public bool IncomingBloodWasNotOk;

		[Provided]
		public void BloodReceived(Blood incomingBlood)
		{
			Water += incomingBlood.Water;
			SmallWasteProducts += incomingBlood.SmallWasteProducts;
			BigWasteProducts += incomingBlood.BigWasteProducts;
			
			var receivedSomething = incomingBlood.BigWasteProducts > 0 || incomingBlood.Water > 0;
			var compositionOk = incomingBlood.ChemicalCompositionOk && incomingBlood.GasFree &&
									(incomingBlood.Temperature == QualitativeTemperature.BodyHeat);
			IncomingBloodWasNotOk |= receivedSomething && !compositionOk;
		}

		[Provided]
		public void DoNothing(Suction incomingSuction)
		{
		}

		public Patient()
		{
			ArteryFlow.SendForward=CreateBlood;
			VeinFlow.SendBackward=CreateBloodSuction;
			VeinFlow.ReceivedForward=BloodReceived;
		}

		public void PrintBloodValues(string pointOfTime)
		{
			System.Console.Out.WriteLine("\t" + "Patient (" + pointOfTime + ")");
			System.Console.Out.WriteLine("\t\tWater: " + Water);
			System.Console.Out.WriteLine("\t\tSmallWasteProducts: " + SmallWasteProducts);
			System.Console.Out.WriteLine("\t\tBigWasteProducts: " + BigWasteProducts);
		}

		public string ValuesAsText()
		{
			return
			"Water: " + Water +
			"\nSmallWasteProducts: " + SmallWasteProducts +
			"\nBigWasteProducts: " + BigWasteProducts;
		}
	}
}
