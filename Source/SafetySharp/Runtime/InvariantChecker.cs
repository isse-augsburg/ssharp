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
	using System.IO;
	using System.Runtime.InteropServices;
	using Analysis;
	using Analysis.FormulaVisitors;
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
		private const int ReportStateCountDelta = 200000;

		/// <summary>
		///   The invariant that is checked.
		/// </summary>
		private readonly Func<bool> _invariant;

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
		///   The trace to the states that are currently being checked.
		/// </summary>
		private readonly StateStack _stateStack;

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
		/// <param name="capacity">The number of states that can be stored.</param>
		public InvariantChecker(Model model, Formula invariant, Action<string> output, int capacity)
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
				_states = new StateStorage(_model.StateSlotCount, capacity);
				_stateStack = new StateStack(capacity);
				_output = output;
				_invariant = CompilationVisitor.Compile(_model.Formulas[0]);
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
		public int LevelCount { get; private set; }

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		public CounterExample Check()
		{
			Console.WriteLine($"State vector has {_model.StateSlotCount} slots ({_model.StateSlotCount * sizeof(int)} bytes).");

			AddStates(_model.ComputeInitialStates());

			int state;
			while (!_invariantViolated && _stateStack.TryGetState(out state))
			{
				AddStates(_model.ComputeSuccessorStates((int*)_states[state]));

				if (StateCount >= _nextReport)
				{
					_nextReport += ReportStateCountDelta;
					Report();
				}

				if (_stateStack.FrameCount > LevelCount)
					LevelCount = _stateStack.FrameCount;
			}

			Report();
			return _invariantViolated ? CreateCounterExample() : null;
		}

		/// <summary>
		///   Adds the states stored in the <paramref name="stateCache" />.
		/// </summary>
		private void AddStates(StateCache stateCache)
		{
			if (stateCache.StateCount == 0)
				throw new InvalidOperationException("Deadlock detected.");

			TransitionCount += stateCache.StateCount;
			_stateStack.PushFrame();

			for (var i = 0; i < stateCache.StateCount; ++i)
			{
				int index;
				if (!_states.AddState((byte*)(stateCache.StateMemory + i * stateCache.SlotCount), out index))
					continue;

				// TODO: Optimize - do not deserialize the state again, check the formula while we still have the deserialized state instead
				_model.Deserialize(stateCache.StateMemory + i * stateCache.SlotCount);
				if (!_invariant())
					_invariantViolated = true;

				StateCount += 1;
				_stateStack.PushState(index);
			}
		}

		/// <summary>
		///   Creates a counter example for the current topmost state.
		/// </summary>
		private CounterExample CreateCounterExample()
		{
			var indexedTrace = _stateStack.GetTrace();
			var trace = new int[indexedTrace.Length][];

			for (var i = 0; i < indexedTrace.Length; ++i)
			{
				trace[i] = new int[_model.StateSlotCount];
				Marshal.Copy(new IntPtr((int*)_states[indexedTrace[i]]), trace[i], 0, trace[i].Length);
			}

			return new CounterExample(_model, _serializedModel, trace);
		}

		/// <summary>
		///   Reports the number of states and transitions that have been checked.
		/// </summary>
		private void Report()
		{
			_output($"Explored {StateCount:n0} states, {TransitionCount:n0} transitions, {LevelCount} levels.");
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_stateStack.SafeDispose();
			_states.SafeDispose();
			_model.SafeDispose();
		}
	}
}