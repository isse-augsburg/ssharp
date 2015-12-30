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

namespace SafetySharp.Runtime
{
	using System;
	using System.Threading;
	using Utilities;

	/// <summary>
	///   Simulates a S# model for visualization purposes or hardware-in-the-loop tests.
	/// </summary>
	public sealed class RealTimeSimulator
	{
		/// <summary>
		///   The simulator that is used to simulate the model.
		/// </summary>
		private readonly Simulator _simulator;

		/// <summary>
		///   The synchronization context that is used to marshal back to the main thread.
		/// </summary>
		private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

		/// <summary>
		///   The current state of the simulation.
		/// </summary>
		private SimulationState _state;

		/// <summary>
		///   The time to wait between two simulation steps.
		/// </summary>
		private int _stepDelay;

		/// <summary>
		///   The timer that is used to schedule simulation updates.
		/// </summary>
		private Timer _timer;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="simulator">The simulator that should be used to simulate the model.</param>
		/// <param name="stepDelay">The step delay in milliseconds, i.e., time to wait between two steps in running mode.</param>
		public RealTimeSimulator(Simulator simulator, int stepDelay)
		{
			Requires.NotNull(simulator, nameof(simulator));
			Requires.That(SynchronizationContext.Current != null, "The simulator requires a valid synchronization context to be set.");

			_simulator = simulator;
			_stepDelay = stepDelay;
			_state = SimulationState.Stopped;
		}

		/// <summary>
		///   Gets the current state of the simulation.
		/// </summary>
		public SimulationState State
		{
			get { return _state; }
			set
			{
				if (_state == value)
					return;

				_state = value;
				SimulationStateChanged?.Invoke(this, EventArgs.Empty);

				if (_state != SimulationState.Running && _timer != null)
				{
					_timer.Dispose();
					_timer = null;
				}
				else if (_state == SimulationState.Running)
					_timer = new Timer(state1 => _synchronizationContext.Post(state2 => ExecuteStep(), null), null, 0, _stepDelay);
			}
		}

		/// <summary>
		///   Gets or sets the step delay in milliseconds, i.e., time to wait between two steps in running mode.
		/// </summary>
		public int StepDelay
		{
			get { return _stepDelay; }
			set
			{
				if (_stepDelay == value)
					return;

				_stepDelay = value;
				_timer?.Change(_stepDelay, _stepDelay);
				ModelStateChanged?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		///   Gets the <see cref="RuntimeModel" /> that is simulated.
		/// </summary>
		public RuntimeModel Model => _simulator.Model;

		/// <summary>
		///   Raised when the simulator has completed the simulation.
		/// </summary>
		public event EventHandler Completed
		{
			add { _simulator.Completed += value; }
			remove { _simulator.Completed -= value; }
		}

		/// <summary>
		///   Raised when the simulator's simulation state has been changed.
		/// </summary>
		public event EventHandler SimulationStateChanged;

		/// <summary>
		///   Raised when the simulated model state has been changed.
		/// </summary>
		public event EventHandler ModelStateChanged;

		/// <summary>
		///   Executes the next step of the simulation. This method can only be called when the simulation is paused or stopped.
		/// </summary>
		public void Step()
		{
			Requires.That(State != SimulationState.Running, "The simulation is already running.");

			State = SimulationState.Paused;
			ExecuteStep();
		}

		/// <summary>
		///   Runs the simulation in real-time mode. This method can only be called if the simulation is not already running.
		/// </summary>
		public void Run()
		{
			Requires.That(State != SimulationState.Running, "The simulation is already running.");
			Requires.That(SynchronizationContext.Current != null, "The simulation cannot be run without a valid SynchronizationContext.");

			State = SimulationState.Running;
		}

		/// <summary>
		///   Stops the simulation and resets it to its initial state. This method can only be called if the simulation is
		///   currently running or in paused mode.
		/// </summary>
		public void Stop()
		{
			Requires.That(State != SimulationState.Stopped, "The simulation is already stopped.");
			State = SimulationState.Stopped;

			_simulator.Reset();
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Pauses the simulation. This method can only be called when the simulation is currently running.
		/// </summary>
		public void Pause()
		{
			Requires.That(State == SimulationState.Running, "Only running simulations can be stopped.");
			State = SimulationState.Paused;
		}

		/// <summary>
		///   Executes the next step of the simulation.
		/// </summary>
		private void ExecuteStep()
		{
			_simulator.SimulateStep();
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}