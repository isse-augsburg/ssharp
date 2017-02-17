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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling
{
	using SafetySharp.Modeling;
	using Utilities.BidirectionalFlow;
	
	// Also called dialysate or dialyzate
	public struct DialyzingFluid
	{
		[Hidden,Range(0, 8, OverflowBehavior.Error)]
		public int Quantity;
		[Hidden]
		public KindOfDialysate KindOfDialysate;
		[Hidden]
		public bool ContaminatedByBlood;
		[Hidden]
		public bool WasUsed;
		[Hidden]
		public QualitativeTemperature Temperature;
		
		/*
		public void CopyValuesFrom(DialyzingFluid @from)
		{
			Quantity = from.Quantity;
			KindOfDialysate = from.KindOfDialysate;
			ContaminatedByBlood = from.ContaminatedByBlood;
			WasUsed = from.WasUsed;
			Temperature = from.Temperature;
		}
		*/
		
		
		public static DialyzingFluid Default()
		{
			var dialyzingFluid = new DialyzingFluid();
			return dialyzingFluid;
		}

		public string ValuesAsText()
		{
			return "Quantity: " + Quantity.ToString();
		}

		public void PrintDialyzingFluidValues(string description)
		{
			System.Console.Out.WriteLine("\t" + description);
			System.Console.Out.WriteLine("\t\tQuantity: " + Quantity.ToString());
		}
	}


	public class DialyzingFluidFlowInToOut : FlowInToOut<DialyzingFluid,Suction>
	{
	}

	public class DialyzingFluidFlowSource : FlowSource<DialyzingFluid, Suction>
	{
	}

	public class DialyzingFluidFlowSink : FlowSink<DialyzingFluid, Suction>
	{
	}


	public class DialyzingFluidFlowSplitter : FlowSplitter<DialyzingFluid, Suction>
	{
		public DialyzingFluidFlowSplitter(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			var source = Incoming.Forward;
			var availableQuantity = source.Quantity;
			// Copy all needed values
			var countRest = 0;
			for (int i = 0; i < Number; i++)
			{
				Outgoings[i].Forward=source;
			}
			// first satisfy all CustomSuctions
			for (int i = 0; i < Number; i++)
			{
				var dependingOn = Outgoings[i].Backward;
				if (dependingOn.SuctionType == SuctionType.CustomSuction)
				{
					Outgoings[i].Forward.Quantity = dependingOn.CustomSuctionValue;
					availableQuantity -= dependingOn.CustomSuctionValue;
				}
				else
				{
					countRest++;
				}
			}
			// then satisfy the rest
			// (assume availableQuantity>=0)
			if (countRest > 0)
			{
				var quantityForEachOfTheRest = availableQuantity / countRest;
				for (int i = 0; i < Number; i++)
				{
					var dependingOn = Outgoings[i].Backward;
					if (dependingOn.SuctionType != SuctionType.CustomSuction)
					{
						Outgoings[i].Forward.Quantity = quantityForEachOfTheRest;
					}
				}
			}
		}

		public override void UpdateBackwardInternal()
		{
			Incoming.Backward=Outgoings[0].Backward;
			for (int i = 1; i < Number; i++) //start with second element
			{
				var source = Outgoings[i].Backward;
				if (Incoming.Backward.SuctionType == SuctionType.SourceDependentSuction || source.SuctionType == SuctionType.SourceDependentSuction)
				{
					Incoming.Backward.SuctionType = SuctionType.SourceDependentSuction;
					Incoming.Backward.CustomSuctionValue = 0;
				}
				else
				{
					Incoming.Backward.SuctionType = SuctionType.CustomSuction;
					Incoming.Backward.CustomSuctionValue += source.CustomSuctionValue;
				}
			}
		}
	}

	public class DialyzingFluidFlowMerger : FlowMerger<DialyzingFluid, Suction>
	{
		public DialyzingFluidFlowMerger(int number)
			: base(number)
		{
		}

		public override void UpdateForwardInternal()
		{
			Outgoing.Forward=Incomings[0].Forward;
			for (int i = 1; i < Number; i++) //start with second element
			{
				var source = Incomings[i].Forward;
				Outgoing.Forward.Quantity += source.Quantity;
				Outgoing.Forward.ContaminatedByBlood |= source.ContaminatedByBlood;
				Outgoing.Forward.WasUsed |= source.WasUsed;
				if (source.Temperature != QualitativeTemperature.BodyHeat)
					Outgoing.Forward.Temperature = source.Temperature;
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

	public class DialyzingFluidFlowComposite : FlowComposite<DialyzingFluid, Suction>
	{
	}

	public class DialyzingFluidFlowDelegate : FlowDelegate<DialyzingFluid, Suction>
	{
	}

	public class DialyzingFluidFlowCombinator : FlowCombinator<DialyzingFluid, Suction>
	{
		public override FlowMerger<DialyzingFluid, Suction> CreateFlowVirtualMerger(int elementNos)
		{
			return new DialyzingFluidFlowMerger(elementNos);
		}

		public override FlowSplitter<DialyzingFluid, Suction> CreateFlowVirtualSplitter(int elementNos)
		{
			return new DialyzingFluidFlowSplitter(elementNos);
		}
	}
}
