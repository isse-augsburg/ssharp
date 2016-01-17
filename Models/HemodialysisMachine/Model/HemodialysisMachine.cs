using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;

	public class HdMachine : Component
	{
		public readonly Dialyzer Dialyzer;
		public readonly ExtracorporealBloodCircuit ExtracorporealBloodCircuit;
		public readonly DialyzingFluidDeliverySystem DialyzingFluidDeliverySystem;
		public readonly ControlSystem ControlSystem;

		[Hidden]
		public readonly BloodFlowUniqueOutgoingStub FromPatientArtery;

		[Hidden]
		public readonly BloodFlowUniqueIncomingStub ToPatientVein;

		public HdMachine()
		{
			Dialyzer = new Dialyzer();
			ExtracorporealBloodCircuit = new ExtracorporealBloodCircuit();
			DialyzingFluidDeliverySystem = new DialyzingFluidDeliverySystem();
			ControlSystem = new ControlSystem(Dialyzer, ExtracorporealBloodCircuit, DialyzingFluidDeliverySystem);

			FromPatientArtery = new BloodFlowUniqueOutgoingStub();
			ToPatientVein = new BloodFlowUniqueIncomingStub();
		}

		public void AddFlows(DialyzingFluidFlowCombinator dialysingFluidFlowCombinator, BloodFlowCombinator bloodFlowCombinator)
		{
			//Dialysate
			DialyzingFluidDeliverySystem.AddFlows(dialysingFluidFlowCombinator);
			//Blood
			bloodFlowCombinator.Connect(FromPatientArtery.Outgoing,
				ExtracorporealBloodCircuit.BloodFlow.Incoming);
			ExtracorporealBloodCircuit.AddFlows(bloodFlowCombinator);
			bloodFlowCombinator.Connect(ExtracorporealBloodCircuit.BloodFlow.Outgoing,
				ToPatientVein.Incoming);
			//Insert Stubs
			bloodFlowCombinator.Replace(ExtracorporealBloodCircuit.ToDialyzer.Incoming,
				Dialyzer.BloodFlow.Incoming);
			bloodFlowCombinator.Replace(ExtracorporealBloodCircuit.FromDialyzer.Outgoing,
				Dialyzer.BloodFlow.Outgoing);
			dialysingFluidFlowCombinator.Replace(DialyzingFluidDeliverySystem.ToDialyzer.Incoming,
				Dialyzer.DialyzingFluidFlow.Incoming);
			dialysingFluidFlowCombinator.Replace(DialyzingFluidDeliverySystem.FromDialyzer.Outgoing,
				Dialyzer.DialyzingFluidFlow.Outgoing);
		}

		public override void Update()
		{
			Update(ControlSystem);
		}
	}
}
