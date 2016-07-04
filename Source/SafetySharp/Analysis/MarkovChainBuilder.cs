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
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;
	
	/// Collect _all_ formulas to be checked in advance such that all state propositions are available.
	/// Thus, the matrix has only to be created once. Add occurrence of exception as one state proposition.

	/// <summary>
	///   Checks whether an invariant holds for all states of a <see cref="MarkovChainBuilder" /> instance.
	/// </summary>
	internal unsafe class MarkovChainBuilder : DisposableObject
	{
		private const int ReportStateCountDelta = 200000;
		private readonly LoadBalancer _loadBalancer;
		private readonly Action<string> _output;
		private readonly StateStorage _states;
		private readonly Worker[] _workers;
		private long _computedTransitionCount;
		private int _levelCount;
		private int _nextReport = ReportStateCountDelta;
		private readonly bool _progressOnly;
		private int _stateCount;
		private long _transitionCount;

		// TODO-Probabilistic: Implement a way to early escape the matrix building process
		// Sometimes not the full model has to be built, e.g. only reachability of several states
		// is requested. Then, matrix building may terminate after every path where the formula is
		// reached. When several formulas are requested in one go to early escape a path every
		// formula must be reached in this path

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal MarkovChainBuilder(Func<RuntimeModel> createModel, Action<string> output, AnalysisConfiguration configuration)
		{
			// TODO-Probabilistic: For now just support one CPU because merging results is not implemented, yet.
			configuration.CpuCount = 1;


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
					_workers[index] = new Worker(index, this, stacks[index], createModel(), configuration.SuccessorCapacity, configuration.StateCapacity);
				});
			}

			Task.WaitAll(tasks);

			_states = new StateStorage(_workers[0].StateVectorLayout, configuration.StateCapacity);

#if false
			Console.WriteLine(_model.StateVectorLayout);
