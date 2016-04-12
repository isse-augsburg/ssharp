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

namespace SafetySharp.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using Modeling;
	using Runtime;
	using Utilities;

	/// <summary>
	///   Simulates a S# model for debugging or testing purposes.
	/// </summary>
	public sealed unsafe class Simulator
	{
		/// <summary>
		///   The counter example that is replayed by the simulator.
		/// </summary>
		private readonly CounterExample _counterExample;

		/// <summary>
		///   The runtime model that is simulated.
		/// </summary>
		private readonly RuntimeModel _runtimeModel;

		/// <summary>
		///   The states encountered by the simulator.
		/// </summary>
		private readonly List<byte[]> _states = new List<byte[]>();

		/// <summary>
		///   The current state number of the simulator.
		/// </summary>
		private int _stateIndex;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be simulated.</param>
		/// <param name="formulas">The formulas that can be evaluated on the model.</param>
		public Simulator(ModelBase model, params Formula[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			_runtimeModel = model.ToRuntimeModel(formulas);
			Reset();
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="counterExample">The counter example that should be simulated.</param>
		public Simulator(CounterExample counterExample)
		{
			Requires.NotNull(counterExample, nameof(counterExample));

			_counterExample = counterExample;
			_runtimeModel = counterExample.RuntimeModel;

			Reset();
		}

		/// <summary>
		///   Gets the model that is simulated, i.e., a copy of the original model passed to the simulator.
		/// </summary>
		public ModelBase Model => _runtimeModel.Model;

		/// <summary>
		///   Gets a value indicating whether the simulator is replaying a counter example.
		/// </summary>
		public bool IsReplay => _counterExample != null;

		/// <summary>
		///   Gets a value indicating whether the simulation is completed.
		/// </summary>
		public bool IsCompleted => _counterExample != null && _stateIndex + 1 == _counterExample.StepCount;

		/// <summary>
		///   Gets a value indicating whether the simulation can be fast-forwarded.
		/// </summary>
		public bool CanFastForward => _counterExample == null || !IsCompleted;

		/// <summary>
		///   Gets a value indicating whether the simulation can be rewound.
		/// </summary>
		public bool CanRewind => _stateIndex > 0;

		/// <summary>
		///   Runs a step of the simulation. Returns <c>false</c> to indicate that the simulation is completed.
		/// </summary>
		public void SimulateStep()
		{
			Prune();

			var state = stackalloc byte[_runtimeModel.StateVectorSize];

			if (_counterExample == null)
			{
				_runtimeModel.ExecuteStep();

				_runtimeModel.Serialize(state);
				AddState(state);
			}
			else
			{
				if (_stateIndex + 1 >= _counterExample.StepCount)
					return;

				_counterExample.DeserializeState(_stateIndex + 1);
				_runtimeModel.Serialize(state);

				AddState(state);
				Replay();
			}
		}

		/// <summary>
		///   Advances the simulator by the given number of <paramref name="steps" />, if possible.
		/// </summary>
		/// <param name="steps">The number of steps the simulation should be advanced.</param>
		public void FastForward(int steps)
		{
			// Reuse the already discovered states after a rewind
			var advanceCount = Math.Min(steps, _states.Count - _stateIndex - 1);
			_stateIndex += advanceCount;
			RestoreState(Math.Min(_stateIndex, _states.Count - 1));

			// Continue the simulation once we cannot reuse previously discovered states
			for (var i = 0; i < steps - advanceCount; ++i)
			{
				SimulateStep();

				if (IsCompleted)
					return;
			}
		}

		/// <summary>
		///   Rewinds the simulator by the given number of <paramref name="steps" />, if possible.
		/// </summary>
		/// <param name="steps">The number of steps that should be rewound.</param>
		public void Rewind(int steps)
		{
			_stateIndex = Math.Max(0, _stateIndex - steps);
			RestoreState(_stateIndex);
		}

		/// <summary>
		///   Prunes all states lying in the future after a rewind.
		/// </summary>
		public void Prune()
		{
			// Delete all previously discovered states that lie in the future if the simulation has
			// been rewinded and is resimulated from a certain point in the past
			for (var i = _states.Count - 1; i >= _stateIndex + 1; --i)
				_states.RemoveAt(i);
		}

		/// <summary>
		///   Resets the model to its initial state.
		/// </summary>
		public void Reset()
		{
			var state = stackalloc byte[_runtimeModel.StateVectorSize];

			_states.Clear();
			_stateIndex = -1;

			if (_counterExample == null)
				_runtimeModel.Reset();
			else
				_counterExample.ReplayInitialState();

			_runtimeModel.Serialize(state);
			AddState(state);
		}

		/// <summary>
		///   Replays the next transition of the simulated counter example.
		/// </summary>
		private void Replay()
		{
			fixed (byte* sourceState = _states[_stateIndex - 1])
				_runtimeModel.Replay(sourceState, _counterExample.GetReplayInformation(_stateIndex - 1), initializationStep: _stateIndex == -1);

			var state = stackalloc byte[_runtimeModel.StateVectorSize];
			_runtimeModel.Serialize(state);

			for (var i = 0; i < _runtimeModel.StateVectorSize; ++i)
				Requires.That(state[i] == _states[_stateIndex][i], "Invalid replay of counter example: Unexpected state difference.");
		}

		/// <summary>
		///   Adds the state to the simulator.
		/// </summary>
		private void AddState(byte* state)
		{
			var newState = new byte[_runtimeModel.StateVectorSize];
			Marshal.Copy(new IntPtr(state), newState, 0, _runtimeModel.StateVectorSize);

			_states.Add(newState);
			++_stateIndex;
		}

		/// <summary>
		///   Restores a previously discovered state.
		/// </summary>
		private void RestoreState(int stateNumber)
		{
			fixed (byte* state = _states[stateNumber])
				_runtimeModel.Deserialize(state);
		}
	}
}