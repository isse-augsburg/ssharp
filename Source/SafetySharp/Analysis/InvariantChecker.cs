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
	using Modeling;
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
		private readonly bool _progressOnly;
		private readonly StateStorage _states;
		private readonly bool _suppressCounterExampleGeneration;
		private readonly Worker[] _workers;
		private long _computedTransitionCount;
		private CounterExample _counterExample;
		private Exception _exception;
		private bool _formulaIsValid = true;
		private int _generatingCounterExample = -1;
		private int _levelCount;
		private int _nextReport = ReportStateCountDelta;
		private int _stateCount;
		private long _transitionCount;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal InvariantChecker(Func<RuntimeModel> createModel, Action<string> output, AnalysisConfiguration configuration)
		{
			Requires.NotNull(createModel, nameof(createModel));
			Requires.NotNull(output, nameof(output));

			_progressOnly = configuration.ProgressReportsOnly;
			_output = output;
			_workers = new Worker[configuration.CpuCount];
			_suppressCounterExampleGeneration = !configuration.GenerateCounterExample;

			var tasks = new Task[configuration.CpuCount];
			var stacks = new StateStack[configuration.CpuCount];

			_loadBalancer = new LoadBalancer(stacks);

			for (var i = 0; i < configuration.CpuCount; ++i)
			{
				var index = i;
				tasks[i] = Task.Factory.StartNew(() =>
				{
					stacks[index] = new StateStack(configuration.StackCapacity);
					_workers[index] = new Worker(index, this, stacks[index], createModel, configuration.SuccessorCapacity);
				});
			}

			Task.WaitAll(tasks);

			_states = new StateStorage(_workers[0].StateVectorLayout, configuration.StateCapacity);
		}

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		internal AnalysisResult Check()
		{
			Reset();

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

			return new AnalysisResult
			{
				FormulaHolds = _formulaIsValid,
				CounterExample = _counterExample,
				StateCount = _stateCount,
				TransitionCount = _transitionCount,
				ComputedTransitionCount = _computedTransitionCount,
				LevelCount = _levelCount,
				StateVectorLayout = _workers[0].StateVectorLayout
			};
		}

		/// <summary>
		///   Updates the activation states of the model's faults.
		/// </summary>
		/// <param name="getActivation">The callback that should be used to determine a fault's activation state.</param>
		internal void ChangeFaultActivations(Func<Fault, Activation> getActivation)
		{
			foreach (var worker in _workers)
				worker.ChangeFaultActivations(getActivation);
		}

		/// <summary>
		///   Resets the checker so that a new invariant check can be started.
		/// </summary>
		private void Reset()
		{
			_formulaIsValid = true;
			_computedTransitionCount = 0;
			_counterExample = null;
			_exception = null;
			_generatingCounterExample = -1;
			_levelCount = 0;
			_nextReport = ReportStateCountDelta;
			_stateCount = 0;
			_transitionCount = 0;

			_loadBalancer.Reset();
			_states.Clear();

			foreach (var worker in _workers)
				worker.Reset();
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
			private readonly Func<RuntimeModel> _createModel;
			private readonly int _index;
			private readonly RuntimeModel _model;
			private readonly StateStack _stateStack;
			private readonly TransitionSet _transitions;
			private StateStorage _states;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			public Worker(int index, InvariantChecker context, StateStack stateStack, Func<RuntimeModel> createModel, int successorCapacity)
			{
				_index = index;

				_context = context;
				_createModel = createModel;
				_stateStack = stateStack;
				_model = _createModel();

				var invariant = CompilationVisitor.Compile(_model.Formulas[0]);
				_transitions = new TransitionSet(_model, successorCapacity, invariant);
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
					_context._counterExample = new CounterExample(_model, trace, choices, endsWithException: true);
				}
			}

			/// <summary>
			///   Checks whether the model's invariant holds for all states.
			/// </summary>
			internal void Check()
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
			///   Updates the activation states of the worker's faults.
			/// </summary>
			/// <param name="getActivation">The callback that should be used to determine a fault's activation state.</param>
			internal void ChangeFaultActivations(Func<Fault, Activation> getActivation)
			{
				_model.ChangeFaultActivations(getActivation);
			}

			/// <summary>
			///   Resets the worker so that a new invariant check can be started.
			/// </summary>
			internal void Reset()
			{
				_model.Reset();
				_stateStack.Clear();
				_transitions.Clear();
			}

			/// <summary>
			///   Adds the states stored in the <see cref="_transitions" /> cache.
			/// </summary>
			private void AddStates()
			{
				if (_transitions.Count == 0)
					throw new InvalidOperationException("Deadlock detected.");

				var transitionCount = 0;
				_stateStack.PushFrame();

				for (var i = 0; i < _transitions.Count; ++i)
				{
					var transition = _transitions[i];
					if (!transition->IsValid)
						continue;

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
						_context._formulaIsValid = false;
						_context._loadBalancer.Terminate();
						CreateCounterExample(endsWithException: false);

						return;
					}

					++transitionCount;
				}

				Interlocked.Add(ref _context._transitionCount, transitionCount);
				Interlocked.Add(ref _context._computedTransitionCount, _transitions.ComputedTransitionCount);
			}

			/// <summary>
			///   Creates a counter example for the current topmost state.
			/// </summary>
			private void CreateCounterExample(bool endsWithException)
			{
				if (Interlocked.CompareExchange(ref _context._generatingCounterExample, _index, -1) != -1)
					return;

				if (_context._suppressCounterExampleGeneration)
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

				// We have to create new model instances to generate and initialize the counter example, otherwise hidden
				// state variables might prevent us from doing so if they somehow influence the state
				var replayModel = _createModel();
				var counterExampleModel = _createModel();
				_model.CopyFaultActivationStates(replayModel);
				_model.CopyFaultActivationStates(counterExampleModel);

				var replayInfo = replayModel.GenerateReplayInformation(trace, endsWithException);
				_context._counterExample = new CounterExample(counterExampleModel, trace, replayInfo, endsWithException);
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
			private readonly StateStack[] _stacks;
			private bool[] _awaitingWork;
			private ConcurrentQueue<int> _idleWorkers;

			private volatile bool _terminated;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			/// <param name="stacks"></param>
			public LoadBalancer(StateStack[] stacks)
			{
				_stacks = stacks;
				Reset();
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
				Assert.That(_stacks[workerIndex].FrameCount != 0, "Idle worker tries to assign work.");

				int idleWorker;
				if (!_idleWorkers.TryDequeue(out idleWorker))
					return true;

				// At this point we've got an idle worker that we can assign work to
				Assert.That(_stacks[idleWorker].FrameCount == 0, "Trying to assign work to non-idle worker.");
				Assert.That(workerIndex != idleWorker, "Worker tries to assign work to itself.");

				// If the worker actually got some new work, notify it, otherwise continue waiting
				if (_stacks[workerIndex].SplitWork(_stacks[idleWorker]))
				{
					Assert.That(_stacks[idleWorker].FrameCount != 0, "No work was assigned to non-idle worker.");
					Volatile.Write(ref _awaitingWork[idleWorker], false);
				}
				else
				{
					Assert.That(_stacks[idleWorker].FrameCount == 0, "Unexpected work assigned to idle worker.");
					_idleWorkers.Enqueue(idleWorker);
				}

				return true;
			}

			/// <summary>
			///   Stalls the worker until work has been assigned to it or there is no more work.
			/// </summary>
			private bool AwaitWork(int workerIndex)
			{
				Assert.That(_stacks[workerIndex].FrameCount == 0, "Non-idle worker awaits work.");

				Volatile.Write(ref _awaitingWork[workerIndex], true);
				_idleWorkers.Enqueue(workerIndex);

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

			/// <summary>
			///   Resets the load balancer so that a new invariant check can be started.
			/// </summary>
			public void Reset()
			{
				_terminated = false;
				_idleWorkers = new ConcurrentQueue<int>();
				_awaitingWork = new bool[_stacks.Length];
			}
		}
	}
}