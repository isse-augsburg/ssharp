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
	using Formula;
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

		private double CalculateMinimumProbabilityOfCid(double[] stateProbabilities, long currentCid)
		{
			NestedMarkovDecisionProcess.ContinuationGraphElement cge = Nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = Nmdp.GetContinuationGraphLeaf(currentCid);
				var transitionProbability = cgl.Probability;
				var targetStateProbability = stateProbabilities[cgl.ToState];
				var probabilityToSatisfyStateWithThisTransition = transitionProbability * targetStateProbability;
				return probabilityToSatisfyStateWithThisTransition;
			}
			else
			{
				var cgi = Nmdp.GetContinuationGraphInnerNode(currentCid);
				if (cge.IsChoiceTypeDeterministic)
				{
					return CalculateMinimumProbabilityOfCid(stateProbabilities, cgi.FromCid);
				}
				if (cge.IsChoiceTypeNondeterministic)
				{
					var smallest = double.PositiveInfinity;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var resultOfChild = CalculateMinimumProbabilityOfCid(stateProbabilities, i);
						if (resultOfChild < smallest)
							smallest = resultOfChild;
					}
					return smallest;
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					var sum = 0.0;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var resultOfChild = CalculateMinimumProbabilityOfCid(stateProbabilities, i);
						sum += resultOfChild;
					}
					return sum;
				}
			}
			return double.NaN;
		}

		private double CalculateMaximumProbabilityOfCid(double[] stateProbabilities, long currentCid)
		{
			NestedMarkovDecisionProcess.ContinuationGraphElement cge = Nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = Nmdp.GetContinuationGraphLeaf(currentCid);
				var transitionProbability = cgl.Probability;
				var targetStateProbability = stateProbabilities[cgl.ToState];
				var probabilityToSatisfyStateWithThisTransition = transitionProbability * targetStateProbability;
				return probabilityToSatisfyStateWithThisTransition;
			}
			else
			{
				var cgi = Nmdp.GetContinuationGraphInnerNode(currentCid);
				if (cge.IsChoiceTypeDeterministic)
				{
					return CalculateMinimumProbabilityOfCid(stateProbabilities, cgi.FromCid);
				}
				if (cge.IsChoiceTypeNondeterministic)
				{
					var biggest = double.NegativeInfinity;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var resultOfChild = CalculateMinimumProbabilityOfCid(stateProbabilities, i);
						if (resultOfChild > biggest)
							biggest = resultOfChild;
					}
					return biggest;
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					var sum = 0.0;
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var resultOfChild = CalculateMinimumProbabilityOfCid(stateProbabilities, i);
						sum += resultOfChild;
					}
					return sum;
				}
			}
			return double.NaN;
		}

		public double CalculateMinimumFinalProbability(double[] initialStateProbabilities)
		{
			var cid = Nmdp.GetRootContinuationGraphLocationOfInitialState();
			return CalculateMinimumProbabilityOfCid(initialStateProbabilities, cid);
		}

		internal double CalculateMaximumFinalProbability(double[] initialStateProbabilities)
		{
			var cid = Nmdp.GetRootContinuationGraphLocationOfInitialState();
			return CalculateMaximumProbabilityOfCid(initialStateProbabilities, cid);
		}

		internal double[] MinimumIterator(Dictionary<int, bool> exactlyOneStates, Dictionary<int, bool> exactlyZeroStates, int steps)
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
						xnew[i] = CalculateMinimumProbabilityOfCid(xold, cid);
					}
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMinimumFinalProbability(xnew);
					_output?.WriteLine(
						$"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}
			stopwatch.Stop();
			return xnew;
		}

		internal double[] MaximumIterator(Dictionary<int, bool> exactlyOneStates, Dictionary<int, bool> exactlyZeroStates, int steps)
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
						xnew[i] = CalculateMaximumProbabilityOfCid(xold, cid);
					}
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMaximumFinalProbability(xnew);
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
			var psiEvaluator = Nmdp.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>(); // change for \phi Until \psi

			var xnew = MinimumIterator(directlySatisfiedStates, excludedStates, steps);

			var finalProbability = CalculateMinimumFinalProbability(xnew);

			return finalProbability;
		}

		internal double CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(Formula psi, int steps)
		{
			var psiEvaluator = Nmdp.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>(); // change for \phi Until \psi


			var xnew = MaximumIterator(directlySatisfiedStates, excludedStates, steps);

			var finalProbability = CalculateMaximumFinalProbability(xnew);

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
