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

namespace HemodialysisMachine.Model
{
	using Utilities.BidirectionalFlow;
	
	// Also called dialysate or dialyzate
	public class DialyzingFluid : IElement<DialyzingFluid>
	{
		public int Quantity;
		public KindOfDialysate KindOfDialysate;
		public bool ContaminatedByBlood;
		public bool WasUsed;
		public QualitativeTemperature Temperature;

		public void CopyValuesFrom(DialyzingFluid @from)
		{
			Quantity = from.Quantity;
			KindOfDialysate = from.KindOfDialysate;
			ContaminatedByBlood = from.ContaminatedByBlood;
			WasUsed = from.WasUsed;
			Temperature = from.Temperature;
		}

		public void PrintDialyzingFluidValues(string description)
		{
			System.Console.Out.WriteLine("\t" + description);
			System.Console.Out.WriteLine("\t\tQuantity: " + Quantity.ToString());
		}
	}


	public class DialyzingFluidFlowInToOutSegment : FlowInToOutSegment<DialyzingFluid,Suction>
	{
	}

	public class DialyzingFluidFlowSource : FlowSource<DialyzingFluid, Suction>
	{
	}

	public class DialyzingFluidFlowSink : FlowSink<DialyzingFluid, Suction>
	{
	}

	public class DialyzingFluidFlowComposite : FlowComposite<DialyzingFluid, Suction>
	{
	}


	public class DialyzingFluidFlowVirtualSplitter : FlowVirtualSplitter<DialyzingFluid, Suction>
	{
		public DialyzingFluidFlowVirtualSplitter(int number)
			: base(number)
		{
		}

		public override void SplitForwards(DialyzingFluid source, DialyzingFluid[] targets, Suction[] dependingOn)
		{
			var number = targets.Length;
			var availableQuantity = source.Quantity;
			// Copy all needed values
			var countRest = 0;
			for (int i = 0; i < number; i++)
			{
				targets[i].CopyValuesFrom(source);
			}
			// first satisfy all CustomSuctions
			for (int i = 0; i < number; i++)
			{
				if (dependingOn[i].SuctionType == SuctionType.CustomSuction)
				{
					targets[i].Quantity = dependingOn[i].CustomSuctionValue;
					availableQuantity -= dependingOn[i].CustomSuctionValue;
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
				for (int i = 0; i < number; i++)
				{
					if (dependingOn[i].SuctionType != SuctionType.CustomSuction)
					{
						targets[i].Quantity = quantityForEachOfTheRest;
					}
				}
			}
		}

		public override void MergeBackwards(Suction[] sources, Suction target)
		{
			target.CopyValuesFrom(sources[0]);
			var number = sources.Length;
			for (int i = 1; i < number; i++) //start with second element
			{
				if (target.SuctionType == SuctionType.SourceDependentSuction || sources[i].SuctionType == SuctionType.SourceDependentSuction)
				{
					target.SuctionType = SuctionType.SourceDependentSuction;
					target.CustomSuctionValue = 0;
				}
				else
				{
					target.SuctionType = SuctionType.CustomSuction;
					target.CustomSuctionValue += sources[i].CustomSuctionValue;
				}
			}
		}
	}

	public class DialyzingFluidFlowVirtualMerger : FlowVirtualMerger<DialyzingFluid, Suction>
	{
		public DialyzingFluidFlowVirtualMerger(int number)
			: base(number)
		{
		}

		public override void SplitBackwards(Suction source, Suction[] targets)
		{
			var number = targets.Length;

			if (source.SuctionType == SuctionType.SourceDependentSuction)
			{
				for (int i = 0; i < number; i++)
				{
					targets[i].CopyValuesFrom(source);
				}
			}
			else
			{
				var suctionForEach = source.CustomSuctionValue / number;
				for (int i = 0; i < number; i++)
				{
					targets[i].SuctionType = SuctionType.CustomSuction;
					targets[i].CustomSuctionValue = suctionForEach;
				}
			}
		}

		public override void MergeForwards(DialyzingFluid[] sources, DialyzingFluid target, Suction dependingOn)
		{
			target.CopyValuesFrom(sources[0]);
			var number = sources.Length;
			for (int i = 1; i < number; i++) //start with second element
			{
				target.Quantity += sources[i].Quantity;
				target.ContaminatedByBlood |= sources[i].ContaminatedByBlood;
				target.WasUsed |= sources[i].WasUsed;
				if (sources[i].Temperature != QualitativeTemperature.BodyHeat)
					target.Temperature = sources[i].Temperature;
			}
		}
	}

	public class DialyzingFluidFlowCombinator : FlowCombinator<DialyzingFluid, Suction>
	{
		public override FlowVirtualMerger<DialyzingFluid, Suction> CreateFlowVirtualMerger(int elementNos)
		{
			return new DialyzingFluidFlowVirtualMerger(elementNos);
		}

		public override FlowVirtualSplitter<DialyzingFluid, Suction> CreateFlowVirtualSplitter(int elementNos)
		{
			return new DialyzingFluidFlowVirtualSplitter(elementNos);
		}
	}

	public class DialyzingFluidFlowUniqueOutgoingStub : FlowUniqueOutgoingStub<DialyzingFluid, Suction>
	{
	}

	public class DialyzingFluidFlowUniqueIncomingStub : FlowUniqueIncomingStub<DialyzingFluid, Suction>
	{
	}
}
