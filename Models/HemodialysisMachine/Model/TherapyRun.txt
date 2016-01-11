using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HemodialysisMachine.TherapyRun
{

	// Common Cause Failures
	// - power loss
	// - handling error
	// Other faults
	// - fault of component
	// - unexpected pressure from patient
	// - loss of connection to patient
	// - dialyzing fluid mixed-up
	// Hazards
	// - Air in Patient
	// - Too cold/warm blood returned
	// - Rupture of dialysator membrane
	// - Acidose
	// - Non filtration of metabolic waste products
	// - Blood in dialysate


	/*
	// Here we list all phases of the therapy (see headings in paper [] section 3.2)
	// 1. Preparation
	// 1.1 Automated self test
	// 1.2 Connecting the concentrate
	// 1.3 Setting the rinsing parameters
	// 1.4 Inserting, rinsing and testing the tubing system
	// 1.5 Preparing the heparin pump
	// 1.6 Setting the treatment parameters
	// 1.7 Rinsing the dialyzer
	//
	// 2. Initiation
	// 2.1 Connecting the patient and starting therapy
	// 2.2 During therapy
	//     a) Monitor blood-side pressure limits
	//     b) Treatment at minimum UF rate
	//     c) Heparin bolus (one time injection of a fixed amount of heparin)
	//     d) Arterial bolus (one time injection of a fixed amount of saline)
	//     e) Interrupting dialysis
	//     f) Completion of treatment
	//
	// 3. Ending
	// 3.1 Reinfusion
	// 3.2 Emptying the dialyzer
	// 3.3 Emptying the cartridge after dialyzer drain
	// 3.4 Overview of the therapy carried out
	//
	*/
	// We only implement here the main phase 2.2 because it covers 26 of 36 requirements

	enum CurrentPhase
	{
		PreparationSelfTest,
		PreparationConnectingConcentrate,
		PreparationSettingRinsingParameters,
		PreparationPreparingTubingSystem,
		PreparationPreparingHeparinPump,
		PreparationSettingTreatmentParameters,
		PreparationRinsingDialyzer,
		InitiationConnectingPatient,
		InitiationMainTherapy,
		EndingReinfusion,
		EndingEmptyingDialyzer,
		EndingEmptyingCatridge,
		EndingSummaryOfTherapy
	}

	/*class TherapyRun
	{
	}
	*/
}
