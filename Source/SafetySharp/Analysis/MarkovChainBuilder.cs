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
		internal SparseProbabilityMatrix CreateProbabilityMatrix()
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
			private readonly MarkovChainBuilder _context;
			private readonly int _index;
			private readonly RuntimeModel _model;
			private readonly StateStack _stateStack;
			private readonly TransitionSet _transitions;
			public SparseProbabilityMatrix ProbabilityMatrix { get; }
			private StateStorage _states;

			/// <summary>
			///   Initializes a new instance.
			/// </summary>
			public Worker(int index, MarkovChainBuilder context, StateStack stateStack, RuntimeModel model, int successorCapacity)
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

				ProbabilityMatrix = new SparseProbabilityMatrix()
				{
					//TODO-Probabilistic: Labels : new List<StateFormula>(),
					OrdinaryTransitionGroups = new Dictionary<int, List<TupleStateProbability>>(),
					StatesLeadingToException = new List<TupleStateProbability>(),
					//States = _context._states,
					StateLabeling = new Dictionary<int, StateFormulaSet>(),
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
			internal void CreateProbabilityMatrix()
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
						ProbabilityMatrix.InitialStates.Add(new TupleStateProbability(index, transition.Probability));
					}
					else
					{
						// Add transition to probability matrix
						ProbabilityMatrix.AddTransition(sourceState.Value,index, transition.Probability);
					}

					// TODO-Probabilistic: why adding again and again -> save in StateStorage and remove here
					AssertOldEntryMatchesNewEntry(index,ref transition);

					ProbabilityMatrix.StateLabeling[index] = transition.Formulas;
					ProbabilityMatrix.StateRewards[index] = new [] {transition.Reward0,transition.Reward1};

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
				if (ProbabilityMatrix.StateLabeling.ContainsKey(index))
				{
					if (!ProbabilityMatrix.StateLabeling[index].Equals(transition.Formulas))
					{
						throw new Exception("The labeling of a state is not consistent.");
					}
				}
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

	internal class MarkovChain
	{
		public bool MarkovChainComplete { get; private set; }

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;

		public SparseDoubleMatrix _matrix { get; }
		
		public DoubleVector InitialStateProbabilities = new DoubleVector();

		public Dictionary<int, StateFormulaSet> StateLabeling = new Dictionary<int, StateFormulaSet>();

		public Dictionary<int, Reward[]> StateRewards = new Dictionary<int, Reward[]>();

		public MarkovChain()
		{
			_matrix=new SparseDoubleMatrix(1<<21, 1 << 23);
			// Add State for exception
			_matrix.SetRow(0); //add state for exception
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(0,1.0)); //Add self-loop in exception
		}
		// Retrieving matrix phase

		public int States => _matrix.Rows();

		public int ExceptionState => 0;


		// Creating matrix phase

		public void AddInitialState(int state, Probability probability)
		{
			InitialStateProbabilities[state]= InitialStateProbabilities[state]+probability.Value;
		}

		public void AddInitialException(Probability probability)
		{
			AddInitialState(ExceptionState,probability);
		}

		public void SetSourceStateOfTransition(int state)
		{
			_matrix.SetRow(state);
		}

		public void AddTransition(int state, Probability probability)
		{
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(state,probability.Value));
		}

		public void AddTransitionException(Probability probability)
		{
			AddTransition(ExceptionState, probability);
		}

		public void SealProbabilityMatrix()
		{
			InitialStateProbabilities.IncreaseSize(States);
			_matrix.OptimizeAndSeal();
		}

		// Validation

		[Conditional("DEBUG")]
		public void ValidateStates()
		{
			var enumerator = _matrix.GetEnumerator();

			while (enumerator.MoveNextRow())
			{
				// for each state there is a row. The sum of all columns in a row should be 1.0
				var probability = 0.0;
				while (enumerator.MoveNextColumn())
				{
					if (enumerator.CurrentColumnValue != null)
						probability += enumerator.CurrentColumnValue.Value.Value;
					else
						throw new Exception("Entry must not be null");
				}
				if (Probability.IsOne(probability, 0.000000001))
					throw new Exception("Probabilities should sum up to 1");
			}
		}


		[Conditional("DEBUG")]
		internal void PrintPathWithStepwiseHighestProbability(int steps)
		{
			var enumerator = _matrix.GetEnumerator();
			Func<int,SparseDoubleMatrix.ColumnValue> selectRowEntryWithHighestProbability =
				row =>
				{
					enumerator.MoveRow(row);
					enumerator.MoveNextColumn();
					var candidate = enumerator.CurrentColumnValue.Value;
					while (enumerator.MoveNextColumn())
						if (candidate.Value < enumerator.CurrentColumnValue.Value.Value)
							candidate = enumerator.CurrentColumnValue.Value;
					return candidate;
				};
			Action<int,double> printStateAndProbability =
				(state,probability) =>
				{
					Console.Write($"step: {probability.ToString(CultureInfo.InvariantCulture)} {state}");
					for (var i = 0; i < StateFormulaLabels.Length; i++)
					{
						var label = StateFormulaLabels[i];
						Console.Write(" " + label + "=");
						if (StateLabeling[state][i])
							Console.Write("true");
						else
							Console.Write("false");
					}
					for (var i = 0; i < StateRewardRetrieverLabels.Length; i++)
					{
						var label = StateRewardRetrieverLabels[i];
						Console.Write(" " + label + "=");
						Console.Write("TODO");
					}
					Console.WriteLine();
				};

			var initialStateWithHighestProbability = 0;
			var probabilityOfInitialStateWithHighestProbability = 0.0;
			for (var i = 0; i < States; i++)
			{
				if (InitialStateProbabilities[i] > probabilityOfInitialStateWithHighestProbability)
				{
					probabilityOfInitialStateWithHighestProbability = InitialStateProbabilities[i];
					initialStateWithHighestProbability = i;
				}
			}
			printStateAndProbability(initialStateWithHighestProbability, probabilityOfInitialStateWithHighestProbability);

			var lastState = initialStateWithHighestProbability;
			for (var i = 0; i < steps; i++)
			{
				var currentTuple = selectRowEntryWithHighestProbability(lastState);
				printStateAndProbability(currentTuple.Column, currentTuple.Value);
				lastState = currentTuple.Column;
			}
		}
	}

	internal class CompactProbabilityMatrix
	{
		//Note: We use index origin=1
		public int States;
		public List<TupleStateProbability> InitialStates=new List<TupleStateProbability>();
		public Dictionary<int, List<TupleStateProbability>> TransitionGroups = new Dictionary<int, List<TupleStateProbability>>();
		
		public string[] StateFormulaLabels;
		public string[] StateRewardRetrieverLabels;

		public int NumberOfTransitions;
		public Dictionary<int, StateFormulaSet> StateLabeling = new Dictionary<int, StateFormulaSet>();
		public Dictionary<int, Reward[]> StateRewards = new Dictionary<int, Reward[]>();


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

		[Conditional("DEBUG")]
		public void ValidateStates()
		{
			foreach (var transitionGroup in TransitionGroups)
			{
				var probability = Probability.Zero;
				foreach (var tupleStateProbability in transitionGroup.Value)
				{
					probability += tupleStateProbability.Probability;
				}
				if (!probability.Is(1.0,0.000000001))
				{
					Debugger.Break();
					throw new Exception("Probabilities should sum up to 1");
				}

			}
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
		public string[] StateFormulaLabels;
		public string[] StateRewardRetrieverLabels;
		public Dictionary<int,StateFormulaSet> StateLabeling;
		public Dictionary<int, Reward[]> StateRewards = new Dictionary<int, Reward[]>();

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

			// State Labeling
			Func<bool> returnFalse = () => false;
			var allFalseStateFormulaSet = new Func<bool>[StateFormulaLabels.Length];
			for (int i = 0; i < StateFormulaLabels.Length; i++)
			{
				allFalseStateFormulaSet[i] = returnFalse;
			}
			compactProbabilityMatrix.StateFormulaLabels = StateFormulaLabels;
			compactProbabilityMatrix.AddTransition(exceptionState, exceptionState, Probability.One);
			compactProbabilityMatrix.StateLabeling.Add(exceptionState, new StateFormulaSet(allFalseStateFormulaSet));

			// State Rewards
			foreach (var stateRewards in StateRewards)
			{
				var compactId = CreateCompactId(stateRewards.Key);
				compactProbabilityMatrix.StateRewards.Add(compactId, stateRewards.Value);
			}
			var noRewards = new Reward[StateRewardRetrieverLabels.Length];
			for (int i = 0; i < StateRewardRetrieverLabels.Length; i++)
			{
				noRewards[i] = compactProbabilityMatrix.StateRewards[1][i]; //use available rewards of any state
				noRewards[i].Reset();
			}
			compactProbabilityMatrix.StateRewards.Add(exceptionState, noRewards);
			compactProbabilityMatrix.StateRewardRetrieverLabels = StateRewardRetrieverLabels;
			
			// return result
			return new Tuple<Dictionary<int, int>, CompactProbabilityMatrix>(compactToSparse, compactProbabilityMatrix);
		}

		internal void PrintPathWithStepwiseHighestProbability(int steps)
		{
			Func<List<TupleStateProbability>,TupleStateProbability> selectTupleWithHighestProbability =
				elements =>
				{
					var enumerator = elements.GetEnumerator();
					enumerator.MoveNext();
					var candidate = enumerator.Current;
					while(enumerator.MoveNext())
						if (candidate.Probability.Value < enumerator.Current.Probability.Value)
							candidate = enumerator.Current;
					return candidate;
				};
			Action<TupleStateProbability> printTuple =
				tuple =>
				{
					Console.Write($"step: {tuple.Probability.Value.ToString(CultureInfo.InvariantCulture)} {tuple.State}");
					for (var i = 0; i < StateFormulaLabels.Length; i++)
					{
						var label = StateFormulaLabels[i];
						Console.Write(" " + label + "=");
						if (StateLabeling[tuple.State][i])
							Console.Write("true");
						else
							Console.Write("false");
					}
					for (var i = 0; i < StateRewardRetrieverLabels.Length; i++)
					{
						var label = StateRewardRetrieverLabels[i];
						Console.Write(" " + label + "=");
						Console.Write("TODO");
					}
					Console.WriteLine();
				};
			var initialStepWithHighestProbability = selectTupleWithHighestProbability(InitialStates);
			printTuple(initialStepWithHighestProbability);
			var lastState = initialStepWithHighestProbability.State;
			for (var i = 0; i < steps; i++)
			{
				var currentTuple = selectTupleWithHighestProbability(OrdinaryTransitionGroups[lastState]);
				printTuple(currentTuple);
				lastState = currentTuple.State;
			}
		}

		[Conditional("DEBUG")]
		public void ValidateState(int? sourceState)
		{
			if (sourceState.HasValue)
			{
				var transitionGroup = OrdinaryTransitionGroups[sourceState.Value];
				var probability = Probability.Zero;
				foreach (var tupleStateProbability in transitionGroup)
				{
					probability += tupleStateProbability.Probability;
				}
				if (!probability.Is(1.0, 0.000000001))
				{
					Debugger.Break();
					throw new Exception("Probabilities should sum up to 1");
				}
			}
			else
			{
				var probability = Probability.Zero;
				foreach (var tupleStateProbability in InitialStates)
				{
					probability += tupleStateProbability.Probability;
				}
				if (!probability.Is(1.0, 0.000000001))
				{
					Debugger.Break();
					throw new Exception("Probabilities should sum up to 1");
				}
			}
		}
	}

}