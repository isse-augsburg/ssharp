// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System.Diagnostics;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using Modeling;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using ExecutedModel;
	using Formula;
	using GenericDataStructures;

	public class MarkovDecisionProcess : IModelWithStateLabelingInLabelingVector
	{
		// Distributions here are Probability Distributions

		public string[] StateFormulaLabels { get; set; }

		public string[] StateRewardRetrieverLabels;

		// Every state might have several non-deterministic choices (and at least 1).
		// Each such choice has a probability distribution which is saved in RowsWithProbabilityDistributions.
		public int StateToRowsEntries { private set; get; } = 0;
		private int _currentMarkovChainState = -1;
		private int _rowCountOfCurrentState = 0;
		public int[] StateToRowsL;
		public int[] StateToRowsRowCount;

		private const int _sizeOfState = sizeof(int);
		private const int _sizeOfTransition = sizeof(int) + sizeof(double);  //sizeof(SparseDoubleMatrix.ColumnValue)

		internal SparseDoubleMatrix RowsWithDistributions { get; }

		public LabelVector StateLabeling { get; }

		public MarkovDecisionProcess(ModelCapacity modelCapacity)
		{
			var modelSize = modelCapacity.DeriveModelByteSize(_sizeOfState, _sizeOfTransition);

			StateLabeling = new LabelVector();
			RowsWithDistributions = new SparseDoubleMatrix(modelSize.NumberOfStates + 1, modelSize.NumberOfTransitions); // need for every distribution one row
			StateToRowsL = new int[modelSize.NumberOfStates + 1]; // one additional row for initial distributions
			StateToRowsRowCount = new int[modelSize.NumberOfStates + 1]; // one additional row for initial distributions
			SetRowOfStateEntriesToInvalid();
		}
		
		private void SetRowOfStateEntriesToInvalid()
		{
			for (var i = 0; i < StateToRowsL.Length; i++)
			{
				StateToRowsL[i] = -1;
			}
			for (var i = 0; i < StateToRowsRowCount.Length; i++)
			{
				StateToRowsRowCount[i] = -1;
			}
		}

		// Retrieving matrix phase

		public int States => StateToRowsEntries -1; //Note: returns 0 if initial distribution added and -1 if nothing was added. So check for 0 is not enough

		public int Transitions { get; private set; } = 0; //without entries of initial distribution

		public int StateToRowsEntryOfInitialDistributions = 0;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int StateToColumn(int state) => state; //Do nothing! Just here to make the algorithms more clear.

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int ColumnToState(int state) => state; //Do nothing! Just here to make the algorithms more clear.


		// Creating matrix phase

		// For the initial distributions the process is
		//    StartWithInitialDistributions()
		//    while(distributions to add exist) {
		//	      StartWithNewInitialDistribution();
		//	      while(transitions to add exist) {
		//	          AddTransitionToInitialDistribution();
		//	      }
		//        FinishInitialDistribution()
		//    }
		//    FinishInitialDistributions()
		internal void StartWithInitialDistributions()
		{
			_rowCountOfCurrentState = 0;
			StateToRowsL[StateToRowsEntryOfInitialDistributions] = RowsWithDistributions.Rows; //set beginning row of state to the next free row
		}
		
		internal void StartWithNewInitialDistribution()
		{
			RowsWithDistributions.SetRow(RowsWithDistributions.Rows); //just append one row in the matrix
			_rowCountOfCurrentState++;
		}

		internal void AddTransitionToInitialDistribution(int markovChainState, double probability)
		{
			RowsWithDistributions.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(StateToColumn(markovChainState), probability));
		}

		internal void FinishInitialDistribution()
		{
			RowsWithDistributions.FinishRow();
		}

		internal void FinishInitialDistributions()
		{
			StateToRowsRowCount[StateToRowsEntryOfInitialDistributions] = _rowCountOfCurrentState;
			StateToRowsEntries++;
		}
		
		// For distributions of a state the process is
		//    StartWithNewDistributions(markovChainSourceState)
		//    while(distributions to add exist) {
		//	      StartWithNewDistribution();
		//	      while(transitions to add exist) {
		//	          AddTransitionToDistribution();
		//	      }
		//        FinishDistribution()
		//    }
		//    FinishDistributions()
		internal void StartWithNewDistributions(int markovChainState)
		{
			_rowCountOfCurrentState = 0;
			_currentMarkovChainState = markovChainState;
			StateToRowsL[_currentMarkovChainState + 1] = RowsWithDistributions.Rows; //set beginning row of state to the next free row
		}

		internal void StartWithNewDistribution()
		{
			RowsWithDistributions.SetRow(RowsWithDistributions.Rows); //just append one row
			_rowCountOfCurrentState++;
		}

		internal void AddTransition(int markovChainState, double probability)
		{
			RowsWithDistributions.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(StateToColumn(markovChainState), probability));
			Transitions++;
		}
		
		internal void FinishDistribution()
		{
			RowsWithDistributions.FinishRow();
		}

		internal void FinishDistributions()
		{
			StateToRowsRowCount[_currentMarkovChainState+1] = _rowCountOfCurrentState;
			StateToRowsEntries++;
		}



		internal void SetStateLabeling(int markovChainState, StateFormulaSet formula)
		{
			StateLabeling[markovChainState] = formula;
		}

		public void SealProbabilityMatrix()
		{
			RowsWithDistributions.OptimizeAndSeal();
		}

		// Validation

		[Conditional("DEBUG")]
		public void ValidateStates()
		{
			var enumerator = RowsWithDistributions.GetEnumerator();

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
					var candidate = new SparseDoubleMatrix.ColumnValue(-1,Double.NegativeInfinity);
					while (enumerator.MoveNextDistribution())
					{
						while (enumerator.MoveNextTransition())
						{
							if (candidate.Value < enumerator.CurrentTransition.Value)
								candidate = enumerator.CurrentTransition;
						}
					}
					
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


			enumerator.SelectInitialDistributions();
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int InitialDistributionsRowL()
		{
			return StateToRowsL[StateToRowsEntryOfInitialDistributions];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int InitialDistributionsRowH()
		{
			return StateToRowsL[StateToRowsEntryOfInitialDistributions] + StateToRowsRowCount[StateToRowsEntryOfInitialDistributions];
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int StateToRowL(int state)
		{
			return StateToRowsL[state+1];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int StateToRowH(int state)
		{
			return StateToRowsL[state + 1] + StateToRowsRowCount[state + 1];
		}
		

		public Func<int, bool> CreateFormulaEvaluator(Formula formula)
		{
			return LabelingVectorFormulaEvaluatorCompilationVisitor.Compile(this, formula);
		}
		
		internal UnderlyingDigraph CreateUnderlyingDigraph()
		{
			return new UnderlyingDigraph(this);
		}
		
		internal class UnderlyingDigraph
		{
			internal struct EdgeData
			{
				internal EdgeData(int rowOfDistribution)
				{
					RowOfDistribution = rowOfDistribution;
				}

				public int RowOfDistribution;
			}

			public BidirectionalGraph<EdgeData> BaseGraph { get; }

			public UnderlyingDigraph(MarkovDecisionProcess mdp)
			{
				//Assumption "every node is reachable" is fulfilled due to the construction
				BaseGraph = new BidirectionalGraph<EdgeData>();

				var enumerator = mdp.GetEnumerator();
				while (enumerator.MoveNextState())
				{
					while (enumerator.MoveNextDistribution())
					{
						//find targets of this distribution and create the union. Some possibleSuccessors may be added
						while (enumerator.MoveNextTransition())
						{
							if (enumerator.CurrentTransition.Value > 0.0)
								BaseGraph.AddVerticesAndEdge(new Edge<EdgeData>(enumerator.CurrentState, enumerator.CurrentTransition.Column,new EdgeData(enumerator.RowOfCurrentDistribution)));
						}
					}
				}
			}
		}

		internal MarkovDecisionProcessEnumerator GetEnumerator()
		{
			return new MarkovDecisionProcessEnumerator(this);
		}
		
		// a nested class can access private members
		internal class MarkovDecisionProcessEnumerator
		{
			private MarkovDecisionProcess _mdp;
			private SparseDoubleMatrix.SparseDoubleMatrixEnumerator _matrixEnumerator;

			public int CurrentState { get; private set; }

			// The CurrentState has several probability distributions, but at least one.
			// These are saved in _mdp.RowsWithDistributions.
			// The row numbers [_rowLOfCurrentState,...,_rowHOfCurrentState) belong to CurrentState.
			private int _rowLOfCurrentState; //inclusive
			private int _rowHOfCurrentState; //exclusive
			//the current row of the enumerator
			public int RowOfCurrentDistribution { get; private set; }


			public SparseDoubleMatrix.ColumnValue CurrentTransition => _matrixEnumerator.CurrentColumnValue.Value;

			public MarkovDecisionProcessEnumerator(MarkovDecisionProcess mdp)
			{
				_mdp = mdp;
				_matrixEnumerator = mdp.RowsWithDistributions.GetEnumerator();
				Reset();
			}

			/// <summary>
			/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
			/// </summary>
			public void Dispose()
			{
			}

			public void SelectInitialDistributions()
			{
				CurrentState = -1;
				_rowLOfCurrentState = _mdp.InitialDistributionsRowL();
				_rowHOfCurrentState = _mdp.InitialDistributionsRowH();
				RowOfCurrentDistribution = _rowLOfCurrentState-1; //select 1 entry before the actual first entry. So MoveNextDistribution can move to the right entry.
			}

			public bool SelectSourceState(int state)
			{
				if (state >= _mdp.States)
					return false;
				CurrentState = state;
				_rowLOfCurrentState = _mdp.StateToRowL(state);
				_rowHOfCurrentState = _mdp.StateToRowH(state);
				RowOfCurrentDistribution = _rowLOfCurrentState-1; //select 1 entry before the actual first entry. So MoveNextDistribution can move to the right entry.
				return true;
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
				return SelectSourceState(CurrentState + 1);
			}

			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns>
			/// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
			/// </returns>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public bool MoveNextDistribution()
			{
				RowOfCurrentDistribution++;
				if (RowOfCurrentDistribution >= _rowHOfCurrentState)
					return false;
				return _matrixEnumerator.MoveRow(RowOfCurrentDistribution);
			}


			public void MoveToDistribution(int distribution)
			{
				// WARNING: Has side effects on internal data. Do not use in conjunction with MoveNextDistribution and MoveNextState.
				// Only with MoveNextTransition. Create separate enumerator for these cases.
				// TODO: Refactor Enumerator. Create separate enumerators for States, Distributions, and Transitions. One
				RowOfCurrentDistribution = distribution;
				_matrixEnumerator.MoveRow(RowOfCurrentDistribution);
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
				return _matrixEnumerator.MoveNextColumn();
			}

			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
			public void Reset()
			{
				SelectInitialDistributions();
			}
		}
	}
}
