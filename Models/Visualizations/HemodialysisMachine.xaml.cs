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

namespace SafetySharp.CaseStudies.Visualizations
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Media.Animation;
	using System.Windows.Shapes;
	using HemodialysisMachine.Model;
	using HemodialysisMachine.Utilities.BidirectionalFlow;
	using Infrastructure;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;

	public partial class HdMachine
	{
		public enum LastVisualState
		{
			Unset,
			Enabled,
			Disabled
		}

		public class VisualFlow
		{
			public string Name;
			public Storyboard Animation;
			public LastVisualState LastVisualState;
			public Rectangle SelectionRectangle;
			public Action UpdateVisualization;
			public Func<string> GetInfoText;

			public VisualFlow(string _name, Storyboard _animation, Rectangle _selectionRectangle, Func<FlowPort<Blood, Suction>> portFlowOut)
			{
				Name = _name;
				Animation = _animation;
				SelectionRectangle = _selectionRectangle;

				UpdateVisualization = () =>
				{
					GenericUpdateVisualization(portFlowOut().Forward.HasWaterOrBigWaste());
				};
				GetInfoText = () => portFlowOut().Forward.ValuesAsText();
			}

			public VisualFlow(string _name, Storyboard _animation, Rectangle _selectionRectangle, Func<FlowPort<DialyzingFluid, Suction>> portFlowOut)
			{
				Name = _name;
				Animation = _animation;
				SelectionRectangle = _selectionRectangle;

				UpdateVisualization = () =>
				{
					GenericUpdateVisualization(portFlowOut().Forward.Quantity > 0);
				};
				GetInfoText = () => portFlowOut().Forward.ValuesAsText();
			}

			private void GenericUpdateVisualization(bool shouldBeEnabled)
			{
				if (shouldBeEnabled && LastVisualState != LastVisualState.Enabled)
				{
					LastVisualState = LastVisualState.Enabled;
					Animation.RepeatBehavior = RepeatBehavior.Forever;
					Animation.Begin();
				}
				else if (shouldBeEnabled == false && LastVisualState != LastVisualState.Disabled)
				{
					LastVisualState = LastVisualState.Disabled;
					Animation.Stop();
				}
			}
		}


		public class VisualFault
		{
			public Func<Fault> Fault;
			public Rectangle FaultSymbol;

			public VisualFault(Func<Fault> _fault, Rectangle _faultSymbol)
			{
				Fault = _fault;
				FaultSymbol = _faultSymbol;
			}
		}

		public void InitializeElements()
		{

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
			
			VisualFlows = new VisualFlow[]
			{
				new VisualFlow("FlowPatientToBloodPump",(Storyboard)Resources["FlowPatientToBloodPump"],selectFlowPatientToBloodPump,() => Patient.ArteryFlow.Outgoing),
				new VisualFlow("FlowBloodPumpToMerge1",(Storyboard)Resources["FlowBloodPumpToMerge1"],selectFlowBloodPumpToMerge1,() => Machine.ExtracorporealBloodCircuit.ArterialBloodPump.MainFlow.Outgoing),
				new VisualFlow("FlowMerge1ToArterialChamber",(Storyboard)Resources["FlowMerge1ToArterialChamber"],selectFlowMerge1ToArterialChamber,() => Machine.ExtracorporealBloodCircuit.ArterialChamber.MainFlow.Incoming),
				new VisualFlow("FlowArterialChamberToDialyzer",(Storyboard)Resources["FlowArterialChamberToDialyzer"],selectFlowArterialChamberToDialyzer,() => Machine.ExtracorporealBloodCircuit.ArterialChamber.MainFlow.Outgoing),
				new VisualFlow("FlowDialyzerBloodSideToSplit2",(Storyboard)Resources["FlowDialyzerBloodSideToSplit2"],selectFlowDialyzerBloodSideToSplit2,() => Machine.Dialyzer.BloodFlow.Outgoing),
				new VisualFlow("FlowSplit2ToVenousChamber",(Storyboard)Resources["FlowSplit2ToVenousChamber"],selectFlowSplit2ToVenousChamber,() => Machine.ExtracorporealBloodCircuit.VenousChamber.MainFlow.Incoming),
				new VisualFlow("FlowVenousChamberToSafetyDetector",(Storyboard)Resources["FlowVenousChamberToSafetyDetector"],selectFlowVenousChamberToSafetyDetector,() => Machine.ExtracorporealBloodCircuit.VenousChamber.MainFlow.Outgoing),
				new VisualFlow("FlowSafetySensorToVenousValve",(Storyboard)Resources["FlowSafetySensorToVenousValve"],selectFlowSafetySensorToVenousValve,() => Machine.ExtracorporealBloodCircuit.VenousSafetyDetector.MainFlow.Outgoing),
				new VisualFlow("FlowVenousValveToPatient",(Storyboard)Resources["FlowVenousValveToPatient"],selectFlowVenousValveToPatient,() => Machine.ExtracorporealBloodCircuit.VenousTubingValve.MainFlow.Outgoing),
				new VisualFlow("FlowWaterSupplyToWaterPreparation",(Storyboard)Resources["FlowWaterSupplyToWaterPreparation"],selectFlowWaterSupplyToWaterPreparation,() => Machine.DialyzingFluidDeliverySystem.WaterSupply.MainFlow.Outgoing),
				new VisualFlow("FlowWaterPreparationToDialyzingFluidPreparation",(Storyboard)Resources["FlowWaterPreparationToDialyzingFluidPreparation"],selectFlowWaterPreparationToDialyzingFluidPreparation,() => Machine.DialyzingFluidDeliverySystem.WaterPreparation.MainFlow.Outgoing),
				new VisualFlow("FlowDialyzingFluidPreparationToBalanceChamber",(Storyboard)Resources["FlowDialyzingFluidPreparationToBalanceChamber"],selectFlowDialyzingFluidPreparationToBalanceChamber,() => Machine.DialyzingFluidDeliverySystem.DialyzingFluidPreparation.DialyzingFluidFlow.Outgoing),
				new VisualFlow("FlowBalanceChamberToDrain",(Storyboard)Resources["FlowBalanceChamberToDrain"],selectFlowBalanceChamberToDrain,() => Machine.DialyzingFluidDeliverySystem.BalanceChamber.ForwardUsedFlowSegment.Outgoing),
				new VisualFlow("FlowBalanceChamberToSafetyBypass",(Storyboard)Resources["FlowBalanceChamberToSafetyBypass"],selectFlowBalanceChamberToSafetyBypass,() => Machine.DialyzingFluidDeliverySystem.BalanceChamber.ForwardProducedFlowSegment.Outgoing),
				new VisualFlow("FlowSafetyBypassToDialyzer",(Storyboard)Resources["FlowSafetyBypassToDialyzer"],selectFlowSafetyBypassToDialyzer,() => Machine.DialyzingFluidDeliverySystem.SafetyBypass.MainFlow.Outgoing),
				new VisualFlow("FlowSafetyBypassToDrain",(Storyboard)Resources["FlowSafetyBypassToDrain"],selectFlowSafetyBypassToDrain,() => Machine.DialyzingFluidDeliverySystem.SafetyBypass.DrainFlow.Outgoing),
				new VisualFlow("FlowDialyzerDialyzingFluidSideToSplit3",(Storyboard)Resources["FlowDialyzerDialyzingFluidSideToSplit3"],selectFlowDialyzerDialyzingFluidSideToSplit3,() => Machine.Dialyzer.DialyzingFluidFlow.Outgoing),
				new VisualFlow("FlowSplit3ToPumpToBalanceChamber",(Storyboard)Resources["FlowSplit3ToPumpToBalanceChamber"],selectFlowSplit3ToPumpToBalanceChamber,() => Machine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.MainFlow.Incoming),
				new VisualFlow("FlowSplit3ToUltraFiltrationPump",(Storyboard)Resources["FlowSplit3ToUltraFiltrationPump"],selectFlowSplit3ToUltraFiltrationPump,() => Machine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.MainFlow.Incoming),
				new VisualFlow("FlowPumpToBalanceChamberToBalanceChamber",(Storyboard)Resources["FlowPumpToBalanceChamberToBalanceChamber"],selectFlowPumpToBalanceChamberToBalanceChamber,() => Machine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.MainFlow.Outgoing),
				new VisualFlow("FlowUltraFiltrationPumpToDrain",(Storyboard)Resources["FlowUltraFiltrationPumpToDrain"],selectFlowUltraFiltrationPumpToDrain,() => Machine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.MainFlow.Outgoing),
			};

			// Add custom buttons
			foreach (var visualFlow in VisualFlows)
			{
				var rectangle = visualFlow.SelectionRectangle;
				rectangle.MouseLeftButtonDown += SelectModelElement;
				SelectedModelElementToVisualFlow.Add(visualFlow.SelectionRectangle,visualFlow);
			}

			VisualFaults = new VisualFault[]
			{
				new VisualFault(() => Machine.Dialyzer.DialyzerMembraneRupturesFault,buttonFaultDialyzer),
				new VisualFault(() => Machine.ExtracorporealBloodCircuit.VenousTubingValve.ValveDoesNotClose,buttonFaultVenousValve),
				new VisualFault(() => Machine.ExtracorporealBloodCircuit.VenousSafetyDetector.SafetyDetectorDefect,buttonFaultSafetyDetector),
				new VisualFault(() => Machine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterDefect,buttonFaultWaterPreparation),
				new VisualFault(() => Machine.ExtracorporealBloodCircuit.ArterialBloodPump.BloodPumpDefect,buttonFaultBloodPump),
				new VisualFault(() => Machine.DialyzingFluidDeliverySystem.DialyzingFluidPreparation.DialyzingFluidPreparationPumpDefect,buttonFaultDialyzingFluidPreparation),
				new VisualFault(() => Machine.DialyzingFluidDeliverySystem.SafetyBypass.SafetyBypassFault,buttonFaultSafetyBypass),
				new VisualFault(() => Machine.DialyzingFluidDeliverySystem.PumpToBalanceChamber.PumpDefect,buttonFaultPumpToBalanceChamber),
			};
		}

		public HdMachine()
		{
			var specification = new SafetySharp.CaseStudies.HemodialysisMachine.Specification();

			InitializeComponent();
			InitializeElements();

			// Initialize the simulation environment
			SimulationControls.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.Reset += (o, e) => OnModelStateReset();
			SimulationControls.Rewound += (o, e) => OnRewound();
			SimulationControls.SetModel(specification);


			

			// Initialize the visualization state
			UpdateModelState();
			
			SimulationControls.MaxSpeed = 64;
			SimulationControls.ChangeSpeed(8);
		}


		private HemodialysisMachine.Model.HdMachine Machine => SimulationControls.Model.Roots.OfType<HemodialysisMachine.Model.HdMachine>().Single();
		private HemodialysisMachine.Model.Patient Patient => SimulationControls.Model.Roots.OfType<HemodialysisMachine.Model.Patient>().First();

		private Storyboard _animationBloodPumpEnabled;
		private Storyboard _animationHeparinPumpEnabled;
		private Storyboard _animationEnableWaterPreparation;
		private Storyboard _animationDisableWaterPreparation;
		private Storyboard _animationDialyzingFluidPreparationEnabled;
		private Storyboard _animationUltraFiltrationEnabled;
		private Storyboard _animationPumpToBalanceChamberEnabled;
		private Storyboard _animationCloseVenousTubingValve;
		private Storyboard _animationOpenVenousTubingValve;
		private Storyboard _animationArterialChamberDripping;
		private Storyboard _animationVenousChamberDripping;
		private Storyboard _animationArterialChamberNotDripping;
		private Storyboard _animationVenousChamberNotDripping;
		
		private LastVisualState _visualStateBloodPump;
		private LastVisualState _visualStateHeparinPump;
		private LastVisualState _visualStateWaterPreparation;
		private LastVisualState _visualStateDialyzingFluidPreparation;
		private LastVisualState _visualStateUltraFiltration;
		private LastVisualState _visualStatePumpToBalanceChamber;
		private LastVisualState _visualStateVenousTubingValve;
		private LastVisualState _visualStateArterialChamber;
		private LastVisualState _visualStateVenousChamber;

		private VisualFlow[] VisualFlows;
		private readonly Dictionary<Rectangle, VisualFlow> SelectedModelElementToVisualFlow=new Dictionary<Rectangle, VisualFlow>();

		private VisualFault[] VisualFaults;

		private System.Windows.Media.Brush Highlighted = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(178, 253, 153, 53));
		private System.Windows.Media.Brush NotHighlighted = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 253, 153, 53));
		private Rectangle SelectedModelElement;

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
			if (Machine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterEnabled() && _visualStateWaterPreparation != LastVisualState.Enabled)
			{
				_visualStateWaterPreparation = LastVisualState.Enabled;
				//_animationEnableWaterPreparation.RepeatBehavior = RepeatBehavior.Forever;
				_animationDisableWaterPreparation.Stop();
				_animationEnableWaterPreparation.Begin();
			}
			else if (Machine.DialyzingFluidDeliverySystem.WaterPreparation.WaterHeaterEnabled()==false && _visualStateWaterPreparation != LastVisualState.Disabled)
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
			if (Machine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.PumpSpeed > 0 && _visualStateUltraFiltration != LastVisualState.Enabled)
			{
				_visualStateUltraFiltration = LastVisualState.Enabled;
				_animationUltraFiltrationEnabled.RepeatBehavior = RepeatBehavior.Forever;
				_animationUltraFiltrationEnabled.Begin();
			}
			else if (Machine.DialyzingFluidDeliverySystem.DialyzingUltraFiltrationPump.PumpSpeed == 0 && _visualStateUltraFiltration != LastVisualState.Disabled)
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
			if (Machine.ExtracorporealBloodCircuit.ArterialChamber.MainFlow.Outgoing.Forward.HasWaterOrBigWaste() && _visualStateArterialChamber != LastVisualState.Enabled)
			{
				_visualStateArterialChamber = LastVisualState.Enabled;
				_animationArterialChamberDripping.RepeatBehavior = RepeatBehavior.Forever;
				_animationArterialChamberNotDripping.Stop();
				_animationArterialChamberDripping.Begin();
			}
			else if (Machine.ExtracorporealBloodCircuit.ArterialChamber.MainFlow.Outgoing.Forward.HasWaterOrBigWaste()==false && _visualStateArterialChamber != LastVisualState.Disabled)
			{
				_visualStateArterialChamber = LastVisualState.Disabled;
				_animationArterialChamberDripping.Stop();
				_animationArterialChamberNotDripping.Begin();
			}

			// VenousChamber
			if (Machine.ExtracorporealBloodCircuit.VenousChamber.MainFlow.Outgoing.Forward.HasWaterOrBigWaste() && _visualStateVenousChamber != LastVisualState.Enabled)
			{
				_visualStateVenousChamber = LastVisualState.Enabled;
				_animationVenousChamberDripping.RepeatBehavior = RepeatBehavior.Forever;
				_animationVenousChamberNotDripping.Stop();
				_animationVenousChamberDripping.Begin();
			}
			else if (Machine.ExtracorporealBloodCircuit.VenousChamber.MainFlow.Outgoing.Forward.HasWaterOrBigWaste() == false && _visualStateVenousChamber != LastVisualState.Disabled)
			{
				_visualStateVenousChamber = LastVisualState.Disabled;
				_animationVenousChamberDripping.Stop();
				_animationVenousChamberNotDripping.Begin();
			}
			
			foreach (var visualFlow in VisualFlows)
			{
				visualFlow.UpdateVisualization();
			}
			UpdateSelectedModelElementInfoText();
			UpdatePatientInfoText();
			UpdateFaultVisualization();

		}
		private void SelectModelElement(object sender, RoutedEventArgs e)
		{
			if (SelectedModelElement!=null) SelectedModelElement.Stroke = null;
			SelectedModelElement = (Rectangle)sender;
			SelectedModelElement.Stroke = Highlighted;

			UpdateSelectedModelElementInfoText();
			//Machine.ExtracorporealBloodCircuit.ArterialBloodPump.BloodPumpDefect.ToggleActivationMode();
		}

		public void UpdateSelectedModelElementInfoText()
		{
			var text = "";
			if (SelectedModelElement != null)
			{
				if (SelectedModelElementToVisualFlow.ContainsKey(SelectedModelElement))
					text = SelectedModelElementToVisualFlow[SelectedModelElement].GetInfoText();
			}
			textBlockSelectedElementInfos.Text = text;
		}

		public void UpdatePatientInfoText()
		{
			var textPatient = Patient.ValuesAsText();
			var textOutgoingBlood = Patient.ArteryFlow.Outgoing.Forward.ValuesAsText();
			var textIncomingBlood = Patient.VeinFlow.Incoming.Forward.ValuesAsText();
			textBlockPatientInfos.Text = "Patient:\n" + textPatient + "\n\nOutgoing (Artery):\n" + textOutgoingBlood + "\n\nIncoming (Vein):\n" + textIncomingBlood;
		}

		public void UpdateFaultVisualization()
		{
			foreach (var visualFault in VisualFaults)
			{
				if (visualFault.Fault().IsActivated)
				{
					visualFault.FaultSymbol.Style = (Style)Resources["FailureIndicator"];
				}
				else
				{
					visualFault.FaultSymbol.Style = (Style)Resources["NoFailureIndicator"];
				}
			}
		}

		/*
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
		*/
	}
}