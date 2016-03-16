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

namespace Visualization
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media.Animation;
	using global::Funkfahrbetrieb.CrossingController;
	using global::Funkfahrbetrieb.TrainController;
	using global::HemodialysisMachine;
	using HemodialysisMachine.Model;
	using HemodialysisMachine.Utilities.BidirectionalFlow;
	using Infrastructure;
	using SafetySharp.Runtime;

	public partial class HdMachine
	{
		public enum LastVisualState
		{
			Unset,
			Enabled,
			Disabled
		}

		public HdMachine()
		{
			var specification = new Specification();

			InitializeComponent();
			_animationBloodPumpEnabled = (Storyboard)Resources["BloodPumpEnabled"];
			_animationHeparinPumpEnabled = (Storyboard)Resources["HeparinPumpEnabled"];
			_animationEnableWaterPreparation = (Storyboard)Resources["EnableWaterPreparation"];
			_animationDisableWaterPreparation = (Storyboard)Resources["DisableWaterPreparation"];
			_animationDialyzingFluidPreparationEnabled = (Storyboard)Resources["DialyzingFluidPreparationEnabled"];
			_animationUltraFiltrationEnabled = (Storyboard)Resources["UltraFiltrationEnabled"];
			_animationPumpToBalanceChamberEnabled = (Storyboard)Resources["PumpToBalanceChamberEnabled"];
			_animationCloseVenousTubingValve = (Storyboard)Resources["CloseVenousTubingValve"];
			_animationOpenVenousTubingValve = (Storyboard)Resources["OpenVenousTubingValve"];
			_animationArterialChamberDripping = (Storyboard)Resources["ArterialChamberDripping"];
			_animationVenousChamberDripping = (Storyboard)Resources["VenousChamberDripping"];
			_animationArterialChamberNotDripping = (Storyboard)Resources["ArterialChamberNotDripping"];
			_animationVenousChamberNotDripping = (Storyboard)Resources["VenousChamberNotDripping"];
			_animationFlowPatientToBloodPump = (Storyboard)Resources["FlowPatientToBloodPump"];
			_animationFlowBloodPumpToMerge1 = (Storyboard)Resources["FlowBloodPumpToMerge1"];
			_animationFlowMerge1ToArterialChamber = (Storyboard)Resources["FlowMerge1ToArterialChamber"];
			_animationFlowArterialChamberToDialyzer = (Storyboard)Resources["FlowArterialChamberToDialyzer"];
			_animationFlowDialyzerToSplit1 = (Storyboard)Resources["FlowDialyzerToSplit1"];
			_animationFlowSplit1ToVenousChamber = (Storyboard)Resources["FlowSplit1ToVenousChamber"];
			_animationFlowVenousChamberToSafetyDetector = (Storyboard)Resources["FlowVenousChamberToSafetyDetector"];
			_animationFlowSafetySensorToVenousValve = (Storyboard)Resources["FlowSafetySensorToVenousValve"];
			_animationFlowVenousValveToPatient = (Storyboard)Resources["FlowVenousValveToPatient"];
			_animationFlowWaterSupplyToWaterPreparation = (Storyboard)Resources["FlowWaterSupplyToWaterPreparation"];
			_animationFlowWaterPreparationToDialyzingFluidPreparation = (Storyboard)Resources["FlowWaterPreparationToDialyzingFluidPreparation"];
			_animationFlowDialyzingFluidPreparationToBalanceChamber = (Storyboard)Resources["FlowDialyzingFluidPreparationToBalanceChamber"];
			_animationFlowBalanceChamberToDrain = (Storyboard)Resources["FlowBalanceChamberToDrain"];
			_animationFlowBalanceChamberToSafetyBypass = (Storyboard)Resources["FlowBalanceChamberToSafetyBypass"];
			_animationFlowSafetyBypassToDialyzer = (Storyboard)Resources["FlowSafetyBypassToDialyzer"];
			_animationFlowDialyzerToSplit2 = (Storyboard)Resources["FlowDialyzerToSplit2"];
			_animationFlowSplit2ToPumpToBalanceChamber = (Storyboard)Resources["FlowSplit2ToPumpToBalanceChamber"];
			_animationFlowSplit2ToUltraFiltrationPump = (Storyboard)Resources["FlowSplit2ToUltraFiltrationPump"];
			_animationFlowPumpToBalanceChamberToBalanceChamber = (Storyboard)Resources["FlowPumpToBalanceChamberToBalanceChamber"];
			_animationFlowUltraFiltrationPumpToDrain = (Storyboard)Resources["FlowUltraFiltrationPumpToDrain"];


			// Initialize the simulation environment
			SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.Reset += (o, e) => OnModelStateReset();
			SimulationControls.Rewound += (o, e) => OnRewound();
			SimulationControls.SetSpecification(specification);

			// Initialize the visualization state
			UpdateModelState();
			
			SimulationControls.MaxSpeed = 64;
			SimulationControls.ChangeSpeed(8);
		}

		private HemodialysisMachine.Model.HdMachine Machine => SimulationControls.Model.RootComponents.OfType<HemodialysisMachine.Model.HdMachine>().Single();
		private HemodialysisMachine.Model.Patient Patient => SimulationControls.Model.RootComponents.OfType<HemodialysisMachine.Model.Patient>().First();

		private readonly Storyboard _animationBloodPumpEnabled;
		private readonly Storyboard _animationHeparinPumpEnabled;
		private readonly Storyboard _animationEnableWaterPreparation;
		private readonly Storyboard _animationDisableWaterPreparation;
		private readonly Storyboard _animationDialyzingFluidPreparationEnabled;
		private readonly Storyboard _animationUltraFiltrationEnabled;
		private readonly Storyboard _animationPumpToBalanceChamberEnabled;
		private readonly Storyboard _animationCloseVenousTubingValve;
		private readonly Storyboard _animationOpenVenousTubingValve;
		private readonly Storyboard _animationArterialChamberDripping;
		private readonly Storyboard _animationVenousChamberDripping;
		private readonly Storyboard _animationArterialChamberNotDripping;
		private readonly Storyboard _animationVenousChamberNotDripping;

		private readonly Storyboard _animationFlowPatientToBloodPump;
		private readonly Storyboard _animationFlowBloodPumpToMerge1;
		private readonly Storyboard _animationFlowMerge1ToArterialChamber;
		private readonly Storyboard _animationFlowArterialChamberToDialyzer;
		private readonly Storyboard _animationFlowDialyzerToSplit1;
		private readonly Storyboard _animationFlowSplit1ToVenousChamber;
		private readonly Storyboard _animationFlowVenousChamberToSafetyDetector;
		private readonly Storyboard _animationFlowSafetySensorToVenousValve;
		private readonly Storyboard _animationFlowVenousValveToPatient;
		private readonly Storyboard _animationFlowWaterSupplyToWaterPreparation;
		private readonly Storyboard _animationFlowWaterPreparationToDialyzingFluidPreparation;
		private readonly Storyboard _animationFlowDialyzingFluidPreparationToBalanceChamber;
		private readonly Storyboard _animationFlowBalanceChamberToDrain;
		private readonly Storyboard _animationFlowBalanceChamberToSafetyBypass;
		private readonly Storyboard _animationFlowSafetyBypassToDialyzer;
		private readonly Storyboard _animationFlowDialyzerToSplit2;
		private readonly Storyboard _animationFlowSplit2ToPumpToBalanceChamber;
		private readonly Storyboard _animationFlowSplit2ToUltraFiltrationPump;
		private readonly Storyboard _animationFlowPumpToBalanceChamberToBalanceChamber;
		private readonly Storyboard _animationFlowUltraFiltrationPumpToDrain;

		private LastVisualState _visualStateBloodPump;
		private LastVisualState _visualStateHeparinPump;
		private LastVisualState _visualStateWaterPreparation;
		private LastVisualState _visualStateDialyzingFluidPreparation;
		private LastVisualState _visualStateUltraFiltration;
		private LastVisualState _visualStatePumpToBalanceChamber;
		private LastVisualState _visualStateVenousTubingValve;
		private LastVisualState _visualStateArterialChamber;
		private LastVisualState _visualStateVenousChamber;

		private LastVisualState _visualStateFlowPatientToBloodPump;
		private LastVisualState _visualStateFlowBloodPumpToMerge1;
		private LastVisualState _visualStateFlowMerge1ToArterialChamber;
		private LastVisualState _visualStateFlowArterialChamberToDialyzer;
		private LastVisualState _visualStateFlowDialyzerToSplit1;
		private LastVisualState _visualStateFlowSplit1ToVenousChamber;
		private LastVisualState _visualStateFlowVenousChamberToSafetyDetector;
		private LastVisualState _visualStateFlowSafetySensorToVenousValve;
		private LastVisualState _visualStateFlowVenousValveToPatient;
		private LastVisualState _visualStateFlowWaterSupplyToWaterPreparation;
		private LastVisualState _visualStateFlowWaterPreparationToDialyzingFluidPreparation;
		private LastVisualState _visualStateFlowDialyzingFluidPreparationToBalanceChamber;
		private LastVisualState _visualStateFlowBalanceChamberToDrain;
		private LastVisualState _visualStateFlowBalanceChamberToSafetyBypass;
		private LastVisualState _visualStateFlowSafetyBypassToDialyzer;
		private LastVisualState _visualStateFlowDialyzerToSplit2;
		private LastVisualState _visualStateFlowSplit2ToPumpToBalanceChamber;
		private LastVisualState _visualStateFlowSplit2ToUltraFiltrationPump;
		private LastVisualState _visualStateFlowPumpToBalanceChamberToBalanceChamber;
		private LastVisualState _visualStateFlowUltraFiltrationPumpToDrain;


		private void OnModelStateReset()
		{
			OnRewound();

			if (SimulationControls.Simulator.IsReplay)
				return;
		}

		private void OnRewound()
		{
		}

		private void UpdateModelState()
		{
			// ArterialBloodPump
			if (Machine.ExtracorporealBloodCircuit.ArterialBloodPump.SpeedOfMotor > 0 && _visualStateBloodPump!=LastVisualState.Enabled)
			{
				_visualStateBloodPump = LastVisualState.Enabled;
				_animationBloodPumpEnabled.RepeatBehavior = RepeatBehavior.Forever;
				_animationBloodPumpEnabled.Begin();
			}
			else if (Machine.ExtracorporealBloodCircuit.ArterialBloodPump.SpeedOfMotor == 0 && _visualStateBloodPump != LastVisualState.Disabled)
			{
				_visualStateBloodPump = LastVisualState.Disabled;
				_animationBloodPumpEnabled.Stop();
			}

			// HeparinPump
			if (Machine.ExtracorporealBloodCircuit.HeparinPump.Enabled && _visualStateHeparinPump != LastVisualState.Enabled)
			{
				_visualStateHeparinPump = LastVisualState.Enabled;
				_animationHeparinPumpEnabled.RepeatBehavior = RepeatBehavior.Forever;
				_animationHeparinPumpEnabled.Begin();
			}
			else if (Machine.ExtracorporealBloodCircuit.HeparinPump.Enabled==false && _visualStateHeparinPump != LastVisualState.Disabled)
			{
				_visualStateHeparinPump = LastVisualState.Disabled;
				_animationHeparinPumpEnabled.Stop();
			}

			// WaterPreparation
			if (Machine.DialyzingFluidDeliverySystem.DialyzingFluidWaterPreparation.WaterHeaterEnabled() && _visualStateWaterPreparation != LastVisualState.Enabled)
			{
				_visualStateWaterPreparation = LastVisualState.Enabled;
				//_animationEnableWaterPreparation.RepeatBehavior = RepeatBehavior.Forever;
				_animationDisableWaterPreparation.Stop();
				_animationEnableWaterPreparation.Begin();
			}
			else if (Machine.DialyzingFluidDeliverySystem.DialyzingFluidWaterPreparation.WaterHeaterEnabled()==false && _visualStateWaterPreparation != LastVisualState.Disabled)
			{
				_visualStateWaterPreparation = LastVisualState.Disabled;
				_animationEnableWaterPreparation.Stop();
				_animationDisableWaterPreparation.Begin();
			}

			// DialyzingFluidPreparation
			if (Machine.DialyzingFluidDeliverySystem.DialyzingFluidPreparation.PumpSpeed > 0 && _visualStateDialyzingFluidPreparation != LastVisualState.Enabled)
			{
				_visualStateDialyzingFluidPreparation = LastVisualState.Enabled;
				_animationDialyzingFluidPreparationEnabled.RepeatBehavior = RepeatBehavior.Forever;
				_animationDialyzingFluidPreparationEnabled.Begin();
			}
			else if (Machine.DialyzingFluidDeliverySystem.DialyzingFluidPreparation.PumpSpeed == 0 && _visualStateDialyzingFluidPreparation != LastVisualState.Disabled)
			{
				_visualStateDialyzingFluidPreparation = LastVisualState.Disabled;
				_animationDialyzingFluidPreparationEnabled.Stop();
			}

			// UltraFiltration
			if (Machine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.UltraFiltrationPumpSpeed > 0 && _visualStateUltraFiltration != LastVisualState.Enabled)
			{
				_visualStateUltraFiltration = LastVisualState.Enabled;
				_animationUltraFiltrationEnabled.RepeatBehavior = RepeatBehavior.Forever;
				_animationUltraFiltrationEnabled.Begin();
			}
			else if (Machine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.UltraFiltrationPumpSpeed == 0 && _visualStateUltraFiltration != LastVisualState.Disabled)
			{
				_visualStateUltraFiltration = LastVisualState.Disabled;
				_animationUltraFiltrationEnabled.Stop();
			}

			// PumpToBalanceChamber
			if (Machine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpSpeed > 0 && _visualStatePumpToBalanceChamber != LastVisualState.Enabled)
			{
				_visualStatePumpToBalanceChamber = LastVisualState.Enabled;
				_animationPumpToBalanceChamberEnabled.RepeatBehavior = RepeatBehavior.Forever;
				_animationPumpToBalanceChamberEnabled.Begin();
			}
			else if (Machine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpSpeed == 0 && _visualStatePumpToBalanceChamber != LastVisualState.Disabled)
			{
				_visualStatePumpToBalanceChamber = LastVisualState.Disabled;
				_animationPumpToBalanceChamberEnabled.Stop();
			}

			// VenousTubingValve
			if (Machine.ExtracorporealBloodCircuit.VenousTubingValve.ValveState == ValveState.Open && _visualStateVenousTubingValve != LastVisualState.Enabled)
			{
				_visualStateVenousTubingValve = LastVisualState.Enabled;
				_animationCloseVenousTubingValve.Stop();
				_animationOpenVenousTubingValve.Begin();
			}
			else if (Machine.ExtracorporealBloodCircuit.VenousTubingValve.ValveState == ValveState.Closed && _visualStateVenousTubingValve != LastVisualState.Disabled)
			{
				_visualStateVenousTubingValve = LastVisualState.Disabled;
				_animationOpenVenousTubingValve.Stop();
				_animationCloseVenousTubingValve.Begin();
			}

			// ArterialChamber
			if (Machine.ExtracorporealBloodCircuit.ArterialBloodPump.SpeedOfMotor > 0 && _visualStateArterialChamber != LastVisualState.Enabled)
			{
				_visualStateArterialChamber = LastVisualState.Enabled;
				_animationArterialChamberDripping.RepeatBehavior = RepeatBehavior.Forever;
				_animationArterialChamberNotDripping.Stop();
				_animationArterialChamberDripping.Begin();
			}
			else if (Machine.ExtracorporealBloodCircuit.ArterialBloodPump.SpeedOfMotor == 0 && _visualStateArterialChamber != LastVisualState.Disabled)
			{
				_visualStateArterialChamber = LastVisualState.Disabled;
				_animationArterialChamberDripping.Stop();
				_animationArterialChamberNotDripping.Begin();
			}

			// VenousChamber
			if (Machine.ExtracorporealBloodCircuit.ArterialBloodPump.SpeedOfMotor > 0 && _visualStateVenousChamber != LastVisualState.Enabled)
			{
				_visualStateVenousChamber = LastVisualState.Enabled;
				_animationVenousChamberDripping.RepeatBehavior = RepeatBehavior.Forever;
				_animationVenousChamberNotDripping.Stop();
				_animationVenousChamberDripping.Begin();
			}
			else if (Machine.ExtracorporealBloodCircuit.ArterialBloodPump.SpeedOfMotor == 0 && _visualStateVenousChamber != LastVisualState.Disabled)
			{
				_visualStateVenousChamber = LastVisualState.Disabled;
				_animationVenousChamberDripping.Stop();
				_animationVenousChamberNotDripping.Begin();
			}

			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowPatientToBloodPump, ref _visualStateFlowPatientToBloodPump);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowBloodPumpToMerge1, ref _visualStateFlowBloodPumpToMerge1);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowMerge1ToArterialChamber, ref _visualStateFlowMerge1ToArterialChamber);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowArterialChamberToDialyzer, ref _visualStateFlowArterialChamberToDialyzer);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowDialyzerToSplit1, ref _visualStateFlowDialyzerToSplit1);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowSplit1ToVenousChamber, ref _visualStateFlowSplit1ToVenousChamber);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowVenousChamberToSafetyDetector, ref _visualStateFlowVenousChamberToSafetyDetector);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowSafetySensorToVenousValve, ref _visualStateFlowSafetySensorToVenousValve);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowVenousValveToPatient, ref _visualStateFlowVenousValveToPatient);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowWaterSupplyToWaterPreparation, ref _visualStateFlowWaterSupplyToWaterPreparation);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowWaterPreparationToDialyzingFluidPreparation, ref _visualStateFlowWaterPreparationToDialyzingFluidPreparation);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowDialyzingFluidPreparationToBalanceChamber, ref _visualStateFlowDialyzingFluidPreparationToBalanceChamber);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowBalanceChamberToDrain, ref _visualStateFlowBalanceChamberToDrain);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowBalanceChamberToSafetyBypass, ref _visualStateFlowBalanceChamberToSafetyBypass);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowSafetyBypassToDialyzer, ref _visualStateFlowSafetyBypassToDialyzer);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowDialyzerToSplit2, ref _visualStateFlowDialyzerToSplit2);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowSplit2ToPumpToBalanceChamber, ref _visualStateFlowSplit2ToPumpToBalanceChamber);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowSplit2ToUltraFiltrationPump, ref _visualStateFlowSplit2ToUltraFiltrationPump);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowPumpToBalanceChamberToBalanceChamber, ref _visualStateFlowPumpToBalanceChamberToBalanceChamber);
			UpdateModelStateFlowElement(Patient.ArteryFlow.Outgoing, _animationFlowUltraFiltrationPumpToDrain, ref _visualStateFlowUltraFiltrationPumpToDrain);
		}

		private void UpdateModelStateFlowElement(PortFlowOut<Blood, Suction> portFlowOut, Storyboard storyboard, ref LastVisualState visualState)
		{
			UpdateModelStateFlowElement(portFlowOut.ForwardToSuccessor.HasWaterOrBigWaste(),storyboard,ref visualState);
		}

		private void UpdateModelStateFlowElement(bool shouldBeEnabled, Storyboard storyboard, ref LastVisualState visualState)
		{
			if (shouldBeEnabled && visualState != LastVisualState.Enabled)
			{
				visualState = LastVisualState.Enabled;
				storyboard.RepeatBehavior = RepeatBehavior.Forever;
				storyboard.Begin();
			}
			else if (shouldBeEnabled == false && visualState != LastVisualState.Disabled)
			{
				visualState = LastVisualState.Disabled;
				storyboard.Stop();
			}

		}

		private void BloodPumpDefect_Click(object sender, RoutedEventArgs e)
		{
			Machine.ExtracorporealBloodCircuit.ArterialBloodPump.BloodPumpDefect.ToggleActivationMode();
		}

		private void ModelValues_Click(object sender, RoutedEventArgs e)
		{
			Console.Out.WriteLine("Time Steps Left: " + Machine.ControlSystem.TimeStepsLeft);
			Console.Out.WriteLine("Bloodpump speed: " +  Machine.ExtracorporealBloodCircuit.ArterialBloodPump.SpeedOfMotor);
			Patient.PrintBloodValues("visualization");
			Patient.ArteryFlow.Outgoing.ForwardToSuccessor.PrintBloodValues("outgoing Blood");
			Patient.VeinFlow.Incoming.ForwardFromPredecessor.PrintBloodValues("incoming Blood");
			Patient.ArteryFlow.Outgoing.BackwardFromSuccessor.PrintSuctionValues("suction of artery (at patient)");
			Machine.ToPatientVein.Incoming.BackwardToPredecessor.PrintSuctionValues("suction of artery (at entrance of machine)");
			Machine.ExtracorporealBloodCircuit.ArterialBloodPump.MainFlow.Incoming.BackwardToPredecessor.PrintSuctionValues("suction of bloodpump");
			Machine.ExtracorporealBloodCircuit.BloodFlow.Incoming.BackwardToPredecessor.PrintSuctionValues("suction of ECB");
			Machine.Dialyzer.DialyzingFluidFlow.Incoming.BackwardToPredecessor.PrintSuctionValues("Dialyzing Fluid suction of Dialyzer to predecessor");
			Machine.Dialyzer.DialyzingFluidFlow.Outgoing.BackwardFromSuccessor.PrintSuctionValues("Dialyzing Fluid suction of Dialyzer from successor");
			Machine.Dialyzer.DialyzingFluidFlow.Incoming.ForwardFromPredecessor.PrintDialyzingFluidValues("Dialyzing Fluid entering Dialyzer");
		}
	}
}