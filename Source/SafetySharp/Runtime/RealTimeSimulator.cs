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
		}

		/// <summary>
		///   Gets a value indicating whether a simulation is currently running.
		/// </summary>
		public bool IsRunning => _timer != null && !IsCompleted;

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
		///   Gets a value indicating whether the simulation is completed.
		/// </summary>
		public bool IsCompleted => _simulator.IsCompleted;

		/// <summary>
		///   Gets a value indicating whether the simulation can be fast-forwarded.
		/// </summary>
		public bool CanFastForward => _simulator.CanFastForward;

		/// <summary>
		///   Gets a value indicating whether the simulation can be rewound.
		/// </summary>
		public bool CanRewind => _simulator.CanRewind;

		/// <summary>
		///   Gets a value indicating whether the simulator is replaying a counter example.
		/// </summary>
		public bool IsReplay => _simulator.IsReplay;

		/// <summary>
		///   Raised when the simulated model state has been changed.
		/// </summary>
		public event EventHandler ModelStateChanged;

		/// <summary>
		///   Runs the simulation in real-time mode. This method can only be called if the simulation is not already running.
		/// </summary>
		public void Run()
		{
			Requires.That(SynchronizationContext.Current != null, "The simulation cannot be run without a valid SynchronizationContext.");

			if (_timer == null)
				_timer = new Timer(state1 => _synchronizationContext.Post(state2 => ExecuteStep(), null), null, 0, _stepDelay);
		}

		/// <summary>
		///   Resets the simulation to its initial state.
		/// </summary>
		public void Reset()
		{
			_simulator.Reset();
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Pauses the simulation.
		/// </summary>
		public void Pause()
		{
			_timer?.Dispose();
			_timer = null;
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Executes the next step of the simulation.
		/// </summary>
		private void ExecuteStep()
		{
			_simulator.SimulateStep();
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Advances the simulator by the given number of <paramref name="steps" />, if possible.
		/// </summary>
		/// <param name="steps">The number of steps the simulation should be advanced.</param>
		public void FastForward(int steps)
		{
			_simulator.FastForward(steps);
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Rewinds the simulator by the given number of <paramref name="steps" />, if possible.
		/// </summary>
		/// <param name="steps">The number of steps that should be rewound.</param>
		public void Rewind(int steps)
		{
			_simulator.Rewind(steps);
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Prunes all states lying in the future after a rewind.
		/// </summary>
		public void Prune()
		{
			_simulator.Prune();
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Replays the next transition of the simulated counter example.
		/// </summary>
		public void Replay()
		{
			_simulator.Replay();
			ModelStateChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}