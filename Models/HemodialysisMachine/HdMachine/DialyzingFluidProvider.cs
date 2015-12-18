using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.HdMachine
{
	// More details on the balance chamber on the internet:
	// http://principlesofdialysis.weebly.com/uploads/5/6/1/3/5613613/2008ccmodule4.pdf
	// -> Chapter "Volumetric UF Control"

	class DialyzingFluidProvider
	{
		class DialyzingFluidWaterPreparation
		{

		}

		class DialyzingFluidPreparation
		{
			
		}


		// Two identical chambers

		// Each chamber has
		//   * Two sides separated by diaphragm.
		//     One half to the dialyzer (fresh dialyzing fluid) and the other half from the dialyzer (used dialyzing fluid)
		//   * inlet valve and outlet valve
		class BalanceChamber : DialyzingFluidFlowDirect
		{
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

	}
}
