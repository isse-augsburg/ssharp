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
	using System.Collections.Generic;
	using System.IO;
	using Analysis;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Checks whether an invariant holds for all states of a <see cref="RuntimeModel" /> instance.
	/// </summary>
	internal unsafe class InvariantChecker : DisposableObject
	{
		/// <summary>
		///   The number of states that must be checked between two consecutive status reports.
		/// </summary>
		private const int ReportStateCountDelta = 100000;

		/// <summary>
		///   The model that is checked.
		/// </summary>
		private readonly RuntimeModel _model;

		/// <summary>
		///   The callback that is used to output messages.
		/// </summary>
		private readonly Action<string> _output;

		/// <summary>
		///   The serialized version of the runtime model.
		/// </summary>
		private readonly byte[] _serializedModel;

		/// <summary>
		///   The states that have been checked already.
		/// </summary>
		private readonly StateStorage _states;

		/// <summary>
		///   The trace to the state that is currently being checked.
		/// </summary>
		private readonly Stack<int> _stateStack = new Stack<int>();

		/// <summary>
		///   Indicates whether the invariant is violated.
		/// </summary>
		private bool _invariantViolated;

		/// <summary>
		///   The number of states that must be reached before a status report is printed.
		/// </summary>
		private int _nextReport = ReportStateCountDelta;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		public InvariantChecker(Model model, Formula invariant, Action<string> output)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(invariant, nameof(invariant));
			Requires.NotNull(output, nameof(output));

			using (var memoryStream = new MemoryStream())
			{
				RuntimeModelSerializer.Save(memoryStream, model, false, invariant);
				memoryStream.Seek(0, SeekOrigin.Begin);

				_serializedModel = memoryStream.ToArray();
				_model = RuntimeModelSerializer.Load(memoryStream);
				_states = new StateStorage(_model.StateSlotCount, 1 << 24);
				_output = output;
			}
		}

		/// <summary>
		///   Gets the number of states that have been checked.
		/// </summary>
		public int StateCount { get; private set; }

		/// <summary>
		///   Gets the number of transitions that have been checked.
		/// </summary>
		public int TransitionCount { get; private set; }

		/// <summary>
		///   Gets the number of levels that have been checked.
		/// </summary>
		public int Levels { get; private set; }

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		public CounterExample Check()
		{
			AddStates(_model.ComputeInitialStates());
			while (!_invariantViolated && _stateStack.Count != 0)
			{
				var state = _stateStack.Pop();
				AddStates(_model.ComputeSuccessorStates((int*)_states[state]));

				if (StateCount <= _nextReport)
					continue;

				_nextReport += ReportStateCountDelta;
				Report();
			}

			Report();
			// TODO: Counter example generation
			return _invariantViolated ? new CounterExample(_model, _serializedModel, new int[0][]) : null;
		}

		/// <summary>
		///   Adds the states stored in the <paramref name="stateCache" />.
		/// </summary>
		private void AddStates(StateCache stateCache)
		{
			if (stateCache.StateCount == 0)
				throw new InvalidOperationException("Deadlock detected.");

			TransitionCount += stateCache.StateCount;

			for (var i = 0; i < stateCache.StateCount; ++i)
			{
				int index;
				if (!_states.AddState((byte*)(stateCache.StateMemory + i * stateCache.SlotCount), out index))
					continue;

				// TODO: Optimize - do not deserialize the state again, check the formula while we still have the deserialized state instead
				_model.Deserialize(stateCache.StateMemory + i * stateCache.SlotCount);
				if (!_model.Formulas[0].Evaluate())
					_invariantViolated = true;

				StateCount += 1;
				_stateStack.Push(index);
			}
		}

		/// <summary>
		///   Reports the number of states and transitions that have been checked.
		/// </summary>
		private void Report()
		{
			_output($"explored {StateCount:n0} states, {TransitionCount:n0} transitions");
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			_states.SafeDispose();
			_model.SafeDispose();
		}
	}
}