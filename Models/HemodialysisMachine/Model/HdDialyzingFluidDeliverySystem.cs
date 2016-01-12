using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using Utilities;

	// More details on the balance chamber on the internet:
	// http://principlesofdialysis.weebly.com/uploads/5/6/1/3/5613613/2008ccmodule4.pdf
	// -> Chapter "Volumetric UF Control"

	class DialyzingFluidWaterSupply
	{
		public readonly DialyzingFluidFlowSource DialyzingFluidFlow = new DialyzingFluidFlowSource((value) => { });
	}

	class DialyzingFluidWaterPreparation
	{
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment(In => In);
	}

	class DialyzingFluidConcentrateSupply
	{
		public readonly DialyzingFluidFlowSource Concentrate = new DialyzingFluidFlowSource((value) => { });
	}

	class DialyzingFluidPreparation
	{
		public readonly DialyzingFluidFlowSink Concentrate = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment(In => In);
	}

	class DialyzingUltraFiltrationPump
	{
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment(In => In);
	}

	class DialyzingFluidDrain
	{
		public readonly DialyzingFluidFlowSink DialyzingFluidFlow = new DialyzingFluidFlowSink();
	}

	class DialyzingFluidSafetyBypass
	{
		public readonly DialyzingFluidFlowInToOutSegment DialyzingFluidFlow = new DialyzingFluidFlowInToOutSegment(In => In);
		public readonly DialyzingFluidFlowSource ToDrain = new DialyzingFluidFlowSource((value) => { });
	}


	// Two identical chambers

	// Each chamber has
	//   * Two sides separated by diaphragm.
	//     One half to the dialyzer (fresh dialyzing fluid) and the other half from the dialyzer (used dialyzing fluid)
	//   * inlet valve and outlet valve
	// - Produce Dialysing Fluid: Fresh Dialysing Fluid to store in passive chamber / used dialysate from passive chamber to drain
	// - Use Dialysing Fluid.Stored in active chamber -> dialysator -> drain or store used in active chamber
	class BalanceChamber
	{
		public readonly DialyzingFluidFlowSink ProducedDialysingFluid = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowSink UsedDialysingFluid = new DialyzingFluidFlowSink();
		public readonly DialyzingFluidFlowSource StoredProducedDialysingFluid = new DialyzingFluidFlowSource((value) => { });
		public readonly DialyzingFluidFlowSource StoredUsedDialysingFluid = new DialyzingFluidFlowSource((value) => { });


		enum State
		{
			UseChamber1ForDialyzer,
			UseChamber2ForDialyzer
		}

		class SubChamber
		{
			private ValveState ValveToDialyser;
			private ValveState ValveToDrain;
			private ValveState ValveFromDialyzingFluidPreparation;
			private ValveState ValveFromDialyzer;

			private int FreshDialysingFluid;
			private int UsedDialysingFluid;


		}
	}


	// Also called dialyzing fluid delivery system
	class DialyzingFluidDeliverySystem
	{
		public readonly DialyzingFluidFlowUniqueOutgoingStub FromDialyzer = new DialyzingFluidFlowUniqueOutgoingStub();
		public readonly DialyzingFluidFlowUniqueIncomingStub ToDialyzer = new DialyzingFluidFlowUniqueIncomingStub();

		public readonly DialyzingFluidWaterSupply DialyzingFluidWaterSupply = new DialyzingFluidWaterSupply();
		public readonly DialyzingFluidWaterPreparation DialyzingFluidWaterPreparation = new DialyzingFluidWaterPreparation();
		public readonly DialyzingFluidConcentrateSupply DialyzingFluidConcentrateSupply = new DialyzingFluidConcentrateSupply();
		public readonly DialyzingFluidPreparation DialyzingFluidPreparation = new DialyzingFluidPreparation();
		public readonly BalanceChamber BalanceChamber = new BalanceChamber();
		public readonly DialyzingFluidDrain DialyzingFluidDrain = new DialyzingFluidDrain();
		public readonly DialyzingFluidSafetyBypass DialyzingFluidSafetyBypass = new DialyzingFluidSafetyBypass();

		public readonly DialyzingUltraFiltrationPump DialyzingUltraFiltrationPump = new DialyzingUltraFiltrationPump();
		
		public void AddFlows(DialyzingFluidFlowCombinator flowCombinator)
		{
			// The order of the connections matters
			flowCombinator.Connect(DialyzingFluidWaterSupply.DialyzingFluidFlow.Outgoing,
				DialyzingFluidWaterPreparation.DialyzingFluidFlow.Incoming);
			flowCombinator.Connect(DialyzingFluidConcentrateSupply.Concentrate.Outgoing,
				DialyzingFluidPreparation.Concentrate.Incoming);
			flowCombinator.Connect(DialyzingFluidWaterPreparation.DialyzingFluidFlow.Outgoing,
				DialyzingFluidPreparation.DialyzingFluidFlow.Incoming);
			flowCombinator.Connect(DialyzingFluidPreparation.DialyzingFluidFlow.Outgoing,
				BalanceChamber.ProducedDialysingFluid.Incoming);
			flowCombinator.Connect(BalanceChamber.StoredProducedDialysingFluid.Outgoing,
				DialyzingFluidSafetyBypass.DialyzingFluidFlow.Incoming);
			flowCombinator.Connect(DialyzingFluidSafetyBypass.DialyzingFluidFlow.Outgoing,
				ToDialyzer.Incoming);
			flowCombinator.Connect(
				FromDialyzer.Outgoing,
				new PortFlowIn<DialyzingFluid>[] {
					DialyzingUltraFiltrationPump.DialyzingFluidFlow.Incoming,
					BalanceChamber.UsedDialysingFluid.Incoming
				});
			flowCombinator.Connect(
				new PortFlowOut<DialyzingFluid>[] {
					DialyzingFluidSafetyBypass.ToDrain.Outgoing,
					BalanceChamber.StoredUsedDialysingFluid.Outgoing,
					DialyzingUltraFiltrationPump.DialyzingFluidFlow.Outgoing
				},
				DialyzingFluidDrain.DialyzingFluidFlow.Incoming);
		}

	}
}
