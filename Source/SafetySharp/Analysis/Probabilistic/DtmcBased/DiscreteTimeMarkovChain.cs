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
	using System.Collections;
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using Analysis;
	using Analysis.ModelChecking.Transitions;
	using Modeling;
	using Serialization;
	using Utilities;
	using SafetySharp.Utilities.Graph;


	public class DiscreteTimeMarkovChain : IFormalismWithStateLabeling
	{
		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"


		public string[] StateFormulaLabels { get; set; }

		public string[] StateRewardRetrieverLabels;

		internal SparseDoubleMatrix ProbabilityMatrix { get; }
		
		public LabelVector StateLabeling { get; }
		
		public DiscreteTimeMarkovChain(int maxNumberOfStates= 1 << 21, int maxNumberOfTransitions=0)
		{
			if (maxNumberOfTransitions <= 0)
			{
				maxNumberOfTransitions = maxNumberOfStates << 12;
				var limit = 5 * 1024 / 16 * 1024 * 1024; // 5 gb / 16 bytes (for entries)

				if (maxNumberOfTransitions < maxNumberOfStates || maxNumberOfTransitions > limit)
					maxNumberOfTransitions = limit;
			}
			
			StateLabeling = new LabelVector();
			ProbabilityMatrix = new SparseDoubleMatrix(maxNumberOfStates+1, maxNumberOfTransitions); // one additional row for row of initial distribution
		}


		// Retrieving matrix phase

		public int States => ProbabilityMatrix.Rows-1; //Note: returns 0 if initial distribution added and -1 if nothing was added. So check for 0 is not enough

		public int Transitions { get; private set; } = 0; //without entries of initial distribution

		public int RowOfInitialStates = 0;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int StateToRow(int state) => state+1;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int StateToColumn(int state) => state; //Do nothing! Just here to make the algorithms more clear.

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int ColumnToState(int state) => state; //Do nothing! Just here to make the algorithms more clear.


		// Creating matrix phase
		// For the initial distribution the process is
		//    StartWithInitialDistribution()
		//    while(transitions to add exist) {
		//	      AddInitialTransition();
		//    }
		//    FinishInitialDistribution()
		internal void StartWithInitialDistribution()
		{
			ProbabilityMatrix.SetRow(RowOfInitialStates);
		}

		internal void AddInitialTransition(int markovChainState, double probability)
		{
			// initial state probabilities are also saved in the ProbabilityMatrix
			ProbabilityMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(StateToColumn(markovChainState), probability));
		}

		internal void FinishInitialDistribution()
		{
			ProbabilityMatrix.FinishRow();
		}

		// For distribution of a state the process is
		//    StartWithNewDistribution(markovChainSourceState)
		//    while(transitions to add exist) {
		//	      AddTransition();
		//    }
		//    FinishDistribution()
		internal void StartWithNewDistribution(int markovChainSourceState)
		{
			ProbabilityMatrix.SetRow(StateToRow(markovChainSourceState));
		}

		internal void AddTransition(int markovChainState, double probability)
		{
			ProbabilityMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(StateToColumn(markovChainState), probability));
			Transitions++;
		}

		internal void FinishDistribution()
		{
			ProbabilityMatrix.FinishRow();
		}

		internal void SetStateLabeling(int markovChainState, StateFormulaSet formula)
		{
			StateLabeling[markovChainState] = formula;
		}

		public void SealProbabilityMatrix()
		{
			ProbabilityMatrix.OptimizeAndSeal();
		}

		// Validation

		[Conditional("DEBUG")]
		public void ValidateStates()
		{
			var enumerator = ProbabilityMatrix.GetEnumerator();

			//every row contains one probability distribution (also the initial distribution)
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
			var enumerator = GetEnumerator();
			Func<SparseDoubleMatrix.ColumnValue> selectRowEntryWithHighestProbability =
				() =>
				{
					enumerator.MoveNextTransition();
					var candidate = enumerator.CurrentTransition;
					while (enumerator.MoveNextTransition())
						if (candidate.Value < enumerator.CurrentTransition.Value)
							candidate = enumerator.CurrentTransition;
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


			enumerator.SelectInitialDistribution();
			var currentTuple = selectRowEntryWithHighestProbability();
			printStateAndProbability(ColumnToState(currentTuple.Column), currentTuple.Value);
			var lastState = ColumnToState(currentTuple.Column);
			
			for (var i = 0; i < steps; i++)
			{
				enumerator.SelectSourceState(lastState);
				currentTuple = selectRowEntryWithHighestProbability();
				printStateAndProbability(ColumnToState(currentTuple.Column), currentTuple.Value);
				lastState = ColumnToState(currentTuple.Column);
			}
		}
		
		public Func<int, bool> CreateFormulaEvaluator(Analysis.Formula formula)
		{
			return Analysis.FormulaVisitors.FormulaEvaluatorCompilationVisitor.Compile(this,formula);
		}

		internal UnderlyingDigraph CreateUnderlyingDigraph()
		{
			return new UnderlyingDigraph(this);
		}
		
		internal class UnderlyingDigraph
		{
			public BidirectionalGraphDirectNodeAccess BaseGraph { get; private set; }

			public UnderlyingDigraph(DiscreteTimeMarkovChain markovChain)
			{
				//Assumption "every node is reachable" is fulfilled due to the construction
				var newGraph = new BidirectionalGraph();
				BaseGraph = newGraph;

				var enumerator = markovChain.GetEnumerator();
				while (enumerator.MoveNextState())
				{
					while (enumerator.MoveNextTransition())
					{
						if (enumerator.CurrentTransition.Value>0.0)
							newGraph.AddVerticesAndEdge(new Edge(enumerator.CurrentState, enumerator.CurrentTransition.Column));
					}
				}
			}
		}

		internal MarkovChainEnumerator GetEnumerator()
		{
			return new MarkovChainEnumerator(this);
		}

		// a nested class can access private members
		internal class MarkovChainEnumerator
		{
			private DiscreteTimeMarkovChain _markovChain;
			private SparseDoubleMatrix.SparseDoubleMatrixEnumerator _enumerator;

			public int CurrentState { get; private set; }

			public SparseDoubleMatrix.ColumnValue CurrentTransition => _enumerator.CurrentColumnValue.Value;

			public MarkovChainEnumerator(DiscreteTimeMarkovChain markovChain)
			{
				_markovChain = markovChain;
				_enumerator = markovChain.ProbabilityMatrix.GetEnumerator();
				Reset();
			}

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// </summary>
			public void Dispose()
			{
			}

			public void SelectInitialDistribution()
			{
				CurrentState = -1;
				_enumerator.MoveRow(_markovChain.RowOfInitialStates);
			}

			public bool SelectSourceState(int state)
			{
				CurrentState = state;
				return _enumerator.MoveRow(CurrentState+1); //state 0 lies in row 1, state 1 in row 2,...
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public bool MoveNextState()
			{
				// MoveNextState() returns on a reseted enumerator the first state
				return SelectSourceState(CurrentState+1);
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public bool MoveNextTransition()
			{
				return _enumerator.MoveNextColumn();
			}

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public void Reset()
			{
				SelectInitialDistribution();
			}
		}
	}
}
