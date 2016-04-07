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
	using System.Collections.Concurrent;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using FormulaVisitors;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Checks whether an invariant holds for all states of a <see cref="RuntimeModel" /> instance.
	/// </summary>
	internal unsafe class InvariantChecker : DisposableObject
	{
		private const int ReportStateCountDelta = 200000;
		private readonly LoadBalancer _loadBalancer;
		private readonly Action<string> _output;
		private readonly StateStorage _states;
		private readonly Worker[] _workers;
		private long _computedTransitionCount;
		private CounterExample _counterExample;
		private Exception _exception;
		private int _generatingCounterExample = -1;
		private int _levelCount;
		private int _nextReport = ReportStateCountDelta;
		private readonly bool _progressOnly;
		private int _stateCount;
		private long _transitionCount;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal InvariantChecker(Func<RuntimeModel> createModel, Action<string> output, AnalysisConfiguration configuration)
		{
			Requires.NotNull(createModel, nameof(createModel));
			Requires.NotNull(output, nameof(output));

			_progressOnly = configuration.ProgressReportsOnly;
			_output = output;
			_workers = new Worker[configuration.CpuCount];

			var tasks = new Task[configuration.CpuCount];
			var stacks = new StateStack[configuration.CpuCount];

			_loadBalancer = new LoadBalancer(stacks);

			for (var i = 0; i < configuration.CpuCount; ++i)
			{
				var index = i;
				tasks[i] = Task.Factory.StartNew(() =>
				{
					stacks[index] = new StateStack(configuration.StackCapacity);
					_workers[index] = new Worker(index, this, stacks[index], createModel(), configuration.SuccessorCapacity);
				});
			}

			Task.WaitAll(tasks);

			_states = new StateStorage(_workers[0].StateVectorLayout, configuration.StateCapacity);

#if false
			Console.WriteLine(_model.StateVectorLayout);
#endif
		}

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		internal AnalysisResult Check()
		{
			if (!_progressOnly)
			{
				_output($"Performing invariant check with {_workers.Length} CPU cores.");
				_output($"State vector has {_workers[0].StateVectorLayout.SizeInBytes} bytes.");
			}

			_workers[0].ComputeInitialStates();

			var tasks = new Task[_workers.Length];
			for (var i = 0; i < _workers.Length; ++i)
				tasks[i] = Task.Factory.StartNew(_workers[i].Check);

			Task.WaitAll(tasks);

			if (!_progressOnly)
				Report();

			if (_exception != null)
				throw new AnalysisException(_exception, _counterExample);

			if (_counterExample != null && !_progressOnly)
				_output("Invariant violation detected.");

			return new AnalysisResult(_counterExample == null, _counterExample, _stateCount, _transitionCount, _computedTransitionCount, _levelCount);
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_states.SafeDispose();
			_workers.SafeDisposeAll();
		}

		/// <summary>
		///   Reports the number of states and transitions that have been checked.
		/// </summary>
		private void Report()
		{
			_output($"Discovered {_stateCount:n0} states, {_transitionCount:n0} transitions, {_levelCount} levels.");
		}

		/// <summary>
		///   Atomically exchanges <paramref name="location" /> with <paramref name="newValue" /> if <paramref name="comparison" /> is
		///   greater than <paramref name="location" />.
		/// </summary>
		private static bool InterlockedExchangeIfGreaterThan(ref int location, int comparison, int newValue)
		{
			int initialValue;
			do
			{
				initialValue = location;
				if (initialValue >= comparison)
					return false;
			} while (Interlocked.CompareExchange(ref location, newValue, initialValue) != initialValue);

			return true;
		}

		/// <summary>
		///   Represents a thread that checks for invariant violations.
		/// </summary>
		private class Worker : DisposableObject
		{
			private readonly InvariantChecker _context;
			private readonly int _index;
			private readonly RuntimeModel _model;
			private readonly StateStack _stateStack;
			private readonly TransitionSet _transitions;
			private StateStorage _states;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			public Worker(int index, InvariantChecker context, StateStack stateStack, RuntimeModel model, int successorCapacity)
			{
				_index = index;

				_context = context;
				_model = model;
				_stateStack = stateStack;

				var invariant = CompilationVisitor.Compile(_model.Formulas[0]);
				_transitions = new TransitionSet(model, successorCapacity, invariant);
			}

			/// <summary>
			///   Gets the state vector layout of the worker's model.
			/// </summary>
			public StateVectorLayout StateVectorLayout => _model.StateVectorLayout;

			/// <summary>
			///   Computes the model's initial states.
			/// </summary>
			public void ComputeInitialStates()
			{
				try
				{
					_states = _context._states;
					_model.ComputeInitialStates(_transitions);

					AddStates();
				}
				catch (Exception e)
				{
					_context._loadBalancer.Terminate();
					_context._exception = e;

					var trace = new[] { _model.ConstructionState, new byte[_model.StateVectorSize] };
					var choices = new[] { _model.GetLastChoices() };
					_context._counterExample = new CounterExample(_model, trace, choices);
				}
			}

			/// <summary>
			///   Checks whether the model's invariant holds for all states.
			/// </summary>
			public void Check()
			{
				_states = _context._states;

				try
				{
					while (_context._loadBalancer.LoadBalance(_index))
					{
						int state;
						if (!_stateStack.TryGetState(out state))
							continue;

						_transitions.Clear();
						_model.ComputeSuccessorStates(_transitions, _states[state]);
						AddStates();

						InterlockedExchangeIfGreaterThan(ref _context._levelCount, _stateStack.FrameCount, _stateStack.FrameCount);
						if (InterlockedExchangeIfGreaterThan(ref _context._nextReport, _context._stateCount, _context._nextReport + ReportStateCountDelta))
							_context.Report();
					}
				}
				catch (OutOfMemoryException e)
				{
					_context._loadBalancer.Terminate();
					_context._exception = e;
				}
				catch (Exception e)
				{
					_context._loadBalancer.Terminate();
					_context._exception = e;

					CreateCounterExample(endsWithException: true);
				}
			}

			/// <summary>
			///   Adds the states stored in the <see cref="_transitions" /> cache.
			/// </summary>
			private void AddStates()
			{
				if (_transitions.Count == 0)
					throw new InvalidOperationException("Deadlock detected.");

				Interlocked.Add(ref _context._transitionCount, _transitions.Count);
				Interlocked.Add(ref _context._computedTransitionCount, _transitions.ComputedTransitionCount);

				_stateStack.PushFrame();

				for (var i = 0; i < _transitions.Count; ++i)
				{
					var transition = _transitions[i];

					// Store the state if it hasn't been discovered before
					int index;
					if (_states.AddState(transition->TargetState, out index))
					{
						Interlocked.Increment(ref _context._stateCount);
						_stateStack.PushState(index);
					}

					// Check if the invariant is violated; if so, generate a counter example and abort
					if (!transition->Formulas[0])
					{
						_context._loadBalancer.Terminate();
						CreateCounterExample(endsWithException: false);

						return;
					}
				}
			}

			/// <summary>
			///   Creates a counter example for the current topmost state.
			/// </summary>
			private void CreateCounterExample(bool endsWithException)
			{
				if (Interlocked.CompareExchange(ref _context._generatingCounterExample, _index, -1) != -1)
					return;

				var indexedTrace = _stateStack.GetTrace();
				var traceLength = 1 + (endsWithException ? indexedTrace.Length + 1 : indexedTrace.Length);
				var trace = new byte[traceLength][];
				trace[0] = _model.ConstructionState;

				if (endsWithException)
					trace[trace.Length - 1] = new byte[_model.StateVectorSize];

				for (var i = 0; i < indexedTrace.Length; ++i)
				{
					trace[i + 1] = new byte[_model.StateVectorSize];
					Marshal.Copy(new IntPtr((int*)_states[indexedTrace[i]]), trace[i + 1], 0, trace[i + 1].Length);
				}

				_context._counterExample = new CounterExample(_model, trace, _model.GenerateReplayInformation(trace, endsWithException));
			}

			/// <summary>
			///   Disposes the object, releasing all managed and unmanaged resources.
			/// </summary>
			/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
			protected override void OnDisposing(bool disposing)
			{
				if (!disposing)
					return;

				_model.SafeDispose();
				_stateStack.SafeDispose();
			}
		}

		/// <summary>
		///   Balances the load of multiple <see cref="Worker" /> instances.
		/// </summary>
		private class LoadBalancer
		{
			private readonly bool[] _awaitingWork;
			private readonly ConcurrentQueue<int> _idleWorkers;
			private readonly StateStack[] _stacks;

			private volatile bool _terminated;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="stacks"></param>
			public LoadBalancer(StateStack[] stacks)
			{
				_stacks = stacks;
				_idleWorkers = new ConcurrentQueue<int>();
				_awaitingWork = new bool[stacks.Length];
			}

			/// <summary>
			///   Gets the number of workers.
			/// </summary>
			private int WorkerCount => _stacks.Length;

			/// <summary>
			///   Balances the load between <see cref="Worker" /> instances. Returns <c>false</c> to indicate that the worker should
			///   terminate.
			/// </summary>
			public bool LoadBalance(int workerIndex)
			{
				// If the invariant check has been terminated, terminate the worker
				if (_terminated)
					return false;

				var hasWork = _stacks[workerIndex].FrameCount > 0;
				var areWorkersIdle = !_idleWorkers.IsEmpty;

				// If the worker still has work and no other worker is idle, let the worker continue
				if (hasWork && !areWorkersIdle)
					return true;

				// If the worker doesn't have any work, wait until new work is assigned to it or there is no more work
				if (!hasWork)
					return AwaitWork(workerIndex);

				// Try to assign some of the worker's work to an idle worker
				return AssignWork(workerIndex);
			}

			/// <summary>
			///   Assigns work to an idle worker.
			/// </summary>
			private bool AssignWork(int workerIndex)
			{
				int idleWorker;
				if (!_idleWorkers.TryDequeue(out idleWorker))
					return true;

				// At this point we've got an idle worker that we can assign work to
				Assert.That(workerIndex != idleWorker, "Worker tries to assign work to itself.");
				_stacks[workerIndex].SplitWork(_stacks[idleWorker]);

				// If the worker actually got some new work, notify it, otherwise continue waiting
				if (_stacks[idleWorker].FrameCount > 0)
					Volatile.Write(ref _awaitingWork[idleWorker], false);
				else
					_idleWorkers.Enqueue(idleWorker);

				return true;
			}

			/// <summary>
			///   Stalls the worker until work has been assigned to it or there is no more work.
			/// </summary>
			private bool AwaitWork(int workerIndex)
			{
				_idleWorkers.Enqueue(workerIndex);
				Volatile.Write(ref _awaitingWork[workerIndex], true);

				var spinWait = new SpinWait();
				while (Volatile.Read(ref _awaitingWork[workerIndex]) && !_terminated)
				{
					// If all workers are idle, terminate the invariant check, otherwise wait a bit
					// before checking again for new work
					if (_idleWorkers.Count == WorkerCount)
						Terminate();
					else
						spinWait.SpinOnce();
				}

				// The worker now either has work available and it can continue, or the invariant check has been
				// terminated and so the worker should terminate
				return !_terminated;
			}

			/// <summary>
			///   Terminates the invariant check.
			/// </summary>
			public void Terminate()
			{
				_terminated = true;
			}
		}
	}
}