// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using global::Elbtunnel;
	using global::Elbtunnel.Controllers;
	using global::Elbtunnel.Vehicles;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Reflection;

	public partial class Elbtunnel
	{
		private readonly RealTimeSimulator _simulator;
		private readonly Vehicle[] _vehicles;
		private bool _hazard;
		private HeightControl _heightControl;

		public Elbtunnel()
		{
			InitializeComponent();

			// Initialize the simulation environment
			var specification = new Specification();
			var model = Model.Create(specification);
			foreach (var fault in model.GetFaults())
				fault.OccurrenceKind = OccurrenceKind.Never;

			_simulator = new RealTimeSimulator(model, 1000, specification.Collision);
			_simulator.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.SetSimulator(_simulator);
			SimulationControls.Reset += (o, e) => OnModelStateReset();

			// Extract the components
			_vehicles = ((VehicleCollection)_simulator.Model.RootComponents[1]).Vehicles;
			_heightControl = (HeightControl)_simulator.Model.RootComponents[0];

			// Initialize the visualization state
			UpdateModelState();
			SimulationControls.ChangeSpeed(32);
		}

		private void OnModelStateReset()
		{
		}

		private void UpdateModelState()
		{
		}
	}
}