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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling
{
	using System.ComponentModel;
	using SafetySharp.Modeling;
	using Utilities.BidirectionalFlow;

	public class Blood : IFlowElement<Blood>
	{
		[Hidden,Range(-1,7, OverflowBehavior.Error)]
		public int Water = 0;
		[Hidden, Range(0, 8, OverflowBehavior.Error)]
		public int SmallWasteProducts = 0;
		[Hidden, Range(0, 8, OverflowBehavior.Error)]
		public int BigWasteProducts = 0;

		[Hidden]
		public bool HasHeparin = false;
		[Hidden]
		public bool ChemicalCompositionOk = true;
		[Hidden]
		public bool GasFree = false;
		[Hidden]
		public QualitativePressure Pressure = QualitativePressure.NoPressure;
		[Hidden]
		public QualitativeTemperature Temperature = QualitativeTemperature.TooCold;

		public void CopyValuesFrom(Blood from)
		{
			Water = from.Water;
			SmallWasteProducts = from.SmallWasteProducts;
			BigWasteProducts = from.BigWasteProducts;
			HasHeparin = from.HasHeparin;
			ChemicalCompositionOk = from.ChemicalCompositionOk;
			GasFree = from.GasFree;
			Pressure = from.Pressure;
			Temperature = from.Temperature;
		}

		public void CopyValuesFrom(BufferedBlood from)
		{
			Water = from.Water;
			SmallWasteProducts = from.SmallWasteProducts;
			BigWasteProducts = from.BigWasteProducts;
			HasHeparin = from.HasHeparin;
			ChemicalCompositionOk = from.ChemicalCompositionOk;
			GasFree = from.GasFree;
			Pressure = from.Pressure;
			Temperature = from.Temperature;
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


	public class BufferedBlood
	{
		[Range(-1, 7, OverflowBehavior.Error)]
		public int Water = 0;
		[Range(0, 8, OverflowBehavior.Error)]
		public int SmallWasteProducts = 0;
		[Range(0, 8, OverflowBehavior.Error)]
		public int BigWasteProducts = 0;
		
		public bool HasHeparin = false;
		public bool ChemicalCompositionOk = true;
		public bool GasFree = false;
		public QualitativePressure Pressure = QualitativePressure.NoPressure;
		public QualitativeTemperature Temperature = QualitativeTemperature.TooCold;

		public void CopyValuesFrom(Blood from)
		{
			Water = from.Water;
			SmallWasteProducts = from.SmallWasteProducts;
			BigWasteProducts = from.BigWasteProducts;
			HasHeparin = from.HasHeparin;
			ChemicalCompositionOk = from.ChemicalCompositionOk;
			GasFree = from.GasFree;
			Pressure = from.Pressure;
			Temperature = from.Temperature;
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
				Outgoings[i].Forward.CopyValuesFrom(Incoming.Forward);
			}
			// TODO: No advanced splitting implemented, yet.
		}

		public override void UpdateBackwardInternal()
		{
			var target = Incoming.Backward;
			target.CopyValuesFrom(Outgoings[0].Backward);
			
			for (int i = 1; i < Outgoings.Length; i++) //start with second element
			{
				if (target.SuctionType == SuctionType.SourceDependentSuction || Outgoings[i].Backward.SuctionType == SuctionType.SourceDependentSuction)
				{
					target.SuctionType = SuctionType.SourceDependentSuction;
					target.CustomSuctionValue = 0;
				}
				else
				{
					target.SuctionType = SuctionType.CustomSuction;
					target.CustomSuctionValue += Outgoings[i].Backward.CustomSuctionValue;
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
			var target = Outgoing.Forward;
			target.CopyValuesFrom(Incomings[0].Forward);
			
			for (int i = 1; i < Incomings.Length; i++) //start with second element
			{
				var source = Incomings[i].Forward;
				target.ChemicalCompositionOk &= source.ChemicalCompositionOk;
				target.GasFree &= source.GasFree;
				target.BigWasteProducts += source.BigWasteProducts;
				target.SmallWasteProducts += source.SmallWasteProducts;
				target.HasHeparin |= source.HasHeparin;
				target.Water += source.Water;
				if (source.Temperature != QualitativeTemperature.BodyHeat)
					target.Temperature = source.Temperature;
				if (source.Pressure != QualitativePressure.GoodPressure)
					target.Pressure = source.Pressure;
			}
		}

		public override void UpdateBackwardInternal()
		{
			var source = Outgoing.Backward;
			
			if (source.SuctionType == SuctionType.SourceDependentSuction)
			{
				for (int i = 0; i < Number; i++)
				{
					var target = Incomings[i].Backward;
					target.CopyValuesFrom(source);
				}
			}
			else
			{
				var suctionForEach = source.CustomSuctionValue / Number;
				for (int i = 0; i < Number; i++)
				{
					var target = Incomings[i].Backward;
					target.SuctionType = SuctionType.CustomSuction;
					target.CustomSuctionValue = suctionForEach;
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
