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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling.DialyzingFluidDeliverySystem
{
	using SafetySharp.Modeling;
	/*

	// Each chamber has
	//   * Two sides separated by diaphragm.
	//     One half to the dialyzer (fresh dialyzing fluid) and the other half from the dialyzer (used dialyzing fluid)
	//   * inlet valve and outlet valve
	// - Produce Dialysing Fluid: Fresh Dialysing Fluid to store in passive chamber / used dialysate from passive chamber to drain
	// - Use Dialysing Fluid.Stored in active chamber -> dialysator -> drain or store used in active chamber
	class DetailedBalanceChamber : Component
	{
		public readonly DialyzingFluidFlowSink ProducedDialysingFluid = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowSink UsedDialysingFluid = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowSource StoredProducedDialysingFluid = new DialyzingFluidFlowSource();
		public readonly DialyzingFluidFlowSource StoredUsedDialysingFluid = new DialyzingFluidFlowSource();

		public enum ChamberForDialyzerEnum
		{
			UseChamber1ForDialyzer,
			UseChamber2ForDialyzer
		}

		public ChamberForDialyzerEnum ChamberForDialyzer = ChamberForDialyzerEnum.UseChamber1ForDialyzer;

		public class Chamber
		{
			//public ValveState ValveToDialyser;
			//public ValveState ValveToDrain;
			//public ValveState ValveFromDialyzingFluidPreparation;
			//public ValveState ValveFromDialyzer;

			public DialyzingFluid StoredProducedDialysingFluid = new DialyzingFluid();
			public DialyzingFluid StoredUsedProducedDialysingFluid = new DialyzingFluid();

		}

		public Chamber Chamber1 = new Chamber();
		public Chamber Chamber2 = new Chamber();

		public DetailedBalanceChamber()
		{
			// Assume we have a rinsed Balance Chamber.
			// Chamber 1 is full of fresh DialysingFluid
			Chamber1.StoredProducedDialysingFluid.Quantity = 12;
			Chamber1.StoredProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber1.StoredProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber1.StoredProducedDialysingFluid.WasUsed = false;
			Chamber1.StoredProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;

			Chamber1.StoredUsedProducedDialysingFluid.Quantity = 0;
			Chamber1.StoredUsedProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber1.StoredUsedProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber1.StoredUsedProducedDialysingFluid.WasUsed = true;
			Chamber1.StoredUsedProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;

			Chamber2.StoredProducedDialysingFluid.Quantity = 0;
			Chamber2.StoredProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber2.StoredProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber2.StoredProducedDialysingFluid.WasUsed = false;
			Chamber2.StoredProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;

			Chamber2.StoredUsedProducedDialysingFluid.Quantity = 12;
			Chamber2.StoredUsedProducedDialysingFluid.ContaminatedByBlood = false;
			Chamber2.StoredUsedProducedDialysingFluid.Temperature = QualitativeTemperature.BodyHeat;
			Chamber2.StoredUsedProducedDialysingFluid.WasUsed = true;
			Chamber2.StoredUsedProducedDialysingFluid.KindOfDialysate = KindOfDialysate.Bicarbonate;
		}

		[Provided]
		public void MakeSuctionOnSource(Suction outgoingSuction)
		{
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction; // The suction depends on the pump before
			outgoingSuction.CustomSuctionValue = 0;
		}

		[Provided]
		public void MakeSuctionOnDrain(Suction outgoingSuction)
		{
			outgoingSuction.SuctionType = SuctionType.SourceDependentSuction; // The suction depends on the membrane
			outgoingSuction.CustomSuctionValue = 0;
		}

		[Provided]
		public void PushDialisateToDialysator(DialyzingFluid outgoing)
		{
			var quantityOfIncomingUsedDialysate = UsedDialysingFluid.Incoming.ForwardFromPredecessor.Quantity;
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredProducedDialysingFluid.Quantity >= quantityOfIncomingUsedDialysate)
				{
					outgoing.CopyValuesFrom(Chamber1.StoredProducedDialysingFluid);
					outgoing.Quantity = quantityOfIncomingUsedDialysate;
					Chamber1.StoredProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredProducedDialysingFluid.Quantity >= quantityOfIncomingUsedDialysate)
				{
					outgoing.CopyValuesFrom(Chamber2.StoredProducedDialysingFluid);
					outgoing.Quantity = quantityOfIncomingUsedDialysate;
					Chamber2.StoredProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
		}

		[Provided]
		public void PushDialysateToDrain(DialyzingFluid outgoing)
		{
			var quantityOfFreshDialysate = ProducedDialysingFluid.Incoming.ForwardFromPredecessor.Quantity;
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredUsedProducedDialysingFluid.Quantity >= quantityOfFreshDialysate)
				{
					outgoing.CopyValuesFrom(Chamber1.StoredUsedProducedDialysingFluid);
					outgoing.Quantity = quantityOfFreshDialysate;
					Chamber1.StoredUsedProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredUsedProducedDialysingFluid.Quantity >= quantityOfFreshDialysate)
				{
					outgoing.CopyValuesFrom(Chamber2.StoredUsedProducedDialysingFluid);
					outgoing.Quantity = quantityOfFreshDialysate;
					Chamber2.StoredUsedProducedDialysingFluid.Quantity -= outgoing.Quantity;
				}
			}
		}


		[Provided]
		public void ReceivedSuctionOnStoredProducedDialyzingFluid(Suction incomingSuction)
		{
		}

		[Provided]
		public void ReceivedProducedDialyzingFluid(DialyzingFluid incomingElement)
		{
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredProducedDialysingFluid.Quantity <= 20)
				{
					Chamber1.StoredProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredProducedDialysingFluid.Quantity <= 20)
				{
					Chamber2.StoredProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
		}

		[Provided]
		public void ReceivedSuctionOnStoredUsedDialyzingFluid(Suction incomingSuction)
		{
		}

		[Provided]
		public void ReceivedUsedDialyzingFluid(DialyzingFluid incomingElement)
		{
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer)
			{
				if (Chamber1.StoredUsedProducedDialysingFluid.Quantity <= 20)
				{
					Chamber1.StoredUsedProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
			else
			{
				if (Chamber2.StoredUsedProducedDialysingFluid.Quantity <= 20)
				{
					Chamber2.StoredUsedProducedDialysingFluid.Quantity += incomingElement.Quantity;
				}
			}
		}

		protected override void CreateBindings()
		{
			Bind(nameof(ProducedDialysingFluid.SetOutgoingBackward), nameof(MakeSuctionOnSource));
			Bind(nameof(UsedDialysingFluid.SetOutgoingBackward), nameof(MakeSuctionOnDrain));
			Bind(nameof(StoredProducedDialysingFluid.SetOutgoingForward), nameof(PushDialisateToDialysator));
			Bind(nameof(StoredUsedDialysingFluid.SetOutgoingForward), nameof(PushDialysateToDrain));
			Bind(nameof(ProducedDialysingFluid.ForwardFromPredecessorWasUpdated), nameof(ReceivedProducedDialyzingFluid));
			Bind(nameof(UsedDialysingFluid.ForwardFromPredecessorWasUpdated), nameof(ReceivedUsedDialyzingFluid));
			Bind(nameof(StoredProducedDialysingFluid.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuctionOnStoredProducedDialyzingFluid));
			Bind(nameof(StoredUsedDialysingFluid.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuctionOnStoredUsedDialyzingFluid));
		}

		public override void Update()
		{
			if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber1ForDialyzer && Chamber1.StoredProducedDialysingFluid.Quantity == 4)
			{
				ChamberForDialyzer = ChamberForDialyzerEnum.UseChamber2ForDialyzer;
			}
			else if (ChamberForDialyzer == ChamberForDialyzerEnum.UseChamber2ForDialyzer && Chamber2.StoredProducedDialysingFluid.Quantity == 4)
			{
				ChamberForDialyzer = ChamberForDialyzerEnum.UseChamber1ForDialyzer;
			}
		}
	}*/
}
