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
	using Modeling;
	using Serialization;
	using Utilities;
	/*
	internal class StateStorageStateToMarkovChainStateMapper
	{
		public StateStorage(StateVectorLayout layout, int capacity)
		{
			Requires.NotNull(layout, nameof(layout));
			Requires.InRange(capacity, nameof(capacity), 1024, Int32.MaxValue);

			_stateVectorSize = layout.SizeInBytes;
			_capacity = capacity;

			_stateBuffer.Resize((long)_capacity * _stateVectorSize, zeroMemory: false);
			_stateMemory = _stateBuffer.Pointer;

			// We allocate enough space so that we can align the returned pointer such that index 0 is the start of a cache line
			_hashBuffer.Resize((long)_capacity * sizeof(int) + CacheLineSize, zeroMemory: false);
			_hashMemory = (int*)_hashBuffer.Pointer;

			if ((ulong)_hashMemory % CacheLineSize != 0)
				_hashMemory = (int*)(_hashBuffer.Pointer + (CacheLineSize - (ulong)_hashBuffer.Pointer % CacheLineSize));

			Assert.InRange((ulong)_hashMemory - (ulong)_hashBuffer.Pointer, 0ul, (ulong)CacheLineSize);
			Assert.That((ulong)_hashMemory % CacheLineSize == 0, "Invalid buffer alignment.");
		}
	}*/

	internal class MarkovChain
	{
		public bool MarkovChainComplete { get; private set; }

		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;

		public SparseDoubleMatrix _matrix { get; }

		public DoubleVector InitialStateProbabilities = new DoubleVector();

		public Dictionary<int, StateFormulaSet> StateLabeling = new Dictionary<int, StateFormulaSet>();

		public Dictionary<int, Reward[]> StateRewards = new Dictionary<int, Reward[]>();

		Dictionary<int, int> compactToSparse = new Dictionary<int, int>();
		Dictionary<int, int> sparseToCompact = new Dictionary<int, int>();

		public MarkovChain()
		{
			_matrix = new SparseDoubleMatrix(1 << 21, 1 << 23);
		}


		// Retrieving matrix phase

		public int States { get; private set; } = 0;

		public int? ExceptionState { get; private set; } = null;

		public bool HasExceptionInModel => ExceptionState != -1;


		// Creating matrix phase

		private int GetOrCreateStateForException()
		{
			if (ExceptionState != null)
				return ExceptionState.Value;
			ExceptionState = States;
			_matrix.SetRow(ExceptionState.Value); //add state for exception
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(ExceptionState.Value, 1.0)); //Add self-loop in exception
			States++;
			return ExceptionState.Value;
		}
		/*
		private int CreateNewState(int sparseId)
		{
			{
				if (sparseToCompact.ContainsKey(sparseId))
				{
					return sparseToCompact[sparseId];
				}
				else
				{
					var compactId = ++compactProbabilityMatrix.States;
					sparseToCompact.Add(sparseId, compactId);
					compactToSparse.Add(compactId, sparseId);
					return compactId;
				}
			}
		}*/

		public void AddInitialState(int state, Probability probability)
		{
			InitialStateProbabilities[state] = InitialStateProbabilities[state] + probability.Value;
		}

		public void AddInitialException(Probability probability)
		{
			AddInitialState(GetOrCreateStateForException(), probability);
		}

		public void SetSourceStateOfUpcomingTransitions(int state)
		{
			_matrix.SetRow(state);
		}

		public void AddTransition(int state, Probability probability)
		{
			_matrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(state, probability.Value));
		}

		public void AddTransitionException(Probability probability)
		{
			AddTransition(GetOrCreateStateForException(), probability);
		}

		public void FinishSourceState()
		{
			_matrix.FinishRow();
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
			if (_matrix.Rows != States)
			{
				throw new Exception("Number of states should be equal to the number of rows in the matrix");
			}

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
	}
}
