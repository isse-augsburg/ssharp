using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;
	using Utilities.BidirectionalFlow;

	class ArterialBloodPump : Component
	{
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	class ArteriaPressureTransducer : Component
	{
		public readonly BloodFlowSink SenseFlow = new BloodFlowSink();

		[Provided]
		public void SetSenseFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.CustomSuction;
		}

		[Provided]
		public void ReceivedBlood(Blood incomingElement)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(SenseFlow.SetOutgoingBackward), nameof(SetSenseFlowSuction));
			Bind(nameof(SenseFlow.ForwardFromPredecessorWasUpdated), nameof(ReceivedBlood));
		}
	}

	class HeparinPump : Component
	{
		public readonly BloodFlowSource HeparinFlow = new BloodFlowSource();

		[Provided]
		public void SetHeparinFlow(Blood outgoing)
		{
			outgoing.HasHeparin = true;
			outgoing.Water = 0;
		}

		[Provided]
		public void ReceivedSuction(Suction incomingSuction)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(HeparinFlow.SetOutgoingForward), nameof(SetHeparinFlow));
			Bind(nameof(HeparinFlow.BackwardFromSuccessorWasUpdated), nameof(ReceivedSuction));
		}
	}

	class ArtierialChamber : Component
	{
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	class VenousChamber : Component
	{
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	class VenousPressureTransducer : Component
	{
		public readonly BloodFlowSink SenseFlow = new BloodFlowSink();

		[Provided]
		public void SetSenseFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.CustomSuction;
		}

		[Provided]
		public void ReceivedBlood(Blood incomingElement)
		{
		}

		protected override void CreateBindings()
		{
			Bind(nameof(SenseFlow.SetOutgoingBackward), nameof(SetSenseFlowSuction));
			Bind(nameof(SenseFlow.ForwardFromPredecessorWasUpdated), nameof(ReceivedBlood));
		}
	}

	class VenousSafetyDetector : Component
	{
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	class VenousTubingValve : Component
	{
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}
	}

	class ExtracorporealBloodCircuit : Component
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
				new PortFlowIn<Blood, Suction>[] { ArterialBloodPump.MainFlow.Incoming, ArteriaPressureTransducer.SenseFlow.Incoming });
			flowCombinator.Connect(new PortFlowOut<Blood, Suction>[] { ArterialBloodPump.MainFlow.Outgoing, HeparinPump.HeparinFlow.Outgoing },
				ArterialChamber.MainFlow.Incoming);
			flowCombinator.Connect(ArterialChamber.MainFlow.Outgoing,
				ToDialyzer.Incoming);
			flowCombinator.Connect(FromDialyzer.Outgoing,
				new PortFlowIn<Blood, Suction>[] { VenousChamber.MainFlow.Incoming, VenousPressureTransducer.SenseFlow.Incoming });
			flowCombinator.Connect(VenousChamber.MainFlow.Outgoing,
				VenousSafetyDetector.MainFlow.Incoming);
			flowCombinator.Connect(VenousSafetyDetector.MainFlow.Outgoing,
				VenousTubingValve.MainFlow.Incoming);
			flowCombinator.Connect(VenousTubingValve.MainFlow.Outgoing,
				BloodFlow.InternalSink.Incoming);
		}

	}
}
