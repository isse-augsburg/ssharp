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

	
	internal unsafe class MarkovChain
	{
		// TODO: Optimization potential for custom model checker: Add every state only once. Save the transitions and evaluate reachability formulas more efficient by only expanding "states" to "states x stateformulaset" where the state labels of interests are in "stateformulaset"


		public string[] StateFormulaLabels;

		public string[] StateRewardRetrieverLabels;

		public SparseDoubleMatrix ProbabilityMatrix { get; }

		public DoubleVector InitialStateProbabilities;

		public LabelVector StateLabeling;
		
		public MarkovChain(int maxNumberOfStates= 1 << 21, int maxNumberOfTransitions=0)
		{
			if (maxNumberOfTransitions <= 0)
			{
				maxNumberOfTransitions = maxNumberOfStates << 6;
				if (maxNumberOfTransitions < maxNumberOfStates)
					maxNumberOfTransitions = Int32.MaxValue - 1;
			}

			InitialStateProbabilities = new DoubleVector();
			StateLabeling = new LabelVector();
			ProbabilityMatrix = new SparseDoubleMatrix(maxNumberOfStates, maxNumberOfTransitions);
		}


		// Retrieving matrix phase

		public int States => ProbabilityMatrix.Rows;

		public int Transitions => ProbabilityMatrix.TotalColumnValueEntries;


		// Creating matrix phase
			
		internal void AddInitialState(int markovChainState, double probability)
		{
			InitialStateProbabilities[markovChainState] = InitialStateProbabilities[markovChainState] + probability;
		}

		internal void SetMarkovChainSourceStateOfUpcomingTransitions(int markovChainState)
		{
			ProbabilityMatrix.SetRow(markovChainState);
		}

		internal void AddTransition(int markovChainState, double probability)
		{
			ProbabilityMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(markovChainState, probability));
		}

		internal void SetStateLabeling(int markovChainState, StateFormulaSet formula)
		{
			StateLabeling[markovChainState] = formula;
		}

		internal void FinishSourceState()
		{
			ProbabilityMatrix.FinishRow();
		}

		public void SealProbabilityMatrix()
		{
			InitialStateProbabilities.IncreaseSize(States);
			ProbabilityMatrix.OptimizeAndSeal();
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
			return new UnderlyingDigraph(this);
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
