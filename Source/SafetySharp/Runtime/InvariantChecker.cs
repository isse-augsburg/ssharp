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
	using System.Collections.Concurrent;
	using System.Runtime.ExceptionServices;
	using System.Runtime.InteropServices;
	using System.Threading;
	using Analysis;
	using Analysis.FormulaVisitors;
	using Serialization;
	using Utilities;

	/// <summary>
	///   Checks whether an invariant holds for all states of a <see cref="RuntimeModel" /> instance.
	/// </summary>
	internal unsafe class InvariantChecker : DisposableObject
	{
		private const int ReportStateCountDelta = 200000;

		private readonly LoadBalancer _loadBalancer;
		private readonly RuntimeModel _model;
		private readonly Action<string> _output;
		private readonly StateStorage _states;
		private readonly Thread[] _threads;
		private readonly Worker[] _workers;
		private CounterExample _counterExample;
		private Exception _exception;
		private int _levelCount;
		private int _nextReport = ReportStateCountDelta;
		private int _stateCount;
		private long _transitionCount;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="capacity">The number of states that can be stored.</param>
		/// <param name="cpuCount">The number of CPUs that should be used.</param>
		/// <param name="enableFaultOptimization">Indicates whether S#'s fault optimization technique should be used.</param>
		internal InvariantChecker(Model model, Formula invariant, Action<string> output, int capacity, int cpuCount, bool enableFaultOptimization)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(invariant, nameof(invariant));
			Requires.NotNull(output, nameof(output));
			Requires.InRange(capacity, nameof(capacity), 1024, Int32.MaxValue);

			var serializedModel = RuntimeModelSerializer.Save(model, 0, invariant);

			_model = RuntimeModelSerializer.Load(serializedModel);
			_states = new StateStorage(_model.StateVectorLayout, capacity, enableFaultOptimization);
			_output = output;

			cpuCount = Math.Min(Environment.ProcessorCount, Math.Max(1, cpuCount));
			var stacks = new StateStack[cpuCount];

			_workers = new Worker[cpuCount];
			_threads = new Thread[cpuCount];

			for (var i = 0; i < cpuCount; ++i)
			{
				stacks[i] = new StateStack(capacity);
				_workers[i] = new Worker(i, this, stacks[i], i == 0 ? _model : RuntimeModelSerializer.Load(serializedModel));
				_threads[i] = new Thread(_workers[i].Check) { IsBackground = true, Name = $"Worker {i}" };
			}

			_loadBalancer = new LoadBalancer(stacks);

#if false
			Console.WriteLine(_model.StateVectorLayout);
#endif
		}

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		internal AnalysisResult Check()
		{
			_output($"Performing invariant check with {_workers.Length} CPU cores.");
			_output($"State vector has {_model.StateVectorSize} bytes.");

			_workers[0].ComputeInitialStates();

			foreach (var thread in _threads)
				thread.Start();

			foreach (var thread in _threads)
				thread.Join();

			if (_exception != null)
				ExceptionDispatchInfo.Capture(_exception).Throw();

			Report();

			if (_counterExample != null)
				_output("Invariant violation detected.");

			return new AnalysisResult(_counterExample, _stateCount, _transitionCount, _levelCount);
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
			_model.SafeDispose();
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
			private readonly Func<bool> _invariant;
			private readonly RuntimeModel _model;
			private readonly StateStorage _states;
			private readonly StateStack _stateStack;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			public Worker(int index, InvariantChecker context, StateStack stateStack, RuntimeModel model)
			{
				_index = index;

				_context = context;
				_states = _context._states;
				_model = model;
				_invariant = CompilationVisitor.Compile(_model.Formulas[0]);
				_stateStack = stateStack;
			}

			/// <summary>
			///   Computes the model's initial states.
			/// </summary>
			public void ComputeInitialStates()
			{
				AddStates(_model.ComputeInitialStates());
			}

			/// <summary>
			///   Checks whether the model's invariant holds for all states.
			/// </summary>
			public void Check()
			{
				try
				{
					while (_context._loadBalancer.LoadBalance(_index))
					{
						int state;
						if (!_stateStack.TryGetState(out state))
							continue;

						AddStates(_model.ComputeSuccessorStates(_states[state]));

						InterlockedExchangeIfGreaterThan(ref _context._levelCount, _stateStack.FrameCount, _stateStack.FrameCount);
						if (InterlockedExchangeIfGreaterThan(ref _context._nextReport, _context._stateCount, _context._nextReport + ReportStateCountDelta))
							_context.Report();
					}
				}
				catch (Exception e)
				{
					_context._loadBalancer.Terminate();
					_context._exception = e;
				}
			}

			/// <summary>
			///   Adds the states stored in the <paramref name="stateCache" />.
			/// </summary>
			private void AddStates(StateCache stateCache)
			{
				if (stateCache.StateCount == 0)
					throw new InvalidOperationException("Deadlock detected.");

				Interlocked.Add(ref _context._transitionCount, stateCache.StateCount);
				_stateStack.PushFrame();

				for (var i = 0; i < stateCache.StateCount; ++i)
				{
					int index;
					if (!_states.AddState(stateCache.StateMemory + i * stateCache.StateVectorSize, out index))
						continue;

					Interlocked.Increment(ref _context._stateCount);
					_stateStack.PushState(index);

					// Deserialize the state in order to check the invariant; this seems inefficient, but
					// other alternatives do not seem to perform any better
					_model.Deserialize(stateCache.StateMemory + i * stateCache.StateVectorSize);
					if (!_invariant())
					{
						_context._loadBalancer.Terminate();
						CreateCounterExample();
						return;
					}
				}
			}

			/// <summary>
			///   Creates a counter example for the current topmost state.
			/// </summary>
			private void CreateCounterExample()
			{
				var indexedTrace = _stateStack.GetTrace();
				var trace = new byte[indexedTrace.Length][];

				for (var i = 0; i < indexedTrace.Length; ++i)
				{
					trace[i] = new byte[_model.StateVectorSize];
					Marshal.Copy(new IntPtr((int*)_states[indexedTrace[i]]), trace[i], 0, trace[i].Length);
				}

				_context._counterExample = new CounterExample(_model, trace, _model.GenerateReplayInformation(trace));
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