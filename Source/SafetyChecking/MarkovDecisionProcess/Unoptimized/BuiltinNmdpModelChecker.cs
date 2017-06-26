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
using System.Collections.Generic;

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using Modeling;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Runtime.CompilerServices;
	using Formula;
	using GenericDataStructures;
	using Utilities;

	class BuiltinNmdpModelChecker : NmdpModelChecker
	{
		private NestedMarkovDecisionProcess.UnderlyingDigraph _underlyingDigraph;

		private double[] CreateDerivedVector(Dictionary<int, bool> exactlyOneStates)
		{
			var derivedVector = new double[Nmdp.States];

			for (var i = 0; i < Nmdp.States; i++)
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
			for (var i = 0; i < Nmdp.States; i++)
			{
				if (!states.ContainsKey(i))
					complement.Add(i, true);
			}
			return complement;
		}

		// Note: Should be used with using(var modelchecker = new ...)
		public BuiltinNmdpModelChecker(NestedMarkovDecisionProcess nmdp, TextWriter output = null)
			: base(nmdp, output)
		{
			_underlyingDigraph = Nmdp.CreateUnderlyingDigraph();
		}

		internal Dictionary<int, bool> CalculateSatisfiedStates(Func<int, bool> formulaEvaluator)
		{
			var satisfiedStates = new Dictionary<int, bool>();
			for (var i = 0; i < Nmdp.States; i++)
			{
				if (formulaEvaluator(i))
					satisfiedStates.Add(i, true);
			}
			return satisfiedStates;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool HasCacheEntry(AutoResizeBigVector<double> cache, long entry)
		{
			return !double.IsNaN(cache[(int)entry]);
		}

		private double CalculateMinimumProbabilityOfCid(AutoResizeBigVector<double> cache, double[] stateProbabilities, long currentCid)
		{
			if (HasCacheEntry(cache,currentCid))
				return cache[(int)currentCid];
			var result = double.NaN;
			NestedMarkovDecisionProcess.ContinuationGraphElement cge = Nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = Nmdp.GetContinuationGraphLeaf(currentCid);
				result = stateProbabilities[cgl.ToState];
			}
			else
			{
				var cgi = Nmdp.GetContinuationGraphInnerNode(currentCid);
				if (cge.IsChoiceTypeForward)
				{
					// Note, cgi.Probability is used in the branch "else if (cge.IsChoiceTypeProbabilitstic)"
					result = CalculateMinimumProbabilityOfCid(cache, stateProbabilities, cgi.FromCid);
				}
				if (cge.IsChoiceTypeNondeterministic)
				{
					var smallest = double.PositiveInfinity;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var resultOfChild = CalculateMinimumProbabilityOfCid(cache, stateProbabilities, i);
						if (resultOfChild < smallest)
							smallest = resultOfChild;
					}
					result = smallest;
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					var sum = 0.0;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var transitionProbability = Nmdp.GetContinuationGraphElement(i).Probability;
						var resultOfChild = CalculateMinimumProbabilityOfCid(cache, stateProbabilities, i);
						sum += transitionProbability*resultOfChild;
					}
					result = sum;
				}
			}
			cache[(int)currentCid] = result;
			return result;
		}

		private double CalculateMaximumProbabilityOfCid(AutoResizeBigVector<double> cache, double[] stateProbabilities, long currentCid)
		{
			if (HasCacheEntry(cache, currentCid))
				return cache[(int)currentCid];
			var result = double.NaN;
			NestedMarkovDecisionProcess.ContinuationGraphElement cge = Nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = Nmdp.GetContinuationGraphLeaf(currentCid);
				result = stateProbabilities[cgl.ToState];
			}
			else
			{
				var cgi = Nmdp.GetContinuationGraphInnerNode(currentCid);
				if (cge.IsChoiceTypeForward)
				{
					// Note, cgi.Probability is used in the branch "else if (cge.IsChoiceTypeProbabilitstic)"
					result = CalculateMaximumProbabilityOfCid(cache, stateProbabilities, cgi.FromCid);
				}
				if (cge.IsChoiceTypeNondeterministic)
				{
					var biggest = double.NegativeInfinity;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var resultOfChild = CalculateMaximumProbabilityOfCid(cache, stateProbabilities, i);
						if (resultOfChild > biggest)
							biggest = resultOfChild;
					}
					result = biggest;
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					var sum = 0.0;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var transitionProbability = Nmdp.GetContinuationGraphElement(i).Probability;
						var resultOfChild = CalculateMaximumProbabilityOfCid(cache, stateProbabilities, i);
						sum += transitionProbability * resultOfChild;
					}
					result = sum;
				}
			}
			cache[(int)currentCid] = result;
			return result;
		}

		public double CalculateMinimumFinalProbability(AutoResizeBigVector<double> cache, double[] initialStateProbabilities)
		{
			var cid = Nmdp.GetRootContinuationGraphLocationOfInitialState();
			cache.Clear(cid); //use cid as offset, because it is the smallest element
			return CalculateMinimumProbabilityOfCid(cache, initialStateProbabilities, cid);
		}

		internal double CalculateMaximumFinalProbability(AutoResizeBigVector<double> cache, double[] initialStateProbabilities)
		{
			var cid = Nmdp.GetRootContinuationGraphLocationOfInitialState();
			cache.Clear(cid); //use cid as offset, because it is the smallest element
			return CalculateMaximumProbabilityOfCid(cache, initialStateProbabilities, cid);
		}

		internal double[] MinimumIterator(AutoResizeBigVector<double> cache, Dictionary<int, bool> exactlyOneStates, Dictionary<int, bool> exactlyZeroStates, int steps)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var stateCount = Nmdp.States;

			var xold = new double[stateCount];
			var xnew = CreateDerivedVector(exactlyOneStates);
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
					if (exactlyOneStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 1.0;
					}
					else if (exactlyZeroStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 0.0;
					}
					else
					{
						var cid = Nmdp.GetRootContinuationGraphLocationOfState(i);
						cache.Clear(cid); //use cid as offset, because it is the smallest element
						xnew[i] = CalculateMinimumProbabilityOfCid(cache, xold, cid);
					}
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMinimumFinalProbability(cache, xnew);
					_output?.WriteLine(
						$"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}
			stopwatch.Stop();
			return xnew;
		}

		internal double[] MaximumIterator(AutoResizeBigVector<double> cache, Dictionary<int, bool> exactlyOneStates, Dictionary<int, bool> exactlyZeroStates, int steps)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var stateCount = Nmdp.States;

			var xold = new double[stateCount];
			var xnew = CreateDerivedVector(exactlyOneStates);
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
					if (exactlyOneStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 1.0;
					}
					else if (exactlyZeroStates.ContainsKey(i))
					{
						//we could remove this line, because already set by CreateDerivedVector and never changed when we initialize xold with CreateDerivedVector(directlySatisfiedStates)
						xnew[i] = 0.0;
					}
					else
					{
						var cid = Nmdp.GetRootContinuationGraphLocationOfState(i);
						cache.Clear(cid); //use cid as offset, because it is the smallest element
						xnew[i] = CalculateMaximumProbabilityOfCid(cache, xold, cid);
					}
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMaximumFinalProbability(cache, xnew);
					_output?.WriteLine(
						$"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}
			stopwatch.Stop();
			return xnew;
		}


		internal double CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps(Formula psi, int steps)
		{
			var cache = new AutoResizeBigVector<double> {DefaultValue = double.NaN};

			var psiEvaluator = Nmdp.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>(); // change for \phi Until \psi
			
			var xnew = MinimumIterator(cache,directlySatisfiedStates, excludedStates, steps);

			var finalProbability = CalculateMinimumFinalProbability(cache, xnew);

			return finalProbability;
		}

		internal double CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(Formula psi, int steps)
		{
			var cache = new AutoResizeBigVector<double> { DefaultValue = double.NaN };

			var psiEvaluator = Nmdp.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>(); // change for \phi Until \psi
			
			var xnew = MaximumIterator(cache, directlySatisfiedStates, excludedStates, steps);

			var finalProbability = CalculateMaximumFinalProbability(cache, xnew);

			return finalProbability;
		}


		internal override ProbabilityRange CalculateProbabilityRange(Formula formulaToCheck)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var finallyUnboundFormula = formulaToCheck as UnaryFormula;
			var finallyBoundedFormula = formulaToCheck as BoundedUnaryFormula;

			double minResult;
			double maxResult;

			if (finallyUnboundFormula != null && finallyUnboundFormula.Operator == UnaryOperator.Finally)
			{
				throw new NotImplementedException();
				//minResult = CalculateMinimumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
				//maxResult = CalculateMaximumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
			}
			else if (finallyBoundedFormula != null && finallyBoundedFormula.Operator == UnaryOperator.Finally)
			{
				minResult = CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps(finallyBoundedFormula.Operand, finallyBoundedFormula.Bound);
				maxResult = CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(finallyBoundedFormula.Operand, finallyBoundedFormula.Bound);
			}
			else
			{
				throw new NotImplementedException();
			}

			stopwatch.Stop();

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new ProbabilityRange(minResult, maxResult);
		}
		
		internal override Probability CalculateMinimalProbability(Formula formulaToCheck)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var finallyUnboundFormula = formulaToCheck as UnaryFormula;
			var finallyBoundedFormula = formulaToCheck as BoundedUnaryFormula;

			double minResult;

			if (finallyUnboundFormula != null && finallyUnboundFormula.Operator == UnaryOperator.Finally)
			{
				throw new NotImplementedException();
				//minResult = CalculateMinimumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
			}
			else if (finallyBoundedFormula != null && finallyBoundedFormula.Operator == UnaryOperator.Finally)
			{
				minResult = CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps(finallyBoundedFormula.Operand, finallyBoundedFormula.Bound);
			}
			else
			{
				throw new NotImplementedException();
			}

			stopwatch.Stop();

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(minResult);
		}

		internal override Probability CalculateMaximalProbability(Formula formulaToCheck)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var finallyUnboundFormula = formulaToCheck as UnaryFormula;
			var finallyBoundedFormula = formulaToCheck as BoundedUnaryFormula;
			
			double maxResult;

			if (finallyUnboundFormula != null && finallyUnboundFormula.Operator == UnaryOperator.Finally)
			{
				throw new NotImplementedException();
				//maxResult = CalculateMaximumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
			}
			else if (finallyBoundedFormula != null && finallyBoundedFormula.Operator == UnaryOperator.Finally)
			{
				maxResult = CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(finallyBoundedFormula.Operand, finallyBoundedFormula.Bound);
			}
			else
			{
				throw new NotImplementedException();
			}

			stopwatch.Stop();

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(maxResult);
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