#endif
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
			_computedTransitionCount = 0;
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
		///   Create probability matrix by visiting every state.
		/// </summary>
		internal MarkovChain CreateMarkovChain()
		{
			Reset();

			if (!_progressOnly)
			{
				_output($"Performing creation of probability matrix with {_workers.Length} CPU cores.");
				_output($"State vector has {_workers[0].StateVectorLayout.SizeInBytes} bytes.");
			}

			_workers[0].ComputeInitialStates();

			var tasks = new Task[_workers.Length];
			for (var i = 0; i < _workers.Length; ++i)
				tasks[i] = Task.Factory.StartNew(_workers[i].CreateMarkovChain);

			Task.WaitAll(tasks);

			if (!_progressOnly)
				Report();


			// TODO-Probabilistic: For now just support one CPU because merging results is not implemented, yet.

			return _workers[0].MarkovChain;
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
			private readonly MarkovChainBuilder _context;
			private readonly int _index;
			private readonly RuntimeModel _model;
			private readonly StateStack _stateStack;
			private readonly TransitionSet _transitions;
			public MarkovChain MarkovChain { get; }
			private StateStorage _states;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			public Worker(int index, MarkovChainBuilder context, StateStack stateStack, RuntimeModel model, int successorCapacity,int stateCapacity)
			{
				_index = index;

				_context = context;
				_model = model;
				_model.EffectlessFaultsMinimizationMode=EffectlessFaultsMinimizationMode.DontActivateEffectlessTransientFaults;
				_stateStack = stateStack;

				var stateFormulaLabels =
					_model.StateFormulas.Select(stateformula => stateformula.Label).ToArray();

				var stateRewardRetrieverLabels =
					_model.Rewards.Select(rewardRetriever => rewardRetriever.Label).ToArray();

				MarkovChain = new MarkovChain(stateCapacity)
				{
					StateFormulaLabels = stateFormulaLabels,
					StateRewardRetrieverLabels = stateRewardRetrieverLabels
				};

				//add debugger
				Func<Func<bool>, Func<bool>> addDebugger = (func) =>
				{
					return () =>
					{
						
						var result = func();
						return result;
					};
				};

				var compiledStateFormulas =
					_model.StateFormulas.Select(CompilationVisitor.Compile)
					//.Select(addDebugger)
					.ToArray();
				
				_transitions = new TransitionSet(model, successorCapacity, compiledStateFormulas, _model.Rewards);
				_transitions.TransitionMinimizationMode=TransitionMinimizationMode.Disable;
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
				_states = _context._states;

				_model.PrepareNextState();
				var newPathAvailable = true;

				while (newPathAvailable)
				{
					try
					{
						newPathAvailable = _model.ComputeNextInitialState(_transitions);
					}
					catch (Exception)
					{
						MarkovChain.AddInitialException(_model.GetProbability());
					}
				}

				AddStates(null);
			}

			/// <summary>
			///   Create probability matrix by visiting every state.
			/// </summary>
			internal void CreateMarkovChain()
			{
				_states = _context._states;

				while (_context._loadBalancer.LoadBalance(_index))
				{
					int state;
					if (!_stateStack.TryGetState(out state))
						continue;
					_transitions.Clear();

					_model.PrepareNextState();
					var newPathAvailable = true;

					MarkovChain.SetSourceStateOfUpcomingTransitions(state);
					while (newPathAvailable)
					{
						try
						{
							newPathAvailable = _model.ComputeNextSuccessorState(_transitions, _states[state]);
						}
						catch (Exception)
						{
							MarkovChain.AddTransitionException(_model.GetProbability());
						}
					}
					AddStates(state);
					MarkovChain.FinishSourceState();

					InterlockedExchangeIfGreaterThan(ref _context._levelCount, _stateStack.FrameCount, _stateStack.FrameCount);
					if (InterlockedExchangeIfGreaterThan(ref _context._nextReport, _context._stateCount, _context._nextReport + ReportStateCountDelta))
						_context.Report();
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
			private void AddStates(int? sourceState)
			{
				if (_transitions.ComputedTransitionCount == 0)
					throw new InvalidOperationException("Deadlock detected.");
				
				var transitionCount = 0;
				_stateStack.PushFrame();

				var transitionEnumerator = _transitions.GetResetedEnumerator();

				while (transitionEnumerator.MoveNext())
				{
					var transition = transitionEnumerator.Current;

					// Store the state if it hasn't been discovered before
					int index;
					if (_states.AddState(transition.TargetState, out index))
					{
						Interlocked.Increment(ref _context._stateCount);
						_stateStack.PushState(index);
					}
					if (sourceState == null)
					{
						// Add initial state to probability matrix
						MarkovChain.AddInitialState(index,transition.Probability);
					}
					else
					{
						// Add transition to probability matrix
						MarkovChain.AddTransition(index, transition.Probability);
					}

					// TODO-Probabilistic: why adding again and again -> save in StateStorage and remove here
					AssertOldEntryMatchesNewEntry(index,ref transition);

					MarkovChain.StateLabeling[index] = transition.Formulas;
					MarkovChain.StateRewards0[index] = transition.Reward0;
					MarkovChain.StateRewards1[index] = transition.Reward1;

					++transitionCount;
				}
				//ProbabilityMatrix.ValidateState(sourceState);

				Interlocked.Add(ref _context._transitionCount, transitionCount);
				Interlocked.Add(ref _context._computedTransitionCount, _transitions.ComputedTransitionCount);
			}

			[Conditional("DEBUG")]
			private void AssertOldEntryMatchesNewEntry(int index,ref TransitionSet.Transition transition)
			{
				// For debugging: Assure that if the index already exists the existing entry is exactly transition.Formulas
				// TODO: This cannot be done anymore. Find another way
				/*
				if (MarkovChain.StateLabeling.ContainsKey(index))
				{
					if (!MarkovChain.StateLabeling[index].Equals(transition.Formulas))
					{
						throw new Exception("The labeling of a state is not consistent.");
					}
				}
				*/
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