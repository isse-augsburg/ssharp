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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling
{
	using SafetySharp.Modeling;
	using Utilities.BidirectionalFlow;
	
	// Also called dialysate or dialyzate
	public class DialyzingFluid : IFlowElement<DialyzingFluid>
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

		public void CopyValuesFrom(DialyzingFluid @from)
		{
			Quantity = from.Quantity;
			KindOfDialysate = from.KindOfDialysate;
			ContaminatedByBlood = from.ContaminatedByBlood;
			WasUsed = from.WasUsed;
			Temperature = from.Temperature;
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
				var target = Outgoings[i].Forward;
				target.CopyValuesFrom(source);
			}
			// first satisfy all CustomSuctions
			for (int i = 0; i < Number; i++)
			{
				var dependingOn = Outgoings[i].Backward;
				var target = Outgoings[i].Forward;
				if (dependingOn.SuctionType == SuctionType.CustomSuction)
				{
					target.Quantity = dependingOn.CustomSuctionValue;
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
					var target = Outgoings[i].Forward;
					if (dependingOn.SuctionType != SuctionType.CustomSuction)
					{
						target.Quantity = quantityForEachOfTheRest;
					}
				}
			}
		}

		public override void UpdateBackwardInternal()
		{
			var target = Incoming.Backward;
			target.CopyValuesFrom(Outgoings[0].Backward);
			for (int i = 1; i < Number; i++) //start with second element
			{
				var source = Outgoings[i].Backward;
				if (target.SuctionType == SuctionType.SourceDependentSuction || source.SuctionType == SuctionType.SourceDependentSuction)
				{
					target.SuctionType = SuctionType.SourceDependentSuction;
					target.CustomSuctionValue = 0;
				}
				else
				{
					target.SuctionType = SuctionType.CustomSuction;
					target.CustomSuctionValue += source.CustomSuctionValue;
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
			var target = Outgoing.Forward;
			target.CopyValuesFrom(Incomings[0].Forward);
			for (int i = 1; i < Number; i++) //start with second element
			{
				var source = Incomings[i].Forward;
				target.Quantity += source.Quantity;
				target.ContaminatedByBlood |= source.ContaminatedByBlood;
				target.WasUsed |= source.WasUsed;
				if (source.Temperature != QualitativeTemperature.BodyHeat)
					target.Temperature = source.Temperature;
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
