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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model.ExtracorporealBloodCircuit
{
	using Modeling;
	using Utilities.BidirectionalFlow;

	public class ExtracorporealBloodCircuit : Component
	{
		public readonly BloodFlowComposite BloodFlow = new BloodFlowComposite();
		public readonly BloodFlowDelegate FromDialyzer = new BloodFlowDelegate();
		public readonly BloodFlowDelegate ToDialyzer = new BloodFlowDelegate();

		public readonly ArterialBloodPump ArterialBloodPump = new ArterialBloodPump();
		public readonly ArteriaPressureTransducer ArteriaPressureTransducer = new ArteriaPressureTransducer();
		public readonly HeparinPump HeparinPump = new HeparinPump();
		public readonly ArtierialChamber ArterialChamber  = new ArtierialChamber();
		public readonly VenousChamber VenousChamber = new VenousChamber();
		public readonly VenousPressureTransducer VenousPressureTransducer = new VenousPressureTransducer();
		public readonly VenousSafetyDetector VenousSafetyDetector = new VenousSafetyDetector();
		public readonly VenousTubingValve VenousTubingValve = new VenousTubingValve();

		public void AddFlows(BloodFlowCombinator flowCombinator)
		{
			// The order of the connections matter
			flowCombinator.ConnectInWithIns(BloodFlow,
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
			flowCombinator.ConnectOutWithOut(VenousTubingValve.MainFlow,
				BloodFlow);
		}

	}
}
