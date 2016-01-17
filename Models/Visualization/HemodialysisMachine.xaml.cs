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
	using Infrastructure;
	using SafetySharp.Runtime;

	public partial class HdMachine
	{
		public HdMachine()
		{
			var specification = new Specification();

			InitializeComponent();
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