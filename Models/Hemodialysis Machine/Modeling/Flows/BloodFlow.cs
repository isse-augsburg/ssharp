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

using System;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling
{
	using System.ComponentModel;
	using SafetySharp.Modeling;
	using Utilities.BidirectionalFlow;

	public struct Blood
	{
		[Hidden,Range(-1,7, OverflowBehavior.Error)]
		public int Water;
		[Hidden, Range(0, 8, OverflowBehavior.Error)]
		public int SmallWasteProducts;
		[Hidden, Range(0, 8, OverflowBehavior.Error)]
		public int BigWasteProducts;

		[Hidden]
		public bool HasHeparin;
		[Hidden]
		public bool ChemicalCompositionOk;
		[Hidden]
		public bool GasFree;
		[Hidden]
		public QualitativePressure Pressure;
		[Hidden]
		public QualitativeTemperature Temperature;

		public static Blood Default()
		{
			var blood = new Blood
			{
				Water = 0,
				SmallWasteProducts = 0,
				BigWasteProducts = 0,
				HasHeparin = false,
				ChemicalCompositionOk = true,
				GasFree = false,
				Pressure = QualitativePressure.NoPressure,
				Temperature = QualitativeTemperature.TooCold
			};
			return blood;
		}
		
		public bool HasWaterOrBigWaste()
		{
			return Water > 0 || BigWasteProducts > 0;
		}

		public string ValuesAsText()
		{
			return 
				"Water: " + Water +
				"\nSmallWasteProducts: " + SmallWasteProducts +
				"\nBigWasteProducts: " + BigWasteProducts;
		}

		public void PrintBloodValues(string description)
		{
			System.Console.Out.WriteLine("\t"+description);
			System.Console.Out.WriteLine("\t\tWater: " + Water);
			System.Console.Out.WriteLine("\t\tSmallWasteProducts: " + SmallWasteProducts);
			System.Console.Out.WriteLine("\t\tBigWasteProducts: " + BigWasteProducts);
		}
	}


	public struct BufferedBlood
	{
		[Range(-1, 7, OverflowBehavior.Error)]
		public int Water;
		[Range(0, 8, OverflowBehavior.Error)]
		public int SmallWasteProducts;
		[Range(0, 8, OverflowBehavior.Error)]
		public int BigWasteProducts;
		
		public bool HasHeparin;
		public bool ChemicalCompositionOk;
		public bool GasFree;
		public QualitativePressure Pressure;
		public QualitativeTemperature Temperature;


		public static BufferedBlood Default()
		{
			var blood = new BufferedBlood
			{
				Water = 0,
				SmallWasteProducts = 0,
				BigWasteProducts = 0,
				HasHeparin = false,
				ChemicalCompositionOk = true,
				GasFree = false,
				Pressure = QualitativePressure.NoPressure,
				Temperature = QualitativeTemperature.TooCold
			};
			return blood;
		}

		public static implicit operator BufferedBlood(Blood from)
		{
			var bufferedBlood = new BufferedBlood
			{
				Water = @from.Water,
				SmallWasteProducts = @from.SmallWasteProducts,
				BigWasteProducts = @from.BigWasteProducts,
				HasHeparin = @from.HasHeparin,
				ChemicalCompositionOk = @from.ChemicalCompositionOk,
				GasFree = @from.GasFree,
				Pressure = @from.Pressure,
				Temperature = @from.Temperature
			};
			return bufferedBlood;
		}

		public static implicit operator Blood(BufferedBlood from)
		{
			var blood = new Blood
			{
				Water = @from.Water,
				SmallWasteProducts = @from.SmallWasteProducts,
				BigWasteProducts = @from.BigWasteProducts,
				HasHeparin = @from.HasHeparin,
				ChemicalCompositionOk = @from.ChemicalCompositionOk,
				GasFree = @from.GasFree,
				Pressure = @from.Pressure,
				Temperature = @from.Temperature
			};
			return blood;
		}
	}


	public class BloodFlowInToOut : FlowInToOut<Blood, Suction>
	{
	}

	public class BloodFlowSource : FlowSource<Blood, Suction>
	{
	}

	public class BloodFlowSink : FlowSink<Blood, Suction>
	{
	}


	public class BloodFlowSplitter : FlowSplitter<Blood, Suction>, IIntFlowComponent
	{
		public BloodFlowSplitter(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			//Standard behavior: Copy each value
			for (int i = 0; i < Number; i++)
			{
				Outgoings[i].Forward=Incoming.Forward;
			}
			// TODO: No advanced splitting implemented, yet.
		}

		public override void UpdateBackwardInternal()
		{
			Incoming.Backward=Outgoings[0].Backward;
			
			for (int i = 1; i < Outgoings.Length; i++) //start with second element
			{
				if (Incoming.Backward.SuctionType == SuctionType.SourceDependentSuction || Outgoings[i].Backward.SuctionType == SuctionType.SourceDependentSuction)
				{
					Incoming.Backward.SuctionType = SuctionType.SourceDependentSuction;
					Incoming.Backward.CustomSuctionValue = 0;
				}
				else
				{
					Incoming.Backward.SuctionType = SuctionType.CustomSuction;
					Incoming.Backward.CustomSuctionValue += Outgoings[i].Backward.CustomSuctionValue;
				}
			}
		}
	}

	public class BloodFlowMerger : FlowMerger<Blood, Suction>, IIntFlowComponent
	{
		public BloodFlowMerger(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			Outgoing.Forward=Incomings[0].Forward;
			
			for (int i = 1; i < Incomings.Length; i++) //start with second element
			{
				var source = Incomings[i].Forward;
				Outgoing.Forward.ChemicalCompositionOk &= source.ChemicalCompositionOk;
				Outgoing.Forward.GasFree &= source.GasFree;
				Outgoing.Forward.BigWasteProducts += source.BigWasteProducts;
				Outgoing.Forward.SmallWasteProducts += source.SmallWasteProducts;
				Outgoing.Forward.HasHeparin |= source.HasHeparin;
				Outgoing.Forward.Water += source.Water;
				if (source.Temperature != QualitativeTemperature.BodyHeat)
					Outgoing.Forward.Temperature = source.Temperature;
				if (source.Pressure != QualitativePressure.GoodPressure)
					Outgoing.Forward.Pressure = source.Pressure;
			}
		}

		public override void UpdateBackwardInternal()
		{
			var source = Outgoing.Backward;
			
			if (source.SuctionType == SuctionType.SourceDependentSuction)
			{
				for (int i = 0; i < Number; i++)
				{
					Incomings[i].Backward=source;
				}
			}
			else
			{
				var suctionForEach = source.CustomSuctionValue / Number;
				for (int i = 0; i < Number; i++)
				{
					Incomings[i].Backward.SuctionType = SuctionType.CustomSuction;
					Incomings[i].Backward.CustomSuctionValue = suctionForEach;
				}
			}
		}
	}

	public class BloodFlowComposite : FlowComposite<Blood, Suction>, IIntFlowComponent
	{
	}

	public class BloodFlowDelegate : FlowDelegate<Blood, Suction>, IIntFlowComponent
	{
	}

	public class BloodFlowCombinator : FlowCombinator<Blood, Suction>
	{
		public override FlowMerger<Blood, Suction> CreateFlowVirtualMerger(int elementNos)
		{
			return new BloodFlowMerger(elementNos);
		}

		public override FlowSplitter<Blood, Suction> CreateFlowVirtualSplitter(int elementNos)
		{
			return new BloodFlowSplitter(elementNos);
		}
	}
}
