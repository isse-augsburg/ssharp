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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.Runtime
{
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using Analysis;
	using Analysis.ModelChecking.Transitions;
	using Modeling;
	using Serialization;
	using Utilities;

	internal sealed unsafe class StateStorageStateToMarkovChainStateMapper
	{
		/// <summary>
		///   The number of states that can be indexed.
		/// </summary>
		private readonly int _capacity;

		/// <summary>
		///   The buffer that stores the states.
		/// </summary>
		private readonly MemoryBuffer _stateStorageToMarkovChainStateBuffer = new MemoryBuffer();
		
		private readonly int* _stateStorageToMarkovChainStateMemory;

		//[Impl(MethodImplOptions.AggressiveInlining)]
		public int this[int index]
		{
			get { return _stateStorageToMarkovChainStateMemory[index]; }
			set { _stateStorageToMarkovChainStateMemory[index]=value; }
		}

		public StateStorageStateToMarkovChainStateMapper(int capacity)
		{
			Requires.InRange(capacity, nameof(capacity), 1024, Int32.MaxValue);
			
			_capacity = capacity;

			_stateStorageToMarkovChainStateBuffer.Resize((long)_capacity * sizeof(int), zeroMemory: false);
			_stateStorageToMarkovChainStateMemory = (int*) _stateStorageToMarkovChainStateBuffer.Pointer;
			
			for (var i = 0; i < capacity; ++i)
				_stateStorageToMarkovChainStateMemory[i] = -1;
		}
	}

	internal unsafe class MarkovChain
	{
		public bool MarkovChainComplete { get; private set; }

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;

		public SparseDoubleMatrix ProbabilityMatrix { get; }

		public DoubleVector InitialStateProbabilities;

		public LabelVector StateLabeling;

		//TODO: Hardcoded. Remove
		public RewardVector StateRewards0;
		public RewardVector StateRewards1;

		/*Dictionary<int, int> compactToSparse = new Dictionary<int, int>();
		Dictionary<int, int> sparseToCompact = new Dictionary<int, int>();
		*/
		private StateStorageStateToMarkovChainStateMapper _stateMapper;

		public MarkovChain(int maxNumberOfStates= 1 << 21, int maxNumberOfTransitions=0)
		{
			if (maxNumberOfTransitions <= 0)
			{
				maxNumberOfTransitions = maxNumberOfStates << 3;
			}
			InitialStateProbabilities = new DoubleVector();
			StateLabeling = new LabelVector();
			StateRewards0 = new RewardVector();
			StateRewards1 = new RewardVector();
			_stateMapper = new StateStorageStateToMarkovChainStateMapper(maxNumberOfStates);
			ProbabilityMatrix = new SparseDoubleMatrix(maxNumberOfStates, maxNumberOfTransitions);
		}


		// Retrieving matrix phase

		public int States { get; private set; } = 0;

		public int Transitions { get; private set; } = 0;

		public int? ExceptionState { get; private set; } = null;

		public bool HasExceptionInModel => ExceptionState != -1;

		// Creating matrix phase

		/*
		private int GetOrCreateStateForException()
		{
			if (ExceptionState != null)
				return ExceptionState.Value;
			ExceptionState = States;
			ProbabilityMatrix.SetRow(ExceptionState.Value); //add state for exception
			ProbabilityMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(ExceptionState.Value, 1.0)); //Add self-loop in exception
			States++;
			return ExceptionState.Value;
		}
		*/

		private int GetMarkovChainState (int stateStorageState)
		{
			{
				if (_stateMapper[stateStorageState]!=-1)
				{
					return _stateMapper[stateStorageState];
				}
				else
				{
					var freshMarkovChainState = States;
					States++;
					_stateMapper[stateStorageState] = freshMarkovChainState;
					return freshMarkovChainState;
				}
			}
		}

		internal void AddInitialState(int stateStorageState, double probability)
		{
			var markovChainState = GetMarkovChainState(stateStorageState);
			InitialStateProbabilities[markovChainState] = InitialStateProbabilities[markovChainState] + probability;
		}

		/*
		internal void AddInitialException(Probability probability)
		{
			AddInitialState(GetOrCreateStateForException(), probability);
		}
		*/

		internal void SetSourceStateOfUpcomingTransitions(int stateStorageState)
		{
			var markovChainState = GetMarkovChainState(stateStorageState);
			ProbabilityMatrix.SetRow(markovChainState);
		}

		internal void AddTransition(int stateStorageState, double probability)
		{
			var markovChainState = GetMarkovChainState(stateStorageState);
			ProbabilityMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(markovChainState, probability));
			Transitions++;
		}

		/*
		private void AddTransitionException(Probability probability)
		{
			AddTransition(GetOrCreateStateForException(), probability);
		}
		*/

		internal void SetStateLabeling(int stateStorageState, StateFormulaSet formula)
		{
			var markovChainState = GetMarkovChainState(stateStorageState);
			StateLabeling[markovChainState] = formula;
		}

		internal void SetStateRewards0(int stateStorageState, Reward reward)
		{
			var markovChainState = GetMarkovChainState(stateStorageState);
			StateRewards0[markovChainState] = reward;
		}

		internal void SetStateRewards1(int stateStorageState, Reward reward)
		{
			var markovChainState = GetMarkovChainState(stateStorageState);
			StateRewards1[markovChainState] = reward;
		}

		internal void FinishSourceState()
		{
			ProbabilityMatrix.FinishRow();
		}


		/// <summary>
		///   Adds the <paramref name="state" /> and all of its <see cref="transitions" /> to the state graph.
		/// </summary>
		/// <param name="state">The state that should be added.</param>
		/// <param name="isInitial">Indicates whether the state is an initial state.</param>
		/// <param name="transitions">The transitions leaving the state.</param>
		/// <param name="transitionCount">The number of valid transitions leaving the state.</param>
		internal void AddStateInfo(int state, bool isInitial, TransitionCollection transitions, int transitionCount)
		{
			Assert.That(transitionCount > 0, "Cannot add deadlock state.");

			if (isInitial)
			{
				foreach (var transition in transitions)
				{
					var probTransition = (ProbabilisticTransition*)transition;
					Assert.That(probTransition->IsValid, "Attempted to add an invalid transition.");
					AddInitialState(transition->TargetState, probTransition->Probability);
					SetStateLabeling(transition->TargetState, transition->Formulas);
				}
			}
			else
			{
				SetSourceStateOfUpcomingTransitions(state);
				foreach (var transition in transitions)
				{
					var probTransition = (ProbabilisticTransition*)transition;
					Assert.That(probTransition->IsValid, "Attempted to add an invalid transition.");
					AddTransition(transition->TargetState, probTransition->Probability);

					//TODO: optimize and allow different target transitions
					SetStateLabeling(transition->TargetState, transition->Formulas);
					//SetStateRewards0(transition->TargetState, transition.Reward0);
					//SetStateRewards1(transition->TargetState, transition.Reward1);
				}
				FinishSourceState();
			}
		}

		public void SealProbabilityMatrix()
		{
			InitialStateProbabilities.IncreaseSize(States);
			ProbabilityMatrix.OptimizeAndSeal();
			MarkovChainComplete = true;
		}

		// Validation

		[Conditional("DEBUG")]
		public void ValidateStates()
		{
			if (ProbabilityMatrix.Rows != States)
			{
				throw new Exception("Number of states should be equal to the number of rows in the matrix");
			}
			//if (_matrix.ColumnEntries != Transitions)
			//{
			//	throw new Exception("Number of transitions should be equal to the number of ColumnEntries in the matrix");
			//}

			var enumerator = ProbabilityMatrix.GetEnumerator();

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
				if (!Probability.IsOne(probability, 0.000000001))
					throw new Exception("Probabilities should sum up to 1");
			}
		}


		[Conditional("DEBUG")]
		internal void PrintPathWithStepwiseHighestProbability(int steps)
		{
			var enumerator = ProbabilityMatrix.GetEnumerator();
			Func<int, SparseDoubleMatrix.ColumnValue> selectRowEntryWithHighestProbability =
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
			Action<int, double> printStateAndProbability =
				(state, probability) =>
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
		
		public Func<int, bool> CreateFormulaEvaluator(Analysis.Formula formula)
		{
			return Analysis.FormulaVisitors.MarkovChainFormulaEvaluatorCompilationVisitor.Compile(this,formula);
		}

		public UnderlyingDigraph CreateUnderlyingDigraph()
		{
			if (MarkovChainComplete)
				return new UnderlyingDigraph(this);
			return null;
		}

		internal class UnderlyingDigraph
		{
			public BidirectionalGraph Graph { get; private set; }

			public UnderlyingDigraph(MarkovChain markovChain)
			{
				//Assumption "every node is reachable" is fulfilled due to the construction
				Graph = new BidirectionalGraph();

				var enumerator = markovChain.ProbabilityMatrix.GetEnumerator();
				while (enumerator.MoveNextRow())
				{
					var sourceState = enumerator.CurrentRow;
					while (enumerator.MoveNextColumn())
					{
						if (enumerator.CurrentColumnValue != null)
						{
							var value = enumerator.CurrentColumnValue.Value;
							if (value.Value>0.0)
								Graph.AddVerticesAndEdge(new BidirectionalGraph.Edge(sourceState, value.Column));
						}
						else
							throw new Exception("Entry must not be null");
					}
				}
			}

			public Dictionary<int, bool> GetAncestors(Dictionary<int, bool> toNodes, Dictionary<int, bool> nodesToIgnore)
			{
				// based on DFS https://en.wikipedia.org/wiki/Depth-first_search
				var nodesAdded = new Dictionary<int,bool>();
				var nodesToTraverse = new Stack<int>();
				foreach (var node in toNodes)
				{
					nodesToTraverse.Push(node.Key);
				}

				while (nodesToTraverse.Count > 0)
				{
					var currentNode = nodesToTraverse.Pop();
					var isIgnored = nodesToIgnore.ContainsKey(currentNode);
					var alreadyDiscovered = nodesAdded.ContainsKey(currentNode);
					if (!(isIgnored || alreadyDiscovered))
					{
						nodesAdded.Add(currentNode,true);
						foreach (var inEdge in Graph.InEdges(currentNode))
						{
							nodesToTraverse.Push(inEdge.Source);
						}
					}
				}
				return nodesAdded;
			}
		}
	}
}
