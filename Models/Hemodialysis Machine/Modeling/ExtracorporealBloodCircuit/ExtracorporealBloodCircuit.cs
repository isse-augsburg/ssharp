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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling.ExtracorporealBloodCircuit
{
	using SafetySharp.Modeling;
	using Utilities.BidirectionalFlow;

	public class ExtracorporealBloodCircuit : Component
	{
		public readonly BloodFlowDelegate FromPatientArtery = new BloodFlowDelegate();
		public readonly BloodFlowDelegate ToPatientVein = new BloodFlowDelegate();

		public readonly BloodFlowDelegate FromDialyzer = new BloodFlowDelegate();
		public readonly BloodFlowDelegate ToDialyzer = new BloodFlowDelegate();

		public readonly BloodPump ArterialBloodPump = new BloodPump();
		public readonly PressureTransducer ArteriaPressureTransducer = new PressureTransducer();
		public readonly HeparinPump HeparinPump = new HeparinPump();
		public readonly DripChamber ArterialChamber  = new DripChamber();
		public readonly DripChamber VenousChamber = new DripChamber();
		public readonly PressureTransducer VenousPressureTransducer = new PressureTransducer();
		public readonly VenousSafetyDetector VenousSafetyDetector = new VenousSafetyDetector();
		public readonly VenousTubingValve VenousTubingValve = new VenousTubingValve();

		public void AddFlows(BloodFlowCombinator flowCombinator)
		{
			// The order of the connections matter
			flowCombinator.ConnectOutWithIns(FromPatientArtery,
				new IFlowComponentUniqueIncoming<Blood, Suction>[] { ArterialBloodPump.MainFlow, ArteriaPressureTransducer.SenseFlow });
			flowCombinator.ConnectOutsWithIn(new IFlowComponentUniqueOutgoing<Blood, Suction>[] { ArterialBloodPump.MainFlow, HeparinPump.HeparinFlow },
				ArterialChamber.MainFlow);
			flowCombinator.ConnectOutWithIn(ArterialChamber.MainFlow,
				ToDialyzer);
			flowCombinator.ConnectOutWithIns(FromDialyzer,
				new IFlowComponentUniqueIncoming<Blood, Suction>[] { VenousChamber.MainFlow, VenousPressureTransducer.SenseFlow });
			flowCombinator.ConnectOutWithIn(VenousChamber.MainFlow,
				VenousSafetyDetector.MainFlow);
			flowCombinator.ConnectOutWithIn(VenousSafetyDetector.MainFlow,
				VenousTubingValve.MainFlow);
			flowCombinator.ConnectOutWithIn(VenousTubingValve.MainFlow,
				ToPatientVein);
		}

	}
}
