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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling.DialyzingFluidDeliverySystem
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;

	public class SafetyBypass : Component
	{
		public readonly DialyzingFluidFlowInToOut MainFlow = new DialyzingFluidFlowInToOut();
		public readonly DialyzingFluidFlowSource DrainFlow = new DialyzingFluidFlowSource();

		public DialyzingFluid ToDrainValue = new DialyzingFluid();

		public bool BypassEnabled = false;

		[Provided]
		public virtual DialyzingFluid SetMainFlow(DialyzingFluid  fromPredecessor)
		{
			if (BypassEnabled || fromPredecessor.Temperature != QualitativeTemperature.BodyHeat)
			{
				ToDrainValue=fromPredecessor;
				DialyzingFluid toSuccessor;
				toSuccessor.Quantity = 0;
				toSuccessor.ContaminatedByBlood = false;
				toSuccessor.Temperature = QualitativeTemperature.TooCold;
				toSuccessor.WasUsed = false;
				toSuccessor.KindOfDialysate = KindOfDialysate.Water;
				return toSuccessor;
			}
			else
			{
				ToDrainValue.Quantity = 0;
				ToDrainValue.ContaminatedByBlood = false;
				ToDrainValue.Temperature = QualitativeTemperature.TooCold;
				ToDrainValue.WasUsed = false;
				ToDrainValue.KindOfDialysate = KindOfDialysate.Water;
				return fromPredecessor;
			}
		}

		[Provided]
		public Suction SetMainFlowSuction(Suction fromSuccessor)
		{
			return fromSuccessor;
		}

		[Provided]
		public DialyzingFluid SetDrainFlow()
		{
			return ToDrainValue;
		}

		public SafetyBypass()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
			DrainFlow.SendForward=SetDrainFlow;
		}


		public readonly Fault SafetyBypassFault = new PermanentFault { DemandType = Fault.DemandTypes.OnStartOfStep };

		[FaultEffect(Fault = nameof(SafetyBypassFault))]
		public class SafetyBypassFaultEffect : SafetyBypass
		{
			[Provided]
			public override DialyzingFluid SetMainFlow(DialyzingFluid  fromPredecessor)
			{
				return fromPredecessor;
			}
		}
	}
}