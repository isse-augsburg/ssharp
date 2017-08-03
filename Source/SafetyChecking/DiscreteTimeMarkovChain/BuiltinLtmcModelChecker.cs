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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using Modeling;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using Utilities;
	using Formula;
	using GenericDataStructures;

	internal class BuiltinLtmcModelChecker : LtmcModelChecker
	{
		internal enum PrecalculatedTransitionTarget : byte
		{
			Nothing = 0,
			Satisfied = 1,
			Excluded = 2,
		}

		// Note: Should be used with using(var modelchecker = new ...)
		public BuiltinLtmcModelChecker(LabeledTransitionMarkovChain markovChain, TextWriter output = null) : base(markovChain, output)
		{
			markovChain.AssertIsDense();
		}

		private PrecalculatedTransitionTarget[] CreateEmptyPrecalculatedTransitionTargetArray()
		{
			PrecalculatedTransitionTarget[] outputTargets = new PrecalculatedTransitionTarget[LabeledMarkovChain.Transitions];
			for (var i = 0; i < LabeledMarkovChain.Transitions; i++)
			{
				outputTargets[i] = PrecalculatedTransitionTarget.Nothing;
			}
			return outputTargets;
		}

		internal void CalculateSatisfiedTargets(PrecalculatedTransitionTarget[] precalculatedStates, Func<int, bool> formulaEvaluator)
		{
			for (var i = 0; i < LabeledMarkovChain.Transitions; i++)
			{
				if (formulaEvaluator(i))
					precalculatedStates[i] |= PrecalculatedTransitionTarget.Satisfied;
			}
		}

		internal void CalculateExcludedTargets(PrecalculatedTransitionTarget[] precalculatedStates, Func<int, bool> formulaEvaluator)
		{
			for (var i = 0; i < LabeledMarkovChain.Transitions; i++)
			{
				if (formulaEvaluator(i))
					precalculatedStates[i] |= PrecalculatedTransitionTarget.Excluded;
			}
		}

		private double[] CreateDerivedVector(PrecalculatedTransitionTarget[] precalculatedStates, PrecalculatedTransitionTarget flagToLookFor)
		{
			var derivedVector = new double[LabeledMarkovChain.Transitions];

			for (var i = 0; i < LabeledMarkovChain.Transitions; i++)
			{
				if (precalculatedStates[i].HasFlag(flagToLookFor))
					derivedVector[i] = 1.0;
				else
					derivedVector[i] = 0.0;
			}
			return derivedVector;
		}


		private double CalculateFinalProbability(double[] initialStateProbabilities)
		{
			var finalProbability = 0.0;

			var enumerator = LabeledMarkovChain.GetInitialDistributionEnumerator();
			while (enumerator.MoveNext())
			{
				finalProbability += enumerator.CurrentProbability * initialStateProbabilities[enumerator.CurrentTargetState];
			}
			return finalProbability;
		}

		private double CalculateProbabilityToReachStateFormulaInBoundedSteps(Formula psi, Formula phi, int steps)
		{
			// Pr[phi U psi]
			// calculate P [true U<=steps psi]

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var psiEvaluator = LabeledMarkovChain.CreateFormulaEvaluator(psi);
			var stateCount = LabeledMarkovChain.SourceStates.Count;

			var precalculatedStates = CreateEmptyPrecalculatedTransitionTargetArray();

			CalculateSatisfiedTargets(precalculatedStates,psiEvaluator);
			if (phi != null)
			{
				// excludedStates = Sat(\phi) \Cup Sat(psi)
				var phiEvaluator = LabeledMarkovChain.CreateFormulaEvaluator(phi);
				Func<int,bool> calculateExcludedStates = target =>
				{
					if (precalculatedStates[target] == PrecalculatedTransitionTarget.Satisfied)
						return false; //satisfied states are never excluded
					if (!phiEvaluator(target))
						return true; //exclude state if it does not satisfy phi
					return false;
				};
				CalculateExcludedTargets(precalculatedStates, calculateExcludedStates);
			}
			
			var xold = new double[stateCount];
			var xnew = new double[stateCount];
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
					var enumerator = LabeledMarkovChain.GetTransitionEnumerator(i);
					var sum = 0.0;
					
					while (enumerator.MoveNext())
					{
						var transitionTarget = enumerator.CurrentIndex;
						if (precalculatedStates[transitionTarget].HasFlag(PrecalculatedTransitionTarget.Satisfied))
						{
							sum += enumerator.CurrentProbability;
						}
						else if (precalculatedStates[transitionTarget].HasFlag(PrecalculatedTransitionTarget.Excluded))
						{
						}
						else
						{
							sum += enumerator.CurrentProbability * xold[enumerator.CurrentTargetState];
						}
					}
					xnew[i] = sum;
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateFinalProbability(xnew);
					_output?.WriteLine($"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}

			var finalProbability=CalculateFinalProbability(xnew);
			
			stopwatch.Stop();
			return finalProbability;
		}

		internal override Probability CalculateProbability(Formula formulaToCheck)
		{
			_output.WriteLine($"Checking formula: {formulaToCheck}");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var finallyUnboundUnaryFormula = formulaToCheck as UnaryFormula;
			var finallyBoundedUnaryFormula = formulaToCheck as BoundedUnaryFormula;
			var finallyBoundedBinaryFormula = formulaToCheck as BoundedBinaryFormula;

			double result;

			if (finallyUnboundUnaryFormula != null && finallyUnboundUnaryFormula.Operator == UnaryOperator.Finally)
			{
				throw new NotImplementedException();
			}
			else if (finallyBoundedUnaryFormula != null && finallyBoundedUnaryFormula.Operator == UnaryOperator.Finally)
			{
				result = CalculateProbabilityToReachStateFormulaInBoundedSteps(finallyBoundedUnaryFormula.Operand, null, finallyBoundedUnaryFormula.Bound);
			}
			else if (finallyBoundedBinaryFormula != null && finallyBoundedBinaryFormula.Operator == BinaryOperator.Until)
			{
				result = CalculateProbabilityToReachStateFormulaInBoundedSteps(finallyBoundedBinaryFormula.RightOperand, finallyBoundedBinaryFormula.LeftOperand, finallyBoundedBinaryFormula.Bound);
			}
			else
			{
				throw new NotImplementedException();
			}
			
			stopwatch.Stop();

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(result);
		}

		private void ApproximateDelta(double target)
		{
			// https://en.wikipedia.org/wiki/Machine_epsilon
			//  Note: Result is even more inaccurate because in lines like
			//		newValue += verySmallValue1 * verySmallValue2
			//  as they appear in the iteration algorithm may already lead to epsilons which are ignored
			//  even if they would have an impact in sum ("sum+=100*epsilon" has an influence, but "for (i=1..100) {sum+=epsilon;}" not.

			double epsilon = 1.0;
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			while ((target + (epsilon / 2.0)) != target)
				epsilon /= 2.0;

			_output?.WriteLine(epsilon);
		}

		internal override bool CalculateBoolean(Formula formulaToCheck)
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
