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
	using Utilities;
	using Utilities.BidirectionalFlow;

	// More details on the balance chamber on the internet:
	// http://principlesofdialysis.weebly.com/uploads/5/6/1/3/5613613/2008ccmodule4.pdf
	// -> Chapter "Volumetric UF Control"

	// Also called dialyzing fluid delivery system
	public class DialyzingFluidDeliverySystem : Component
	{
		public readonly DialyzingFluidFlowDelegate FromDialyzer = new DialyzingFluidFlowDelegate();
		public readonly DialyzingFluidFlowDelegate ToDialyzer = new DialyzingFluidFlowDelegate();

		public readonly WaterSupply WaterSupply = new WaterSupply();
		public readonly WaterPreparation WaterPreparation = new WaterPreparation();
		public readonly ConcentrateSupply ConcentrateSupply = new ConcentrateSupply();
		public readonly DialyzingFluidPreparation DialyzingFluidPreparation = new DialyzingFluidPreparation();
		public readonly SimplifiedBalanceChamber BalanceChamber = new SimplifiedBalanceChamber();
		public readonly Pump PumpToBalanceChamber = new Pump();
		public readonly Drain DialyzingFluidDrain = new Drain();
		public readonly SafetyBypass SafetyBypass = new SafetyBypass();

		public readonly Pump DialyzingUltraFiltrationPump = new Pump();

		public DialyzingFluidDeliverySystem()
		{
			DialyzingUltraFiltrationPump.PumpDefect.Name = "UltrafiltrationPumpDefect";
			PumpToBalanceChamber.PumpDefect.Name = "PumpToBalanceChamberDefect";
		}

		public void AddFlows(DialyzingFluidFlowCombinator flowCombinator)
		{
			// The order of the connections matters
			flowCombinator.ConnectOutWithIn(WaterSupply.MainFlow,
				WaterPreparation.MainFlow);
			flowCombinator.ConnectOutWithIn(ConcentrateSupply.Concentrate,
				DialyzingFluidPreparation.Concentrate);
			flowCombinator.ConnectOutWithIn(WaterPreparation.MainFlow,
				DialyzingFluidPreparation.DialyzingFluidFlow);
			flowCombinator.ConnectOutWithIn(DialyzingFluidPreparation.DialyzingFluidFlow,
				BalanceChamber.ProducedDialysingFluid);
			flowCombinator.ConnectOutWithIn(BalanceChamber.StoredProducedDialysingFluid,
				SafetyBypass.MainFlow);
			flowCombinator.ConnectOutWithIn(SafetyBypass.MainFlow,
				ToDialyzer);
			flowCombinator.ConnectOutWithIns(
				FromDialyzer,
				new IFlowComponentUniqueIncoming<DialyzingFluid, Suction>[] {
					DialyzingUltraFiltrationPump.MainFlow,
					PumpToBalanceChamber.MainFlow
				});
			flowCombinator.ConnectOutWithIn(PumpToBalanceChamber.MainFlow,
				BalanceChamber.UsedDialysingFluid);
			flowCombinator.ConnectOutsWithIn(
				new IFlowComponentUniqueOutgoing<DialyzingFluid, Suction>[] {
					SafetyBypass.DrainFlow,
					BalanceChamber.StoredUsedDialysingFluid,
					DialyzingUltraFiltrationPump.MainFlow
				},
				DialyzingFluidDrain.DrainFlow);

			BalanceChamber.AddFlows(flowCombinator);
		}

	}
}
