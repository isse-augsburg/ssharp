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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling
{
	using SafetySharp.Modeling;

	public class HdMachine : Component
	{
		public readonly Dialyzer Dialyzer;
		public readonly ExtracorporealBloodCircuit.ExtracorporealBloodCircuit ExtracorporealBloodCircuit;
		public readonly DialyzingFluidDeliverySystem.DialyzingFluidDeliverySystem DialyzingFluidDeliverySystem;
		public readonly ControlSystem ControlSystem;

		[Hidden]
		public readonly BloodFlowDelegate FromPatientArtery;

		[Hidden]
		public readonly BloodFlowDelegate ToPatientVein;

		public HdMachine()
		{
			Dialyzer = new Dialyzer();
			ExtracorporealBloodCircuit = new ExtracorporealBloodCircuit.ExtracorporealBloodCircuit();
			DialyzingFluidDeliverySystem = new DialyzingFluidDeliverySystem.DialyzingFluidDeliverySystem();
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
				ExtracorporealBloodCircuit.FromPatientArtery);
			ExtracorporealBloodCircuit.AddFlows(bloodFlowCombinator);
			bloodFlowCombinator.ConnectOutWithIn(ExtracorporealBloodCircuit.ToPatientVein,
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
