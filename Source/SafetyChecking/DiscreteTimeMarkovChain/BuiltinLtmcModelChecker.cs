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

	internal class BuiltinLtmcModelChecker : LtmcModelChecker
	{
		internal enum PrecalculatedTransitionTarget : byte
		{
			Nothing = 0,
			Satisfied = 1,
			Excluded = 2,
		}

		private readonly LabeledTransitionMarkovChain.UnderlyingDigraph _underlyingDigraph;

		// Note: Should be used with using(var modelchecker = new ...)
		public BuiltinLtmcModelChecker(LabeledTransitionMarkovChain markovChain, TextWriter output = null)
			: base(markovChain, output)
		{
			Requires.That(true, "Need CompactStateStorage to use this model checker");
			LabeledMarkovChain.AssertIsDense();

			output.WriteLine("Initializing Built-in Ltmdp Model checker");
			output.WriteLine("Creating underlying digraph");
			_underlyingDigraph = LabeledMarkovChain.CreateUnderlyingDigraph();
			output.WriteLine("Finished creating underlying digraph");
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

		private void CalculateSatisfiedTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, Func<int, bool> formulaEvaluator)
		{
			for (var i = 0; i < LabeledMarkovChain.Transitions; i++)
			{
				if (formulaEvaluator(i))
					precalculatedTransitionTargets[i] |= PrecalculatedTransitionTarget.Satisfied;
			}
		}

		private void CalculateExcludedTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, Func<int, bool> formulaEvaluator)
		{
			for (var i = 0; i < LabeledMarkovChain.Transitions; i++)
			{
				if (formulaEvaluator(i))
					precalculatedTransitionTargets[i] |= PrecalculatedTransitionTarget.Excluded;
			}
		}

		private double[] CreateDerivedVector(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, PrecalculatedTransitionTarget flagToLookFor)
		{
			var derivedVector = new double[LabeledMarkovChain.Transitions];

			for (var i = 0; i < LabeledMarkovChain.Transitions; i++)
			{
				if (precalculatedTransitionTargets[i].HasFlag(flagToLookFor))
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

		private PrecalculatedTransitionTarget[] CreatePrecalculatedTransitionTargets(Formula phi, Formula psi)
		{
			var psiEvaluator = LabeledMarkovChain.CreateFormulaEvaluator(psi);

			var precalculatedTransitionTargets = CreateEmptyPrecalculatedTransitionTargetArray();

			CalculateSatisfiedTargets(precalculatedTransitionTargets, psiEvaluator);
			if (phi != null)
			{
				// excludedStates = Sat(\phi) \Cup Sat(psi)
				var phiEvaluator = LabeledMarkovChain.CreateFormulaEvaluator(phi);
				Func<int, bool> calculateExcludedStates = target =>
				{
					if (precalculatedTransitionTargets[target] == PrecalculatedTransitionTarget.Satisfied)
						return false; //satisfied states are never excluded
					if (!phiEvaluator(target))
						return true; //exclude state if it does not satisfy phi
					return false;
				};
				CalculateExcludedTargets(precalculatedTransitionTargets, calculateExcludedStates);
			}
			return precalculatedTransitionTargets;
		}

		private double CalculateProbabilityToReachStateFormulaInBoundedSteps(Formula phi, Formula psi, int steps)
		{
			// Pr[phi U psi]
			// calculate P [true U<=steps psi]

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var precalculatedTransitionTargets = CreatePrecalculatedTransitionTargets(phi, psi);

			var stateCount = LabeledMarkovChain.SourceStates.Count;
			
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
						if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.Satisfied))
						{
							sum += enumerator.CurrentProbability;
						}
						else if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.Excluded))
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

			Formula phi;
			Formula psi;
			int? steps;
			ExtractPsiPhiAndBoundFromFormula(formulaToCheck, out phi, out psi, out steps);
			
			double result;

			if (steps.HasValue)
			{
				result = CalculateProbabilityToReachStateFormulaInBoundedSteps(phi, psi, steps.Value);
			}
			else
			{
				throw new NotImplementedException();
			}
			
			stopwatch.Stop();

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(result);
		}

		private void ExtractPsiPhiAndBoundFromFormula(Formula formulaToCheck, out Formula phi, out Formula psi, out int? steps)
		{
			// [phi U<=steps psi]

			var unboundUnaryFormula = formulaToCheck as UnaryFormula;
			var boundedUnaryFormula = formulaToCheck as BoundedUnaryFormula;
			var unboundBinaryFormula = formulaToCheck as BinaryFormula;
			var boundedBinaryFormula = formulaToCheck as BoundedBinaryFormula;

			if (unboundUnaryFormula != null && unboundUnaryFormula.Operator == UnaryOperator.Finally)
			{
				phi = null;
				psi = unboundUnaryFormula.Operand;
				steps = null;
				return;
			}
			if (boundedUnaryFormula != null && boundedUnaryFormula.Operator == UnaryOperator.Finally)
			{
				phi = null;
				psi = boundedUnaryFormula.Operand;
				steps = boundedUnaryFormula.Bound;
				return;
			}
			if (unboundBinaryFormula != null && unboundBinaryFormula.Operator == BinaryOperator.Until)
			{
				phi = unboundBinaryFormula.LeftOperand;
				psi = unboundBinaryFormula.RightOperand;
				steps = null;
				return;
			}
			if (boundedBinaryFormula != null && boundedBinaryFormula.Operator == BinaryOperator.Until)
			{
				phi = boundedBinaryFormula.LeftOperand;
				psi = boundedBinaryFormula.RightOperand;
				steps = boundedBinaryFormula.Bound;
				return;
			}

			throw new NotImplementedException();
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
