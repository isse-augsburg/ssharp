using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model
{
	using Modeling;
	using Utilities.BidirectionalFlow;

	public class ArterialBloodPump : Component
	{
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		[Range(0, 8, OverflowBehavior.Error)]
		public int SpeedOfMotor = 0;

		[Provided]
		public void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
		}

		[Provided]
		public virtual void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CustomSuctionValue = SpeedOfMotor; //Force suction set by motor
			toPredecessor.SuctionType=SuctionType.CustomSuction;
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}

		public readonly Fault BloodPumpDefect = new TransientFault();

		[FaultEffect(Fault = nameof(BloodPumpDefect))]
		public class BloodPumpDefectEffect : ArterialBloodPump
		{
			[Provided]
			public override void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
			{
				toPredecessor.CustomSuctionValue = 0; //Force suction set by motor
				toPredecessor.SuctionType = SuctionType.CustomSuction;
			}
		}
	}

	public class ArteriaPressureTransducer : Component
	{
		public readonly BloodFlowSink SenseFlow = new BloodFlowSink();
		
		public QualitativePressure SensedPressure = QualitativePressure.NoPressure;

		[Provided]
		public void SetSenseFlowSuction(Suction toPredecessor)
		{
			toPredecessor.CustomSuctionValue = 0;
			toPredecessor.SuctionType = SuctionType.CustomSuction;
		}

		[Provided]
		public void ReceivedBlood(Blood incomingElement)
		{
			SensedPressure = incomingElement.Pressure;
		}

		protected override void CreateBindings()
		{
			SenseFlow.SendBackward=SetSenseFlowSuction;
			SenseFlow.ReceivedForward=ReceivedBlood;
		}
	}

	public class HeparinPump : Component
	{
		public readonly BloodFlowSource HeparinFlow = new BloodFlowSource();

		public readonly bool Enabled = true;

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
		public void ReceivedSuction(Suction fromSuccessor)
		{
		}

		protected override void CreateBindings()
		{
			HeparinFlow.SendForward=SetHeparinFlow;
			HeparinFlow.ReceivedBackward=ReceivedSuction;
		}
	}

	public class ArtierialChamber : Component
	{
		// Drip Chamber
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		[Provided]
		public void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
			toSuccessor.GasFree = true;
		}

		[Provided]
		public void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}
	}

	public class VenousChamber : Component
	{
		// Drip Chamber
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		[Provided]
		public void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
			toSuccessor.GasFree = true;
		}

		[Provided]
		public void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}
	}

	public class VenousPressureTransducer : Component
	{
		public readonly BloodFlowSink SenseFlow = new BloodFlowSink();

		public QualitativePressure SensedPressure = QualitativePressure.NoPressure;

		[Provided]
		public void SetSenseFlowSuction(Suction toPredecessor)
		{
			toPredecessor.CustomSuctionValue = 0;
			toPredecessor.SuctionType = SuctionType.CustomSuction;
		}

		[Provided]
		public void ReceivedBlood(Blood incomingElement)
		{
			SensedPressure = incomingElement.Pressure;
		}

		protected override void CreateBindings()
		{
			SenseFlow.SendBackward=SetSenseFlowSuction;
			SenseFlow.ReceivedForward=ReceivedBlood;
		}
	}

	public class VenousSafetyDetector : Component
	{
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		public bool DetectedGasOrContaminatedBlood = false;

		[Provided]
		public virtual void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			toSuccessor.CopyValuesFrom(fromPredecessor);
			if (fromPredecessor.GasFree == false || fromPredecessor.ChemicalCompositionOk != true)
			{
				DetectedGasOrContaminatedBlood = true;
			}
			else
			{
				DetectedGasOrContaminatedBlood = false;
			}
		}

		[Provided]
		public void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
		}

		public readonly Fault SafetyDetectorDefect = new TransientFault();

		[FaultEffect(Fault = nameof(SafetyDetectorDefect))]
		public class SafetyDetectorDefectEffect : VenousSafetyDetector
		{
			[Provided]
			public override void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
			{
				toSuccessor.CopyValuesFrom(fromPredecessor);
				DetectedGasOrContaminatedBlood = false;
			}
		}
	}

	public class VenousTubingValve : Component
	{
		// HACK: To be able to react in time we delay the BloodFlow
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		public ValveState ValveState = ValveState.Open;

		public readonly BufferedBlood DelayedBlood = new BufferedBlood();

		[Provided]
		public void SetMainFlow(Blood toSuccessor, Blood fromPredecessor)
		{
			if (ValveState == ValveState.Open)
			{
				toSuccessor.CopyValuesFrom(DelayedBlood);
			}
			else
			{
				toSuccessor.HasHeparin = true;
				toSuccessor.Water = 0;
				toSuccessor.SmallWasteProducts = 0;
				toSuccessor.BigWasteProducts = 0;
				toSuccessor.ChemicalCompositionOk = true;
				toSuccessor.GasFree = true;
				toSuccessor.Pressure = QualitativePressure.NoPressure;
				toSuccessor.Temperature = QualitativeTemperature.BodyHeat;
			}
			DelayedBlood.CopyValuesFrom(fromPredecessor);
		}

		[Provided]
		public void SetMainFlowSuction(Suction fromSuccessor, Suction toPredecessor)
		{
			toPredecessor.CopyValuesFrom(fromSuccessor);
		}

		public virtual void CloseValve()
		{
			ValveState = ValveState.Closed;
		}

		protected override void CreateBindings()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
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
