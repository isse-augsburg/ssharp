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

namespace SafetySharp.Analysis.ModelChecking.Probabilistic
{
	using Modeling;
	using Runtime;
	using System.Diagnostics;
	using System.Globalization;
	using Utilities;

	class BuiltinProbabilisticModelChecker : ProbabilisticModelChecker
	{
		private SparseDoubleMatrix CreateDerivedMatrix(Dictionary<int, bool> exactlyOneStates, Dictionary<int, bool> exactlyZeroStates)
		{
			var derivedMatrix = new SparseDoubleMatrix(MarkovChain.States, MarkovChain.Transitions+ MarkovChain.States); //Transitions+States is a upper limit

			var enumerator = MarkovChain.ProbabilityMatrix.GetEnumerator();

			while (enumerator.MoveNextRow())
			{
				var state = enumerator.CurrentRow;
				derivedMatrix.SetRow(state);
				if (exactlyOneStates.ContainsKey(state) || exactlyZeroStates.ContainsKey(state))
				{
					// only add a self reference entry
					derivedMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(state,1.0));
				}
				else
				{
					// if state is neither exactlyOneStates nor exactlyZeroStates, it is a toCalculateState
					var selfReferenceAdded = false;
					while (enumerator.MoveNextColumn())
					{
						if (enumerator.CurrentColumnValue != null)
						{
							var columnValueEntry = enumerator.CurrentColumnValue.Value;
							if (columnValueEntry.Column == state)
							{
								//this implements the removal of the identity matrix
								derivedMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(state, columnValueEntry.Value - 1.0));
								selfReferenceAdded = true;
							}
							else
							{
								derivedMatrix.AddColumnValueToCurrentRow(columnValueEntry);
							}
						}
						else
							throw new Exception("Entry must not be null");
					}
					if (!selfReferenceAdded)
					{
						//this implements the removal of the identity matrix
						derivedMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(state, -1.0));
					}
				}
				derivedMatrix.FinishRow();
			}
			return derivedMatrix;
		}

		private double[] CreateDerivedVector(Dictionary<int, bool> exactlyOneStates)
		{
			var derivedVector = new double[MarkovChain.States];

			for (var i = 0; i < MarkovChain.States; i++)
			{
				if (exactlyOneStates.ContainsKey(i))
					derivedVector[i] = 1.0;
				else
					derivedVector[i] = 0.0;
			}
			return derivedVector;
		}

		public Dictionary<int, bool> CreateComplement(Dictionary<int, bool> states)
		{
			var complement = new Dictionary<int, bool>();
			for (var i = 0; i < MarkovChain.States; i++)
			{
				if (!states.ContainsKey(i))
					complement.Add(i, true);
			}
			return complement;
		}

		public BuiltinProbabilisticModelChecker(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}

		internal Dictionary<int,bool> CalculateSatisfiedStates(Func<int,bool> formulaEvaluator)
		{
			var satisfiedStates = new Dictionary<int,bool>();
			for (var i = 0; i < MarkovChain.States; i++)
			{
				if (formulaEvaluator(i))
					satisfiedStates.Add(i,true);
			}
			return satisfiedStates;
		}
		
		private double GaussSeidel(SparseDoubleMatrix derivedMatrix, double[] derivedVector, int iterationsLeft)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var stateCount = MarkovChain.States;
			var resultVector = new double[stateCount];
			var fixPointReached = iterationsLeft <= 0;
			var iterations = 0;

			var enumerator = derivedMatrix.GetEnumerator();

			for (var i = 0; i < stateCount; i++)
			{
				resultVector[i] = 0.0;
			}
			while (!fixPointReached)
			{
				for (var i = 0; i < stateCount; i++)
				{
					var reflexiveEntry = 0.0;
					var temporaryValue = derivedVector[i];

					enumerator.MoveRow(i);
					while (enumerator.MoveNextColumn())
					{
						var currentEntry = enumerator.CurrentColumnValue.Value;
						if (currentEntry.Column == i)
							reflexiveEntry = currentEntry.Value;
						else
							temporaryValue -= currentEntry.Value * resultVector[currentEntry.Column];
					}
					Assert.That(reflexiveEntry != 0.0, "entry must not be 0.0");

					resultVector[i] = temporaryValue/ reflexiveEntry;
				}

				iterationsLeft--;
				iterations++;
				if (iterations % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateFinalProbability(resultVector);
					Console.WriteLine($"Completed {iterations} Gauss-Seidel iterations in {stopwatch.Elapsed}.  Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
				if (iterationsLeft <= 0)
					fixPointReached = true;
			}
			stopwatch.Stop();
			var finalProbability = CalculateFinalProbability(resultVector);

			return finalProbability;
		}

		private double CalculateFinalProbability(double[] initialStateProbabilities)
		{
			var finalProbability = 0.0;
			for (var i = 0; i < MarkovChain.States; i++)
			{
				finalProbability += MarkovChain.InitialStateProbabilities[i] * initialStateProbabilities[i];
			}
			return finalProbability;
		}

		private double CalculateProbabilityToReachStateFormulaInBoundedSteps(Formula psi, int steps)
		{
			// calculate P [true U<=steps psi]


			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var psiEvaluator = MarkovChain.CreateFormulaEvaluator(psi);
			var stateCount = MarkovChain.States;

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var directlyUnsatisfiedStates = new Dictionary<int, bool>();  // change for \phi Until \psi
			
			var enumerator = MarkovChain.ProbabilityMatrix.GetEnumerator();

			var xold = new double[stateCount];
			var xnew = CreateDerivedVector(directlySatisfiedStates);
			var loops = 0;
			while (loops < steps)
			{
				// switch xold and xnew
				var xtemp = xold;
				xold = xnew;
				xnew = xtemp;
				loops++;
				for (var i = 0; i < stateCount; i++)
				{
					enumerator.MoveRow(i);
					if (directlySatisfiedStates.ContainsKey(i))
					{
						xnew[i] = 1.0;
					}
					else if (directlyUnsatisfiedStates.ContainsKey(i))
					{
						xnew[i] = 0.0;
					}
					else
					{
						var sum = 0.0;
						while (enumerator.MoveNextColumn())
						{
							var entry = enumerator.CurrentColumnValue.Value;
							sum += entry.Value * xold[entry.Column];
						}
						xnew[i] = sum;
					}
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateFinalProbability(xnew);
					Console.WriteLine($"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}

			var finalProbability=CalculateFinalProbability(xnew);
			
			stopwatch.Stop();
			return finalProbability;
			
		}

		private double CalculateProbabilityToReachStateFormula(Formula psi)
		{
			// calculate P [true U psi]

			var underlyingDigraph = MarkovChain.CreateUnderlyingDigraph(); //TODO: source out

			var psiEvaluator = MarkovChain.CreateFormulaEvaluator(psi);

			// calculate probabilityExactlyZero
			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var nodesToIgnore=new Dictionary<int,bool>();  // change for \phi Until \psi
			var probabilityGreaterThanZero = underlyingDigraph.GetAncestors(directlySatisfiedStates, nodesToIgnore);
			var probabilityExactlyZero = CreateComplement(probabilityGreaterThanZero);

			// calculate probabilityExactlyOne
			nodesToIgnore = directlySatisfiedStates; // change for \phi Until \psi
			var probabilitySmallerThanOne = underlyingDigraph.GetAncestors(probabilityExactlyZero, nodesToIgnore); ;
			var probabilityExactlyOne = CreateComplement(probabilitySmallerThanOne); ;

			//TODO: Do not calculate exact state probabilities, when every initial state>0 is either in probabilityExactlyZero or in probabilityExactlyOne

			var derivedMatrix = CreateDerivedMatrix(probabilityExactlyOne, probabilityExactlyZero);
			var derivedVector = CreateDerivedVector(probabilityExactlyOne);

			var finalProbability = GaussSeidel(derivedMatrix, derivedVector, 2000);
			
			return finalProbability;
		}

		internal override Probability CalculateProbability(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var reachStateFormula = formulaToCheck as CalculateProbabilityToReachStateFormula;
			if (reachStateFormula == null)
				throw new NotImplementedException();
			var result=CalculateProbabilityToReachStateFormula(reachStateFormula.Operand);
			//var result = CalculateProbabilityToReachStateFormulaInBoundedSteps(reachStateFormula.Operand, 200);


			stopwatch.Stop();

			Console.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(result);
		}

		internal override bool CalculateFormula(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		internal override RewardResult CalculateReward(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
		}
	}
}
