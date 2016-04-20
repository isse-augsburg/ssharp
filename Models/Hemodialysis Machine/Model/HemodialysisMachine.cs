using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model
{
	using Modeling;

	public class HdMachine : Component
	{
		public readonly Dialyzer Dialyzer;
		public readonly ExtracorporealBloodCircuit ExtracorporealBloodCircuit;
		public readonly DialyzingFluidDeliverySystem DialyzingFluidDeliverySystem;
		public readonly ControlSystem ControlSystem;

		[Hidden]
		public readonly BloodFlowDelegate FromPatientArtery;

		[Hidden]
		public readonly BloodFlowDelegate ToPatientVein;

		public HdMachine()
		{
			Dialyzer = new Dialyzer();
			ExtracorporealBloodCircuit = new ExtracorporealBloodCircuit();
			DialyzingFluidDeliverySystem = new DialyzingFluidDeliverySystem();
			ControlSystem = new ControlSystem(Dialyzer, ExtracorporealBloodCircuit, DialyzingFluidDeliverySystem);

			FromPatientArtery = new BloodFlowDelegate();
			ToPatientVein = new BloodFlowDelegate();
		}

		public void AddFlows(DialyzingFluidFlowCombinator dialysingFluidFlowCombinator, BloodFlowCombinator bloodFlowCombinator)
		{
			//Dialysate
			DialyzingFluidDeliverySystem.AddFlows(dialysingFluidFlowCombinator);
			//Blood
			bloodFlowCombinator.ConnectOutWithIn(FromPatientArtery,
				ExtracorporealBloodCircuit.BloodFlow);
			ExtracorporealBloodCircuit.AddFlows(bloodFlowCombinator);
			bloodFlowCombinator.ConnectOutWithIn(ExtracorporealBloodCircuit.BloodFlow,
				ToPatientVein);
			//Insert Stubs
			bloodFlowCombinator.ConnectOutWithIn(ExtracorporealBloodCircuit.ToDialyzer,
				Dialyzer.BloodFlow);
			bloodFlowCombinator.ConnectOutWithIn(Dialyzer.BloodFlow,
				ExtracorporealBloodCircuit.FromDialyzer);
			dialysingFluidFlowCombinator.ConnectOutWithIn(DialyzingFluidDeliverySystem.ToDialyzer,
				Dialyzer.DialyzingFluidFlow);
			dialysingFluidFlowCombinator.ConnectOutWithIn(Dialyzer.DialyzingFluidFlow,
				DialyzingFluidDeliverySystem.FromDialyzer);
		}

		public override void Update()
		{
			Update(Dialyzer,ControlSystem);
		}
	}
}
