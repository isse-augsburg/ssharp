using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.Model
{
	using SafetySharp.Modeling;
	using Utilities.BidirectionalFlow;

	public class ArterialBloodPump : Component
	{
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Range(0, 8, OverflowBehavior.Error)]
		public int SpeedOfMotor = 0;

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
		}

		[Provided]
		public virtual void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CustomSuctionValue = SpeedOfMotor; //Force suction set by motor
			outgoingSuction.SuctionType=SuctionType.CustomSuction;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}

		public readonly Fault BloodPumpDefect = new TransientFault();

		[FaultEffect(Fault = nameof(BloodPumpDefect))]
		public class BloodPumpDefectEffect : ArterialBloodPump
		{
			[Provided]
			public override void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
			{
				outgoingSuction.CustomSuctionValue = 0; //Force suction set by motor
				outgoingSuction.SuctionType = SuctionType.CustomSuction;
			}
		}
	}

	public class ArteriaPressureTransducer : Component
	{
		public readonly BloodFlowSink SenseFlow = new BloodFlowSink();
		
		public QualitativePressure SensedPressure = QualitativePressure.NoPressure;

		[Provided]
		public void SetSenseFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.CustomSuction;
		}

		[Provided]
		public void ReceivedBlood(Blood incomingElement)
		{
			SensedPressure = incomingElement.Pressure;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(SenseFlow.SetOutgoingBackward), nameof(SetSenseFlowSuction));
			Bind(nameof(SenseFlow.ForwardFromPredecessorWasUpdated), nameof(ReceivedBlood));
		}
	}

	public class HeparinPump : Component
	{
		public readonly BloodFlowSource HeparinFlow = new BloodFlowSource();

		[Provided]
		public void SetHeparinFlow(Blood outgoing)
		{
			outgoing.HasHeparin = true;
			outgoing.Water = 0;
			outgoing.SmallWasteProducts = 0;
			outgoing.BigWasteProducts = 0;
			outgoing.ChemicalCompositionOk = true;
			outgoing.GasFree = true;
			outgoing.Pressure = QualitativePressure.NoPressure;
			outgoing.Temperature = QualitativeTemperature.BodyHeat;
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

	public class ArtierialChamber : Component
	{
		// Drip Chamber
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
			outgoing.GasFree = true;
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

	public class VenousChamber : Component
	{
		// Drip Chamber
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
			outgoing.GasFree = true;
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

	public class VenousPressureTransducer : Component
	{
		public readonly BloodFlowSink SenseFlow = new BloodFlowSink();

		public QualitativePressure SensedPressure = QualitativePressure.NoPressure;

		[Provided]
		public void SetSenseFlowSuction(Suction outgoingSuction)
		{
			outgoingSuction.CustomSuctionValue = 0;
			outgoingSuction.SuctionType = SuctionType.CustomSuction;
		}

		[Provided]
		public void ReceivedBlood(Blood incomingElement)
		{
			SensedPressure = incomingElement.Pressure;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(SenseFlow.SetOutgoingBackward), nameof(SetSenseFlowSuction));
			Bind(nameof(SenseFlow.ForwardFromPredecessorWasUpdated), nameof(ReceivedBlood));
		}
	}

	public class VenousSafetyDetector : Component
	{
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		public bool DetectedGasOrContaminatedBlood = false;

		[Provided]
		public virtual void SetMainFlow(Blood outgoing, Blood incoming)
		{
			outgoing.CopyValuesFrom(incoming);
			if (incoming.GasFree == false || incoming.ChemicalCompositionOk != true)
			{
				DetectedGasOrContaminatedBlood = true;
			}
			else
			{
				DetectedGasOrContaminatedBlood = false;
			}
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

		public readonly Fault SafetyDetectorDefect = new TransientFault();

		[FaultEffect(Fault = nameof(SafetyDetectorDefect))]
		public class SafetyDetectorDefectEffect : VenousSafetyDetector
		{
			[Provided]
			public override void SetMainFlow(Blood outgoing, Blood incoming)
			{
				outgoing.CopyValuesFrom(incoming);
				DetectedGasOrContaminatedBlood = false;
			}
		}
	}

	public class VenousTubingValve : Component
	{
		// HACK: To be able to react in time we delay the BloodFlow
		public readonly BloodFlowInToOutSegment MainFlow = new BloodFlowInToOutSegment();

		public ValveState ValveState = ValveState.Open;

		public readonly Blood DelayedBlood = new Blood();

		[Provided]
		public void SetMainFlow(Blood outgoing, Blood incoming)
		{
			if (ValveState == ValveState.Open)
			{
				outgoing.CopyValuesFrom(DelayedBlood);
			}
			else
			{
				outgoing.HasHeparin = true;
				outgoing.Water = 0;
				outgoing.SmallWasteProducts = 0;
				outgoing.BigWasteProducts = 0;
				outgoing.ChemicalCompositionOk = true;
				outgoing.GasFree = true;
				outgoing.Pressure = QualitativePressure.NoPressure;
				outgoing.Temperature = QualitativeTemperature.BodyHeat;
			}
			DelayedBlood.CopyValuesFrom(incoming);
		}

		[Provided]
		public void SetMainFlowSuction(Suction outgoingSuction, Suction incomingSuction)
		{
			outgoingSuction.CopyValuesFrom(incomingSuction);
		}

		public virtual void CloseValve()
		{
			ValveState = ValveState.Closed;
		}

		protected override void CreateBindings()
		{
			Bind(nameof(MainFlow.SetOutgoingBackward), nameof(SetMainFlowSuction));
			Bind(nameof(MainFlow.SetOutgoingForward), nameof(SetMainFlow));
		}

		public readonly Fault ValveDoesNotClose = new TransientFault();

		[FaultEffect(Fault = nameof(ValveDoesNotClose))]
		public class ValveDoesNotCloseEffect : VenousTubingValve
		{
			public override void CloseValve()
			{
			}
		}
	}

	public class ExtracorporealBloodCircuit : Component
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
