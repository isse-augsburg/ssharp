using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.DialyzingFluidDeliverySystem
{
	using Modeling;
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
		public readonly SafetyBypass DialyzingFluidSafetyBypass = new SafetyBypass();

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
				DialyzingFluidSafetyBypass.MainFlow);
			flowCombinator.ConnectOutWithIn(DialyzingFluidSafetyBypass.MainFlow,
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
					DialyzingFluidSafetyBypass.DrainFlow,
					BalanceChamber.StoredUsedDialysingFluid,
					DialyzingUltraFiltrationPump.MainFlow
				},
				DialyzingFluidDrain.DrainFlow);

			BalanceChamber.AddFlows(flowCombinator);
		}

	}
}
