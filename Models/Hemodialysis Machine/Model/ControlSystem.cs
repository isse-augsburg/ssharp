using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.CaseStudies.HemodialysisMachine.Model
{
	using System.Linq.Expressions;
	using Modeling;


	// Simplifications:
	// - Only main step of therapy modeled
	// - Tick Time: 1 Tick = 1 minute
	// - Therapy Time is fix 60 minutes
	// - We do not use pressure directly and make exact calculations based on physical equations
	//   (modelica is better suited for that). We give integer numbers, which describe the demand
	//   in milliliters in one minute.
	// - Interdependent Parameters: If parameters A and B can derive parameter C then C is not saved here.
	// - Remove parameters where more domain knowledge is needed to correctly model the behavior of the system.
	//   In these cases we use inside the code qualitative descriptions (e.g. blood pressure too high,
	//   trans membrane pressure too low).
	// - leave out parameters which are sensed and not set by the medical staff (ActualTmp).

	public enum InternalTherapyPhase // the phase seen by controller
	{
		//PreparationSelfTest,
		//PreparationConnectingConcentrate,
		//PreparationSettingRinsingParameters,
		//PreparationPreparingTubingSystem,
		//PreparationPreparingHeparinPump,
		//PreparationSettingTreatmentParameters,
		//PreparationRinsingDialyzer,
		//InitiationConnectingPatient,
		InitiationMainTherapy,
		EndOfThreatment
		//EndingReinfusion,
		//EndingEmptyingDialyzer,
		//EndingEmptyingCatridge,
		//EndingSummaryOfTherapy
	}


	class TherapyParameters
	{
		// TreatmentParameters
		public KindOfDialysate KindOfDialysateConcentrate { get; } = KindOfDialysate.Bicarbonate;
		public int DialysingFluidTemperature { get; } = 33; // in °C. Should be between 33°C and 40°C
		public int DialysingFluidFlowRate { get; } = 0;
		
		// UltraFiltrationParameters
		public int UltraFiltrationRate { get; } = 0; // 0 - 500mL/h

		// PressureParameters
		public bool LimitsTransMembranePressure { get; } = true; //true=On, false=OFF

		// HeparinParameters
		public bool UseHeparinInThreatment { get; } = true;// true=enabled, false=disabled
	}

	public class ControlSystem : Component
	{

		//[Hidden]
		//public Timer Timer;

		public int TimeStepsLeft = 7; // hard code 7 time steps

		//Subcomponents
		private readonly DialyzingFluidWaterPreparation DialyzingFluidWaterPreparation;
		private readonly DialyzingFluidPreparation DialyzingFluidPreparation;
		private readonly DialyzingUltraFiltrationPump DialyzingUltraFiltrationPump;
		private readonly DialyzingFluidSafetyBypass DialyzingFluidSafetyBypass;
		private readonly PumpToBalanceChamber PumpToBalanceChamber;
		private readonly HeparinPump HeparinPump;
		private readonly ArterialBloodPump ArterialBloodPump;
		private readonly VenousPressureTransducer VenousPressureTransducer;
		private readonly VenousSafetyDetector VenousSafetyDetector;
		private readonly VenousTubingValve VenousTubingValve;


		[Hidden]
		public readonly StateMachine<InternalTherapyPhase> CurrentTherapyPhase = new StateMachine<InternalTherapyPhase>(InternalTherapyPhase.InitiationMainTherapy);

		private Dialyzer Dialyzer;

		public ControlSystem(Dialyzer dialyzer, ExtracorporealBloodCircuit extracorporealBloodCircuit, DialyzingFluidDeliverySystem dialyzingFluidDeliverySystem)
		{
			Dialyzer = dialyzer;
			DialyzingFluidWaterPreparation = dialyzingFluidDeliverySystem.DialyzingFluidWaterPreparation;
			DialyzingFluidPreparation = dialyzingFluidDeliverySystem.DialyzingFluidPreparation;
			DialyzingUltraFiltrationPump = dialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump;
			DialyzingFluidSafetyBypass = dialyzingFluidDeliverySystem.DialyzingFluidSafetyBypass;
			PumpToBalanceChamber = dialyzingFluidDeliverySystem.PumpToBalanceChamber;
			HeparinPump = extracorporealBloodCircuit.HeparinPump;
			ArterialBloodPump = extracorporealBloodCircuit.ArterialBloodPump;
			VenousPressureTransducer = extracorporealBloodCircuit.VenousPressureTransducer;
			VenousSafetyDetector = extracorporealBloodCircuit.VenousSafetyDetector;
			VenousTubingValve = extracorporealBloodCircuit.VenousTubingValve;
		}

		public void StepOfMainTherapy()
		{
			TimeStepsLeft = (TimeStepsLeft > 0) ? (TimeStepsLeft - 1) : 0;
			ArterialBloodPump.SpeedOfMotor = 4;
			DialyzingUltraFiltrationPump.UltraFiltrationPumpSpeed = 1;
			PumpToBalanceChamber.PumpSpeed = 4;
			DialyzingFluidPreparation.PumpSpeed = 4;
		}

		public void ShutdownMotors()
		{
			VenousTubingValve.CloseValve();
			TimeStepsLeft = 0;
			ArterialBloodPump.SpeedOfMotor = 0;
			DialyzingUltraFiltrationPump.UltraFiltrationPumpSpeed = 0;
			PumpToBalanceChamber.PumpSpeed = 0;
			DialyzingFluidPreparation.PumpSpeed = 0;
		}

		public override void Update()
		{
			//Update(Sensor, Timer, Pump);
			CurrentTherapyPhase
				.Transition(
					from: InternalTherapyPhase.InitiationMainTherapy,
					to: InternalTherapyPhase.InitiationMainTherapy,
					guard: TimeStepsLeft>0 && !VenousSafetyDetector.DetectedGasOrContaminatedBlood,
					action: StepOfMainTherapy
				);
			CurrentTherapyPhase
				.Transition(
					from: InternalTherapyPhase.InitiationMainTherapy,
					to: InternalTherapyPhase.EndOfThreatment,
					guard: TimeStepsLeft <= 0 || VenousSafetyDetector.DetectedGasOrContaminatedBlood,
					action: ShutdownMotors
				);
		}
	}


}
