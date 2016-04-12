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
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Threading.Tasks;
	using FormulaVisitors;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;
	
	/// Collect _all_ formulas to be checked in advance such that all state propositions are available.
	/// Thus, the matrix has only to be created once. Add occurrence of exception as one state proposition.

	/// <summary>
	///   Checks whether an invariant holds for all states of a <see cref="ProbabilityMatrixBuilder" /> instance.
	/// </summary>
	internal unsafe class ProbabilityMatrixBuilder : DisposableObject
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
		/// <param name="createModel">Creates model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal ProbabilityMatrixBuilder(Func<RuntimeModel> createModel, Action<string> output, AnalysisConfiguration configuration)
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
		///   Create probability matrix by visiting every state.
		/// </summary>
		internal SparseProbabilityMatrix CreateProbabilityMatrix()
		{
			if (!_progressOnly)
			{
				_output($"Performing creation of probability matrix with {_workers.Length} CPU cores.");
				_output($"State vector has {_workers[0].StateVectorLayout.SizeInBytes} bytes.");
			}

			_workers[0].ComputeInitialStates();

			var tasks = new Task[_workers.Length];
			for (var i = 0; i < _workers.Length; ++i)
				tasks[i] = Task.Factory.StartNew(_workers[i].CreateProbabilityMatrix);

			Task.WaitAll(tasks);

			if (!_progressOnly)
				Report();


			// TODO-Probabilistic: For now just support one CPU because merging results is not implemented, yet.

			return _workers[0].ProbabilityMatrix;
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
			private readonly ProbabilityMatrixBuilder _context;
			private readonly int _index;
			private readonly RuntimeModel _model;
			private readonly StateStack _stateStack;
			private readonly TransitionSet _transitions;
			public SparseProbabilityMatrix ProbabilityMatrix { get; }
			private StateStorage _states;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			public Worker(int index, ProbabilityMatrixBuilder context, StateStack stateStack, RuntimeModel model, int successorCapacity)
			{
				_index = index;

				_context = context;
				_model = model;
				_stateStack = stateStack;

				var stateFormulaLabels =
					_model.StateFormulas.Select(stateformula => stateformula.Label).ToArray();

				ProbabilityMatrix = new SparseProbabilityMatrix()
				{
					//TODO-Probabilistic: Labels : new List<StateFormula>(),
					OrdinaryTransitionGroups = new Dictionary<int, List<TupleStateProbability>>(),
					StatesLeadingToException = new List<TupleStateProbability>(),
					//States = _context._states,
					StateLabeling = new Dictionary<int, StateFormulaSet>(),
					NoOfStateFormulaLabels = _model.StateFormulas.Length,
					StateFormulaLabels = stateFormulaLabels
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
				
				_transitions = new TransitionSet(model, successorCapacity, compiledStateFormulas);
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
				ProbabilityMatrix.InitialStates=new List<TupleStateProbability>();
				ProbabilityMatrix.InitialExceptionProbability = Probability.Zero;
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
						ProbabilityMatrix.InitialExceptionProbability += _model.GetProbability();
					}
				}

				AddStates(null);
			}

			/// <summary>
			///   Create probability matrix by visiting every state.
			/// </summary>
			public void CreateProbabilityMatrix()
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
					while (newPathAvailable)
					{
						try
						{
							newPathAvailable = _model.ComputeNextSuccessorState(_transitions, _states[state]);
						}
						catch (Exception)
						{
							ProbabilityMatrix.StatesLeadingToException.Add(new TupleStateProbability(state,_model.GetProbability()));
						}
					}
					AddStates(state);

					InterlockedExchangeIfGreaterThan(ref _context._levelCount, _stateStack.FrameCount, _stateStack.FrameCount);
					if (InterlockedExchangeIfGreaterThan(ref _context._nextReport, _context._stateCount, _context._nextReport + ReportStateCountDelta))
						_context.Report();
				}
				
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

				var transitionEnumerator = _transitions.GetResettedEnumerator();

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
						ProbabilityMatrix.InitialStates.Add(new TupleStateProbability(index, transition.Probability));
					}
					else
					{
						// Add transition to probability matrix
						ProbabilityMatrix.AddTransition(sourceState.Value,index, transition.Probability);
					}

					// TODO-Probabilistic: why adding again and again -> save in StateStorage and remove here
					// TODO for debugging: Assure that if the index already exists the existing entry is exactly transition.Formulas
					ProbabilityMatrix.StateLabeling[index] = transition.Formulas;

					++transitionCount;
				}

				Interlocked.Add(ref _context._transitionCount, transitionCount);
				Interlocked.Add(ref _context._computedTransitionCount, _transitions.ComputedTransitionCount);
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

	struct TupleStateProbability
	{
		internal TupleStateProbability(int state,Probability probability)
		{
			State = state;
			Probability = probability;
		}
		// refer to the index in StateStorage, not the actual bytevector
		public int State;
		public Probability Probability;
	}

	internal class CompactProbabilityMatrix
	{
		//Note: We use index origin=1
		public int States;
		public List<TupleStateProbability> InitialStates=new List<TupleStateProbability>();
		public Dictionary<int, List<TupleStateProbability>> TransitionGroups = new Dictionary<int, List<TupleStateProbability>>();

		public int NoOfStateFormulaLabels;
		public string[] StateFormulaLabels;

		public int NumberOfTransitions;
		public Dictionary<int, StateFormulaSet> StateLabeling=new Dictionary<int, StateFormulaSet>();


		public void AddTransition(int sourceState, int targetState, Probability probability)
		{
			List<TupleStateProbability> listOfState = null;
			if (TransitionGroups.ContainsKey(sourceState))
			{
				listOfState = TransitionGroups[sourceState];
			}
			else
			{
				listOfState = new List<TupleStateProbability>();
				TransitionGroups.Add(sourceState, listOfState);
			}
			NumberOfTransitions++;
			listOfState.Add(new TupleStateProbability(targetState, probability));
		}
	}

	internal class SparseProbabilityMatrix
	{
		// Sparse in the sense that not all state numbers are used due to state hashing.
		// For example state 233923923 and 30303 are used, but nothing in between.

		public List<TupleStateProbability> InitialStates;
		public Probability? InitialExceptionProbability=null;
		public Dictionary<int,List<TupleStateProbability>> OrdinaryTransitionGroups;
		public List<TupleStateProbability> StatesLeadingToException;
		//public int NumberOfStates;
		public int NumberOfTransitions;
		// TODO-Probabilistic: Exception gets StateNumber MaxState+1

		//TODO-Probabilistic: why reevaluating again and again -> save in StateStorage and remove here
		public int NoOfStateFormulaLabels;
		public string[] StateFormulaLabels;
		public Dictionary<int,StateFormulaSet> StateLabeling;

		public void AddTransition(int sourceState, int targetState, Probability probability)
		{
			List<TupleStateProbability> listOfState = null;
			if (OrdinaryTransitionGroups.ContainsKey(sourceState))
			{
				listOfState = OrdinaryTransitionGroups[sourceState];
			}
			else
			{
				listOfState = new List<TupleStateProbability>();
				OrdinaryTransitionGroups.Add(sourceState, listOfState);
			}
			NumberOfTransitions++;
			listOfState.Add(new TupleStateProbability(targetState,probability));
		}

		public Tuple<Dictionary<int,int>,CompactProbabilityMatrix> DeriveCompactProbabilityMatrix()
		{
			var compactToSparse = new Dictionary<int,int>(); //for counterexamples
			var sparseToCompact = new Dictionary<int, int>();
			var compactProbabilityMatrix = new CompactProbabilityMatrix();
			Func<int, int> CreateCompactId = (sparseId) =>
			{
				if (sparseToCompact.ContainsKey(sparseId))
				{
					return sparseToCompact[sparseId];
				}
				else
				{
					var compactId = ++compactProbabilityMatrix.States;
					sparseToCompact.Add(sparseId,compactId);
					compactToSparse.Add(compactId,sparseId);
					return compactId;
				}
			};

			foreach (var tupleSparseStateProbability in InitialStates)
			{
				var compactId = CreateCompactId(tupleSparseStateProbability.State);
				compactProbabilityMatrix.InitialStates.Add(new TupleStateProbability(compactId, tupleSparseStateProbability.Probability));
			}

			// Faster: Convert it directly rather than using AddTransition
			foreach (var sparseTransitionList in OrdinaryTransitionGroups)
			{
				var compactSourceId = CreateCompactId(sparseTransitionList.Key);
				var listOfTargetStates = new List<TupleStateProbability>(sparseTransitionList.Value.Count);
				compactProbabilityMatrix.TransitionGroups.Add(compactSourceId,listOfTargetStates);
				foreach (var transition in sparseTransitionList.Value)
				{
					var compactTargetId = CreateCompactId(transition.State);
					listOfTargetStates.Add(new TupleStateProbability(compactTargetId, transition.Probability));
				}
			}
			compactProbabilityMatrix.NumberOfTransitions = NumberOfTransitions;


			// Exception gets StateNumber MaxState+1
			var exceptionState = ++compactProbabilityMatrix.States;
			// initial probability for an exception
			compactProbabilityMatrix.InitialStates.Add(new TupleStateProbability(exceptionState, InitialExceptionProbability.Value));
			// probability leading to an exception
			foreach (var tupleSparseStateProbability in StatesLeadingToException)
			{
				// targetState is state of exception
				var compactSourceId = CreateCompactId(tupleSparseStateProbability.State);
				compactProbabilityMatrix.AddTransition(compactSourceId, exceptionState, tupleSparseStateProbability.Probability);
			}

			foreach (var stateFormulaSet in StateLabeling)
			{
				var compactId = CreateCompactId(stateFormulaSet.Key);
				compactProbabilityMatrix.StateLabeling.Add(compactId,stateFormulaSet.Value);
			}
			compactProbabilityMatrix.NoOfStateFormulaLabels = NoOfStateFormulaLabels;
			// TODO: Add label for exception
			// todo: remove hack
			Func<bool> returnFalse = () => false;
			var allFalseStateFormulaSet = new Func<bool>[NoOfStateFormulaLabels];
			for (int i = 0; i < NoOfStateFormulaLabels; i++)
			{
				allFalseStateFormulaSet[i] = returnFalse;
			}
			compactProbabilityMatrix.StateFormulaLabels = StateFormulaLabels;
			compactProbabilityMatrix.AddTransition(exceptionState, exceptionState, Probability.One);
			compactProbabilityMatrix.StateLabeling.Add(exceptionState,new StateFormulaSet(allFalseStateFormulaSet));


			// return result
			return new Tuple<Dictionary<int, int>, CompactProbabilityMatrix>(compactToSparse, compactProbabilityMatrix);
		}
	}

}