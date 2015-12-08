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

namespace SafetySharp.Runtime
{
	using System;
	using Analysis;
	using Utilities;

	/// <summary>
	///   Simulates a S# model for debugging or testing purposes.
	/// </summary>
	public sealed class Simulator
	{
		/// <summary>
		///   The counter example that is replayed by the simulator.
		/// </summary>
		private readonly CounterExample _counterExample;

		/// <summary>
		///   The current state number of the counter example.
		/// </summary>
		private int _stateNumber;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be simulated.</param>
		/// <param name="formulas">The formulas that can be evaluated on the model.</param>
		public Simulator(Model model, params Formula[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			Model = model.ToRuntimeModel(formulas);
			Model.Reset();
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="counterExample">The counter example that should be simulated.</param>
		public Simulator(CounterExample counterExample)
		{
			Requires.NotNull(counterExample, nameof(counterExample));

			Model = counterExample.Model;
			_counterExample = counterExample;
			_counterExample.DeserializeState(0);
		}

		/// <summary>
		///   Gets the <see cref="RuntimeModel" /> that is simulated.
		/// </summary>
		public RuntimeModel Model { get; }

		/// <summary>
		///   Raised when the simulator has completed the simulation.
		/// </summary>
		public event EventHandler Completed;

		/// <summary>
		///   Runs the simulation for the <paramref name="timeSpan" />.
		/// </summary>
		/// <param name="timeSpan">The time span that should be simulated.</param>
		public void Simulate(TimeSpan timeSpan)
		{
			for (var i = 0; i < timeSpan.TotalSeconds; ++i)
				Model.ExecuteStep();

			Completed?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		///   Runs a step of the simulation.
		/// </summary>
		public void SimulateStep()
		{
			if (_counterExample == null)
				Model.ExecuteStep();
			else if (_stateNumber + 1 < _counterExample.StepCount)
			{
				_counterExample.DeserializeState(++_stateNumber);
				if (_stateNumber + 1 == _counterExample.StepCount)
					Completed?.Invoke(this, EventArgs.Empty);
			}
		}

		/// <summary>
		///   Resets the model to its initial state.
		/// </summary>
		public void Reset()
		{
			if (_counterExample == null)
				Model.Reset();
			else
			{
				_counterExample.DeserializeState(0);
				_stateNumber = 0;
			}
		}
	}
}