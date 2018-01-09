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

	public class BuiltinLtmcModelChecker : LtmcModelChecker
	{
		[Flags]
		internal enum PrecalculatedTransitionTarget : byte
		{
			Nothing = 0,
			SatisfiedDirect = 1,
			ExcludedDirect = 2,
			SatisfiedFinally = 4,
			ExcludedFinally = 8,
			Mark = 16,
		}

		private LabeledTransitionMarkovChain.UnderlyingDigraph _underlyingDigraph;

		// Note: Should be used with using(var modelchecker = new ...)
		public BuiltinLtmcModelChecker(LabeledTransitionMarkovChain markovChain, TextWriter output = null)
			: base(markovChain, output)
		{
			Requires.That(true, "Need CompactStateStorage to use this model checker");
			LabeledMarkovChain.AssertIsDense();
			output.WriteLine("Initializing Built-in Ltmc Model checker");
		}

		private PrecalculatedTransitionTarget[] CreateEmptyPrecalculatedTransitionTargetArray()
		{
			PrecalculatedTransitionTarget[] outputTargets = new PrecalculatedTransitionTarget[LabeledMarkovChain.Transitions];
			for (var i = 0L; i < LabeledMarkovChain.Transitions; i++)
			{
				outputTargets[i] = PrecalculatedTransitionTarget.Nothing;
			}
			return outputTargets;
		}

		private void CalculateSatisfiedTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, Func<long, bool> formulaEvaluator)
		{
			for (var i = 0L; i < LabeledMarkovChain.Transitions; i++)
			{
				if (formulaEvaluator(i))
					precalculatedTransitionTargets[i] |= PrecalculatedTransitionTarget.SatisfiedDirect;
			}
		}

		private void CalculateExcludedTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, Func<long, bool> formulaEvaluator)
		{
			for (var i = 0L; i < LabeledMarkovChain.Transitions; i++)
			{
				if (formulaEvaluator(i))
					precalculatedTransitionTargets[i] |= PrecalculatedTransitionTarget.ExcludedDirect;
			}
		}

		private PrecalculatedTransitionTarget[] CreateSimplePrecalculatedTransitionTargets(Formula phi, Formula psi)
		{
			var psiEvaluator = LabeledMarkovChain.CreateFormulaEvaluator(psi);

			var precalculatedTransitionTargets = CreateEmptyPrecalculatedTransitionTargetArray();

			CalculateSatisfiedTargets(precalculatedTransitionTargets, psiEvaluator);
			if (phi != null)
			{
				// excludedStates = Sat(\phi) \Cup Sat(psi)
				var phiEvaluator = LabeledMarkovChain.CreateFormulaEvaluator(phi);
				Func<long, bool> calculateExcludedStates = target =>
				{
					if (precalculatedTransitionTargets[target] == PrecalculatedTransitionTarget.SatisfiedDirect)
						return false; //satisfied states are never excluded
					if (!phiEvaluator(target))
						return true; //exclude state if it does not satisfy phi
					return false;
				};
				CalculateExcludedTargets(precalculatedTransitionTargets, calculateExcludedStates);
			}
			return precalculatedTransitionTargets;
		}

		private double CalculateFinalBoundedProbability(double[] initialStateProbabilities, PrecalculatedTransitionTarget[] precalculatedTransitionTargets)
		{
			var finalProbability = 0.0;

			var enumerator = LabeledMarkovChain.GetInitialDistributionEnumerator();

			while (enumerator.MoveNext())
			{
				var transitionTarget = enumerator.CurrentIndex;
				if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.SatisfiedDirect))
				{
					finalProbability += enumerator.CurrentProbability;
				}
				else if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.ExcludedDirect))
				{
				}
				else
				{
					finalProbability += enumerator.CurrentProbability * initialStateProbabilities[enumerator.CurrentTargetState];
				}
			}
			return finalProbability;
		}

		private double CalculateBoundedProbability(Formula phi, Formula psi, int steps)
		{
			// Pr[phi U psi]
			// calculate P [true U<=steps psi]

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var precalculatedTransitionTargets = CreateSimplePrecalculatedTransitionTargets(phi, psi);

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
						if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.SatisfiedDirect))
						{
							sum += enumerator.CurrentProbability;
						}
						else if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.ExcludedDirect))
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
					var currentProbability = CalculateFinalBoundedProbability(xnew,precalculatedTransitionTargets);
					_output?.WriteLine($"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}

			var finalProbability=CalculateFinalBoundedProbability(xnew, precalculatedTransitionTargets);
			
			stopwatch.Stop();
			return finalProbability;
		}
		
		private void SetFlagInUnmarkedTransitionTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, PrecalculatedTransitionTarget flagToSet)
		{
			for (var i = 0L; i < LabeledMarkovChain.Transitions; i++)
			{
				if (precalculatedTransitionTargets[i].HasFlag(PrecalculatedTransitionTarget.Mark))
				{
					precalculatedTransitionTargets[i] = precalculatedTransitionTargets[i] & (~PrecalculatedTransitionTarget.Mark);
				}
				else
				{
					precalculatedTransitionTargets[i] = precalculatedTransitionTargets[i] | flagToSet;
				}
			}
		}

		private void CalculateUnderlyingDigraph()
		{
			// We use the underlying digraph to interfere the transitionTargets with the final probability
			// of 0 or 1.
			// I think, the data from the graph is also valid for the states. So, if a state-node
			// is in Prob0 or Prob1, then we can also assume that the state has always probability 0 or 1,
			// respectively. One further check can could be introduced. When we know that the probability
			// of a state is 0 or 1, then we do not have to check the outgoing transitionTargets anymore.
			if (_underlyingDigraph != null)
				return;
			_output.WriteLine("Creating underlying digraph");
			_underlyingDigraph = LabeledMarkovChain.CreateUnderlyingDigraph();
			_output.WriteLine("Finished creating underlying digraph");
		}

		private IEnumerable<long> GetAllTransitionTargetIndexesWithFlag(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, PrecalculatedTransitionTarget flag)
		{
			for (var i = 0L; i < precalculatedTransitionTargets.Length; i++)
			{
				if (precalculatedTransitionTargets[i].HasFlag(flag))
					yield return i;
			}
		}

		private void CalculateProb0TransitionTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets)
		{
			CalculateUnderlyingDigraph();

			var targetTransitionTargets = GetAllTransitionTargetIndexesWithFlag(precalculatedTransitionTargets, PrecalculatedTransitionTarget.SatisfiedDirect);

			Action<long> setFlagForTransitionTarget =
				(index) => precalculatedTransitionTargets[index] |= PrecalculatedTransitionTarget.Mark;

			Func<long, bool> transitionTargetsToIgnore =
				(index) => precalculatedTransitionTargets[index].HasFlag(PrecalculatedTransitionTarget.ExcludedDirect);
			
			_underlyingDigraph.BackwardTraversal(targetTransitionTargets, setFlagForTransitionTarget, transitionTargetsToIgnore);

			SetFlagInUnmarkedTransitionTargets(precalculatedTransitionTargets, PrecalculatedTransitionTarget.ExcludedFinally);
		}

		private void CalculateProb1TransitionTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets)
		{
			// Need to know Prob0TransitionTargets first

			var targetTransitionTargets = GetAllTransitionTargetIndexesWithFlag(precalculatedTransitionTargets, PrecalculatedTransitionTarget.ExcludedFinally);

			Action<long> setFlagForTransitionTarget =
				(index) => precalculatedTransitionTargets[index] |= PrecalculatedTransitionTarget.Mark;

			Func<long, bool> transitionTargetsToIgnore =
				(index) =>
				precalculatedTransitionTargets[index].HasFlag(PrecalculatedTransitionTarget.ExcludedDirect) ||
				precalculatedTransitionTargets[index].HasFlag(PrecalculatedTransitionTarget.SatisfiedDirect);

			_underlyingDigraph.BackwardTraversal(targetTransitionTargets, setFlagForTransitionTarget, transitionTargetsToIgnore);

			SetFlagInUnmarkedTransitionTargets(precalculatedTransitionTargets, PrecalculatedTransitionTarget.SatisfiedFinally);
		}

		private double CalculateFinalUnboundedProbability(double[] initialStateProbabilities, PrecalculatedTransitionTarget[] precalculatedTransitionTargets)
		{
			var finalProbability = 0.0;

			var enumerator = LabeledMarkovChain.GetInitialDistributionEnumerator();

			while (enumerator.MoveNext())
			{
				var transitionTarget = enumerator.CurrentIndex;
				if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.SatisfiedFinally))
				{
					finalProbability += enumerator.CurrentProbability;
				}
				else if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.ExcludedFinally))
				{
				}
				else
				{
					finalProbability += enumerator.CurrentProbability * initialStateProbabilities[enumerator.CurrentTargetState];
				}
			}
			return finalProbability;
		}


		private double CalculateUnboundUntil(Formula phi, Formula psi, int iterationsLeft)
		{
			// Based on the iterative idea by:
			// On algorithmic verification methods for probabilistic systems (1998) by Christel Baier
			// Theorem 3.1.6 (page 36)
			// http://wwwneu.inf.tu-dresden.de/content/institutes/thi/algi/publikationen/texte/15_98.pdf

			// Pr[phi U psi]
			// calculate P [true U<=steps psi]

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var fixPointReached = iterationsLeft <= 0;

			var precalculatedTransitionTargets = CreateSimplePrecalculatedTransitionTargets(phi, psi);
			CalculateProb0TransitionTargets(precalculatedTransitionTargets);
			CalculateProb1TransitionTargets(precalculatedTransitionTargets);

			var stateCount = LabeledMarkovChain.SourceStates.Count;

			var xold = new double[stateCount];
			var xnew = new double[stateCount];
			var loops = 0;
			while (!fixPointReached)
			{
				// switch xold and xnew
				var xtemp = xold;
				xold = xnew;
				xnew = xtemp;
				iterationsLeft--;
				loops++;
				for (var i = 0; i < stateCount; i++)
				{
					var enumerator = LabeledMarkovChain.GetTransitionEnumerator(i);
					var sum = 0.0;

					while (enumerator.MoveNext())
					{
						var transitionTarget = enumerator.CurrentIndex;
						if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.SatisfiedFinally))
						{
							sum += enumerator.CurrentProbability;
						}
						else if (precalculatedTransitionTargets[transitionTarget].HasFlag(PrecalculatedTransitionTarget.ExcludedFinally))
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
					var currentProbability = CalculateFinalUnboundedProbability(xnew,precalculatedTransitionTargets);
					_output?.WriteLine($"{loops} Fixpoint Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
				if (iterationsLeft <= 0)
					fixPointReached = true;
			}

			var finalProbability = CalculateFinalUnboundedProbability(xnew,precalculatedTransitionTargets);

			stopwatch.Stop();
			return finalProbability;
		}
		

		public override Probability CalculateProbability(Formula formulaToCheck)
		{
			_output.WriteLine($"Checking formula: {formulaToCheck}");
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Formula phi;
			Formula psi;
			int? steps;
			ExtractPsiPhiAndBoundedFromFormula(formulaToCheck, out phi, out psi, out steps);
			
			double result;

			if (steps.HasValue)
			{
				result = CalculateBoundedProbability(phi, psi, steps.Value);
			}
			else
			{
				var maxIterations = 50;
				result = CalculateUnboundUntil(phi, psi, maxIterations);
			}
			
			stopwatch.Stop();

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(result);
		}

		private void ExtractPsiPhiAndBoundedFromFormula(Formula formulaToCheck, out Formula phi, out Formula psi, out int? steps)
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
