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
	using Analysis.Probabilistic;
	using Utilities;

	class BuiltinMdpModelChecker : MdpModelChecker
	{
		/*
		private SparseDoubleMatrix CreateDerivedMatrix(Dictionary<int, bool> exactlyOneStates, Dictionary<int, bool> exactlyZeroStates)
		{
			//Derived matrix is 0-based. Row i is equivalent to the probability distribution of state i (this is not the case for the Markov Chain). 

			var derivedMatrix = new SparseDoubleMatrix(MarkovChain.States, MarkovChain.Transitions+ MarkovChain.States); //Transitions+States is a upper limit

			var enumerator = MarkovChain.GetEnumerator();

			for (var sourceState = 0; sourceState < MarkovChain.States; sourceState++)
			{
				enumerator.SelectSourceState(sourceState);
				derivedMatrix.SetRow(sourceState);
				if (exactlyOneStates.ContainsKey(sourceState) || exactlyZeroStates.ContainsKey(sourceState))
				{
					// only add a self reference entry
					derivedMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(sourceState,1.0));
				}
				else
				{
					// if state is neither exactlyOneStates nor exactlyZeroStates, it is a toCalculateState
					var selfReferenceAdded = false;
					while (enumerator.MoveNextTransition())
					{
						var columnValueEntry = enumerator.CurrentTransition;
						var targetState = columnValueEntry.Column;
						if (targetState == sourceState)
						{
							//this implements the removal of the identity matrix
							derivedMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(sourceState, columnValueEntry.Value - 1.0));
							selfReferenceAdded = true;
						}
						else
						{
							derivedMatrix.AddColumnValueToCurrentRow(columnValueEntry);
						}
					}
					if (!selfReferenceAdded)
					{
						//this implements the removal of the identity matrix (if not already done)
						derivedMatrix.AddColumnValueToCurrentRow(new SparseDoubleMatrix.ColumnValue(sourceState, -1.0));
					}
				}
				derivedMatrix.FinishRow();
			}
			return derivedMatrix;
		}*/

		private double[] CreateDerivedVector(Dictionary<int, bool> exactlyOneStates)
		{
			var derivedVector = new double[MarkovDecisionProcess.States];

			for (var i = 0; i < MarkovDecisionProcess.States; i++)
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
			for (var i = 0; i < MarkovDecisionProcess.States; i++)
			{
				if (!states.ContainsKey(i))
					complement.Add(i, true);
			}
			return complement;
		}

		public BuiltinMdpModelChecker(ProbabilityRangeChecker probabilityRangeChecker) : base(probabilityRangeChecker)
		{
		}

		internal Dictionary<int,bool> CalculateSatisfiedStates(Func<int,bool> formulaEvaluator)
		{
			var satisfiedStates = new Dictionary<int,bool>();
			for (var i = 0; i < MarkovDecisionProcess.States; i++)
			{
				if (formulaEvaluator(i))
					satisfiedStates.Add(i,true);
			}
			return satisfiedStates;
		}
		/*
		private double GaussSeidelLike(SparseDoubleMatrix derivedMatrix, double[] derivedVector, int iterationsLeft)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var stateCount = MarkovChain.States;
			var resultVector = new double[stateCount];
			var fixPointReached = iterationsLeft <= 0;
			var iterations = 0;

			//Derived matrix is 0-based. Row i is equivalent to the probability distribution of state i (this is not the case for the Markov Chain). 
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
		}*/

		private double CalculateMinimumFinalProbability(double[] initialStateProbabilities)
		{
			var enumerator = MarkovDecisionProcess.GetEnumerator();
			enumerator.SelectInitialDistributions();

			//select sum of first distribution
			enumerator.MoveNextDistribution();
			var sum = 0.0;
			while (enumerator.MoveNextTransition())
			{
				var entry = enumerator.CurrentTransition;
				sum += entry.Value * initialStateProbabilities[entry.Column];
			}
			var finalProbability = sum;
			//now find a smaller one
			while (enumerator.MoveNextDistribution())
			{
				sum = 0.0;
				while (enumerator.MoveNextTransition())
				{
					var entry = enumerator.CurrentTransition;
					sum += entry.Value * initialStateProbabilities[entry.Column];
				}
				if (sum < finalProbability)
					finalProbability = sum;
			}
			return finalProbability;
		}

		private double CalculateMaximumFinalProbability(double[] initialStateProbabilities)
		{
			var enumerator = MarkovDecisionProcess.GetEnumerator();
			enumerator.SelectInitialDistributions();

			//select sum of first distribution
			enumerator.MoveNextDistribution();
			var sum = 0.0;
			while (enumerator.MoveNextTransition())
			{
				var entry = enumerator.CurrentTransition;
				sum += entry.Value * initialStateProbabilities[entry.Column];
			}
			var finalProbability = sum;
			//now find a larger one
			while (enumerator.MoveNextDistribution())
			{
				sum = 0.0;
				while (enumerator.MoveNextTransition())
				{
					var entry = enumerator.CurrentTransition;
					sum += entry.Value * initialStateProbabilities[entry.Column];
				}
				if (sum > finalProbability)
					finalProbability = sum;
			}
			return finalProbability;
		}

		private double CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps(Formula psi, int steps)
		{

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var psiEvaluator = MarkovDecisionProcess.CreateFormulaEvaluator(psi);
			var stateCount = MarkovDecisionProcess.States;

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var directlyUnsatisfiedStates = new Dictionary<int, bool>();  // change for \phi Until \psi

			var enumerator = MarkovDecisionProcess.GetEnumerator();

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
					if (directlySatisfiedStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 1.0;
					}
					else if (directlyUnsatisfiedStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 0.0;
					}
					else
					{
						enumerator.SelectSourceState(i);
						//select sum of first distribution
						enumerator.MoveNextDistribution();
						var sum = 0.0;
						while (enumerator.MoveNextTransition())
						{
							var entry = enumerator.CurrentTransition;
							sum += entry.Value * xold[entry.Column];
						}
						xnew[i] = sum;
						//now find a smaller one
						while (enumerator.MoveNextDistribution())
						{
							sum = 0.0;
							while (enumerator.MoveNextTransition())
							{
								var entry = enumerator.CurrentTransition;
								sum += entry.Value * xold[entry.Column];
							}
							if (sum < xnew[i])
								xnew[i] = sum;
						}
					}
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMinimumFinalProbability(xnew);
					Console.WriteLine($"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}

			var finalProbability = CalculateMinimumFinalProbability(xnew);

			stopwatch.Stop();
			return finalProbability;
		}

		private double CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(Formula psi, int steps)
		{

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var psiEvaluator = MarkovDecisionProcess.CreateFormulaEvaluator(psi);
			var stateCount = MarkovDecisionProcess.States;

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var directlyUnsatisfiedStates = new Dictionary<int, bool>();  // change for \phi Until \psi

			var enumerator = MarkovDecisionProcess.GetEnumerator();

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
					if (directlySatisfiedStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 1.0;
					}
					else if (directlyUnsatisfiedStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 0.0;
					}
					else
					{
						enumerator.SelectSourceState(i);
						//select sum of first distribution
						enumerator.MoveNextDistribution();
						var sum = 0.0;
						while (enumerator.MoveNextTransition())
						{
							var entry = enumerator.CurrentTransition;
							sum += entry.Value * xold[entry.Column];
						}
						xnew[i] = sum;
						//now find a larger one
						while (enumerator.MoveNextDistribution())
						{
							sum = 0.0;
							while (enumerator.MoveNextTransition())
							{
								var entry = enumerator.CurrentTransition;
								sum += entry.Value * xold[entry.Column];
							}
							if (sum > xnew[i])
								xnew[i] = sum;
						}
					}
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMaximumFinalProbability(xnew);
					Console.WriteLine($"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}

			var finalProbability = CalculateMaximumFinalProbability(xnew);

			stopwatch.Stop();
			return finalProbability;
		}



		/*
		private double CalculateProbabilityToReachStateFormula(Formula psi)
		{
			// calculate P [true U psi]

			var underlyingDigraph = MarkovChain.CreateUnderlyingDigraph();

			var psiEvaluator = MarkovChain.CreateFormulaEvaluator(psi);

			// calculate probabilityExactlyZero
			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var nodesToIgnore=new Dictionary<int,bool>();  // change for \phi Until \psi
			var probabilityGreaterThanZero = underlyingDigraph.Graph.GetAncestors(directlySatisfiedStates, nodesToIgnore);
			var probabilityExactlyZero = CreateComplement(probabilityGreaterThanZero);

			// calculate probabilityExactlyOne
			nodesToIgnore = directlySatisfiedStates; // change for \phi Until \psi
			var probabilitySmallerThanOne = underlyingDigraph.Graph.GetAncestors(probabilityExactlyZero, nodesToIgnore); ;
			var probabilityExactlyOne = CreateComplement(probabilitySmallerThanOne); ;

			//TODO: Do not calculate exact state probabilities, when _every_ initial state>0 is either in probabilityExactlyZero or in probabilityExactlyOne

			var derivedMatrix = CreateDerivedMatrix(probabilityExactlyOne, probabilityExactlyZero);
			var derivedVector = CreateDerivedVector(probabilityExactlyOne);

			var finalProbability = GaussSeidel(derivedMatrix, derivedVector, 50);
			
			return finalProbability;
		}*/

		/*
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
		}*/

		internal override Probability CalculateProbabilityRange(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}
		
		internal override Probability CalculateMinimalProbability(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		internal override Probability CalculateMaximalProbability(Formula formulaToCheck)
		{
			throw new NotImplementedException();
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
