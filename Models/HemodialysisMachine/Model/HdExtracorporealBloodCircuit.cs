using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using Utilities;

	class ArterialBloodPump
	{
		public readonly BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment(In => In);
	}

	class ArteriaPressureTransducer
	{
		public readonly BloodFlowSink BloodFlow = new BloodFlowSink();
	}

	class HeparinPump
	{
		public readonly BloodFlowSource BloodFlow = new BloodFlowSource((value) => { });
	}

	class ArtierialChamber
	{
		public readonly BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment(In => In);
	}

	class VenousChamber
	{
		public readonly BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment(In => In);
	}

	class VenousPressureTransducer
	{
		public readonly BloodFlowSink BloodFlow = new BloodFlowSink();
	}

	class VenousSafetyDetector
	{
		public readonly BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment(In => In);
	}

	class VenousTubingValve
	{
		public readonly BloodFlowInToOutSegment BloodFlow = new BloodFlowInToOutSegment(In => In);
	}

	class ExtracorporealBloodCircuit
	{
		public readonly BloodFlowComposite BloodFlow = new BloodFlowComposite();
		public readonly BloodFlowUniqueOutgoingStub FromDialyzer = new BloodFlowUniqueOutgoingStub();
		public readonly BloodFlowUniqueIncomingStub ToDialyzer = new BloodFlowUniqueIncomingStub();

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
			flowCombinator.Connect(BloodFlow.InternalSource.Outgoing,
				new PortFlowIn<Blood>[] { ArterialBloodPump.BloodFlow.Incoming, ArteriaPressureTransducer.BloodFlow.Incoming });
			flowCombinator.Connect(new PortFlowOut<Blood>[] { ArterialBloodPump.BloodFlow.Outgoing, HeparinPump.BloodFlow.Outgoing },
				ArterialChamber.BloodFlow.Incoming);
			flowCombinator.Connect(ArterialChamber.BloodFlow.Outgoing,
				ToDialyzer.Incoming);
			flowCombinator.Connect(FromDialyzer.Outgoing,
				new PortFlowIn<Blood>[] { VenousChamber.BloodFlow.Incoming, VenousPressureTransducer.BloodFlow.Incoming });
			flowCombinator.Connect(VenousChamber.BloodFlow.Outgoing,
				VenousSafetyDetector.BloodFlow.Incoming);
			flowCombinator.Connect(VenousSafetyDetector.BloodFlow.Outgoing,
				VenousTubingValve.BloodFlow.Incoming);
			flowCombinator.Connect(VenousTubingValve.BloodFlow.Outgoing,
				BloodFlow.InternalSink.Incoming);
		}

	}
}
