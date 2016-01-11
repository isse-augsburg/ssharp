using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;

	class HdMachine : Component
	{
		private readonly DialyzingFluidFlowCombinator _dialysingFluidFlowCombinator;
		private readonly BloodFlowCombinator _bloodFlowCombinator;

		public readonly Dialyzer Dialyzer;
		public readonly ExtracorporealBloodCircuit ExtracorporealBloodCircuit;
		public readonly DialyzingFluidDeliverySystem DialyzingFluidDeliverySystem;
		public readonly ControlSystem ControlSystem;

		public readonly BloodFlowUniqueOutgoingStub FromPatientArtery;
		public readonly BloodFlowUniqueIncomingStub ToPatientVein;

		public HdMachine()
		{
			_dialysingFluidFlowCombinator = new DialyzingFluidFlowCombinator();
			_bloodFlowCombinator = new BloodFlowCombinator();

			Dialyzer = new Dialyzer();
			ExtracorporealBloodCircuit = new ExtracorporealBloodCircuit();
			DialyzingFluidDeliverySystem = new DialyzingFluidDeliverySystem();
			ControlSystem = new ControlSystem(Dialyzer, ExtracorporealBloodCircuit, DialyzingFluidDeliverySystem);

			FromPatientArtery = new BloodFlowUniqueOutgoingStub();
			ToPatientVein = new BloodFlowUniqueIncomingStub();
		}

		public void AddFlows()
		{
			//Dialysate
			DialyzingFluidDeliverySystem.AddFlows(_dialysingFluidFlowCombinator);
			//Blood
			_bloodFlowCombinator.Connect(FromPatientArtery.Outgoing,
				ExtracorporealBloodCircuit.BloodFlow.Incoming);
			ExtracorporealBloodCircuit.AddFlows(_bloodFlowCombinator);
			_bloodFlowCombinator.Connect(ExtracorporealBloodCircuit.BloodFlow.Outgoing,
				ToPatientVein.Incoming);
			//Insert Stubs
			_bloodFlowCombinator.Replace(ExtracorporealBloodCircuit.ToDialyzer.Incoming,
				Dialyzer.BloodFlow.Incoming);
			_bloodFlowCombinator.Replace(ExtracorporealBloodCircuit.FromDialyzer.Outgoing,
				Dialyzer.BloodFlow.Outgoing);
			_dialysingFluidFlowCombinator.Replace(DialyzingFluidDeliverySystem.ToDialyzer.Incoming,
				Dialyzer.DialyzingFluidFlow.Incoming);
			_dialysingFluidFlowCombinator.Replace(DialyzingFluidDeliverySystem.FromDialyzer.Outgoing,
				Dialyzer.DialyzingFluidFlow.Outgoing);

		}

		public override void Update()
		{
			_dialysingFluidFlowCombinator.UpdateFlows();
			_bloodFlowCombinator.UpdateFlows();
		}
	}
}
