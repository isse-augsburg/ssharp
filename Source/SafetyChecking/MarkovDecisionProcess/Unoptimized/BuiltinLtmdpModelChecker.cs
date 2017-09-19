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

	class BuiltinLtmdpModelChecker : LtmdpModelChecker
	{
		public bool CacheCidValues { get; set; } = true;

		internal enum PrecalculatedTransitionTarget : byte
		{
			Nothing = 0,
			Satisfied = 1,
			Excluded = 2,
		}
		
		// Note: Should be used with using(var modelchecker = new ...)
		public BuiltinLtmdpModelChecker(LabeledTransitionMarkovDecisionProcess mdp, TextWriter output = null)
			 : base(mdp, output)
		{
			Requires.That(true, "Need CompactStateStorage to use this model checker");
			mdp.AssertIsDense();
		}

		private PrecalculatedTransitionTarget[] CreateEmptyPrecalculatedTransitionTargetArray()
		{
			PrecalculatedTransitionTarget[] outputTargets = new PrecalculatedTransitionTarget[Ltmdp.TransitionTargets];
			for (var i = 0; i < Ltmdp.TransitionTargets; i++)
			{
				outputTargets[i] = PrecalculatedTransitionTarget.Nothing;
			}
			return outputTargets;
		}
		
		private void CalculateSatisfiedTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, Func<int, bool> formulaEvaluator)
		{
			for (var i = 0; i < Ltmdp.TransitionTargets; i++)
			{
				if (formulaEvaluator(i))
					precalculatedTransitionTargets[i] |= PrecalculatedTransitionTarget.Satisfied;
			}
		}

		private void CalculateExcludedTargets(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, Func<int, bool> formulaEvaluator)
		{
			for (var i = 0; i < Ltmdp.TransitionTargets; i++)
			{
				if (formulaEvaluator(i))
					precalculatedTransitionTargets[i] |= PrecalculatedTransitionTarget.Excluded;
			}
		}

		private double[] CreateDerivedVector(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, PrecalculatedTransitionTarget flagToLookFor)
		{
			var derivedVector = new double[Ltmdp.TransitionTargets];

			for (var i = 0; i < Ltmdp.TransitionTargets; i++)
			{
				if (precalculatedTransitionTargets[i].HasFlag(flagToLookFor))
					derivedVector[i] = 1.0;
				else
					derivedVector[i] = 0.0;
			}
			return derivedVector;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool HasCacheEntry(AutoResizeBigVector<double> cache, long entry)
		{
			return !double.IsNaN(cache[(int)entry]);
		}

		private double CalculateMinimumProbabilityOfCid(AutoResizeBigVector<double> cache, PrecalculatedTransitionTarget[] precalculatedTransitionTargets, double[] stateProbabilities, long currentCid)
		{
			if (cache != null && HasCacheEntry(cache, currentCid))
				return cache[(int)currentCid];
			var result = double.NaN;
			LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement cge = Ltmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var transitionTargetPosition = cge.To;
				var transitionTarget = Ltmdp.GetTransitionTarget(transitionTargetPosition);

				if (precalculatedTransitionTargets[transitionTargetPosition].HasFlag(PrecalculatedTransitionTarget.Satisfied))
				{
					result = 1.0;
				}
				else if (precalculatedTransitionTargets[transitionTargetPosition].HasFlag(PrecalculatedTransitionTarget.Excluded))
				{
					result = 0.0;
				}
				else
				{
					result = stateProbabilities[transitionTarget.TargetState];
				}
			}
			else
			{
				if (cge.IsChoiceTypeForward)
				{
					// Note, cgi.Probability is used in the branch "else if (cge.IsChoiceTypeProbabilitstic)"
					result = CalculateMinimumProbabilityOfCid(cache, precalculatedTransitionTargets, stateProbabilities, cge.From);
				}
				if (cge.IsChoiceTypeNondeterministic)
				{
					var smallest = double.PositiveInfinity;
					for (var i = cge.From; i <= cge.To; i++)
					{
						var resultOfChild = CalculateMinimumProbabilityOfCid(cache, precalculatedTransitionTargets, stateProbabilities, i);
						if (resultOfChild < smallest)
							smallest = resultOfChild;
					}
					result = smallest;
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					var sum = 0.0;
					for (var i = cge.From; i <= cge.To; i++)
					{
						var transitionProbability = Ltmdp.GetContinuationGraphElement(i).Probability;
						var resultOfChild = CalculateMinimumProbabilityOfCid(cache, precalculatedTransitionTargets, stateProbabilities, i);
						sum += transitionProbability * resultOfChild;
					}
					result = sum;
				}
			}
			if (cache != null)
				cache[(int)currentCid] = result;
			return result;
		}

		private double CalculateMaximumProbabilityOfCid(AutoResizeBigVector<double> cache, PrecalculatedTransitionTarget[] precalculatedTransitionTargets, double[] stateProbabilities, long currentCid)
		{
			if (cache != null && HasCacheEntry(cache, currentCid))
				return cache[(int)currentCid];
			var result = double.NaN;
			LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement cge = Ltmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var transitionTargetPosition = cge.To;
				var transitionTarget = Ltmdp.GetTransitionTarget(transitionTargetPosition);
				
				if (precalculatedTransitionTargets[transitionTargetPosition].HasFlag(PrecalculatedTransitionTarget.Satisfied))
				{
					result = 1.0;
				}
				else if (precalculatedTransitionTargets[transitionTargetPosition].HasFlag(PrecalculatedTransitionTarget.Excluded))
				{
					result = 0.0;
				}
				else
				{
					result = stateProbabilities[transitionTarget.TargetState];
				}
			}
			else
			{
				if (cge.IsChoiceTypeForward)
				{
					// Note, cgi.Probability is used in the branch "else if (cge.IsChoiceTypeProbabilitstic)"
					result = CalculateMaximumProbabilityOfCid(cache, precalculatedTransitionTargets, stateProbabilities, cge.From);
				}
				if (cge.IsChoiceTypeNondeterministic)
				{
					var biggest = double.NegativeInfinity;
					for (var i = cge.From; i <= cge.To; i++)
					{
						var resultOfChild = CalculateMaximumProbabilityOfCid(cache, precalculatedTransitionTargets, stateProbabilities, i);
						if (resultOfChild > biggest)
							biggest = resultOfChild;
					}
					result = biggest;
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					var sum = 0.0;
					for (var i = cge.From; i <= cge.To; i++)
					{
						var transitionProbability = Ltmdp.GetContinuationGraphElement(i).Probability;
						var resultOfChild = CalculateMaximumProbabilityOfCid(cache, precalculatedTransitionTargets, stateProbabilities, i);
						sum += transitionProbability * resultOfChild;
					}
					result = sum;
				}
			}
			if (cache != null)
				cache[(int)currentCid] = result;
			return result;
		}

		public double CalculateMinimumFinalProbability(AutoResizeBigVector<double> cache, PrecalculatedTransitionTarget[] precalculatedTransitionTargets,  double[] initialStateProbabilities)
		{
			var cid = Ltmdp.GetRootContinuationGraphLocationOfInitialState();
			cache?.Clear(cid); //use cid as offset, because it is the smallest element
			return CalculateMinimumProbabilityOfCid(cache, precalculatedTransitionTargets, initialStateProbabilities, cid);
		}

		internal double CalculateMaximumFinalProbability(AutoResizeBigVector<double> cache, PrecalculatedTransitionTarget[] precalculatedTransitionTargets, double[] initialStateProbabilities)
		{
			var cid = Ltmdp.GetRootContinuationGraphLocationOfInitialState();
			cache?.Clear(cid); //use cid as offset, because it is the smallest element
			return CalculateMaximumProbabilityOfCid(cache, precalculatedTransitionTargets, initialStateProbabilities, cid);
		}

		internal double[] MinimumIterator(AutoResizeBigVector<double> cache, PrecalculatedTransitionTarget[] precalculatedTransitionTargets, int steps)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var stateCount = Ltmdp.SourceStates.Count;

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
					var cid = Ltmdp.GetRootContinuationGraphLocationOfState(i);
					cache?.Clear(cid); //use cid as offset, because it is the smallest element
					xnew[i] = CalculateMinimumProbabilityOfCid(cache, precalculatedTransitionTargets, xold, cid);
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMinimumFinalProbability(cache, precalculatedTransitionTargets, xnew);
					_output?.WriteLine(
						$"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}
			stopwatch.Stop();
			return xnew;
		}

		internal double[] MaximumIterator(AutoResizeBigVector<double> cache, PrecalculatedTransitionTarget[] precalculatedTransitionTargets, int steps)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var stateCount = Ltmdp.SourceStates.Count;

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
					var cid = Ltmdp.GetRootContinuationGraphLocationOfState(i);
					cache?.Clear(cid); //use cid as offset, because it is the smallest element
					xnew[i] = CalculateMaximumProbabilityOfCid(cache, precalculatedTransitionTargets, xold, cid);
				}

				if (loops % 10 == 0)
				{
					stopwatch.Stop();
					var currentProbability = CalculateMaximumFinalProbability(cache, precalculatedTransitionTargets, xnew);
					_output?.WriteLine(
						$"{loops} Bounded Until iterations in {stopwatch.Elapsed}. Current probability={currentProbability.ToString(CultureInfo.InvariantCulture)}");
					stopwatch.Start();
				}
			}
			stopwatch.Stop();
			return xnew;
		}


		internal double CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, int steps)
		{
			var cache = CacheCidValues ? new AutoResizeBigVector<double> { DefaultValue = double.NaN } : null;

			var xnew = MinimumIterator(cache, precalculatedTransitionTargets, steps);

			var finalProbability = CalculateMinimumFinalProbability(cache, precalculatedTransitionTargets, xnew);

			return finalProbability;
		}

		internal double CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(PrecalculatedTransitionTarget[] precalculatedTransitionTargets, int steps)
		{
			var cache = CacheCidValues ? new AutoResizeBigVector<double> { DefaultValue = double.NaN } : null;

			var xnew = MaximumIterator(cache, precalculatedTransitionTargets, steps);

			var finalProbability = CalculateMaximumFinalProbability(cache, precalculatedTransitionTargets, xnew);

			return finalProbability;
		}


		internal override ProbabilityRange CalculateProbabilityRange(Formula formulaToCheck)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Formula phi;
			Formula psi;
			int? steps;
			ExtractPsiPhiAndBoundFromFormula(formulaToCheck, out phi, out psi, out steps);
			var precalculatedTransitionTargets = CreatePrecalculatedTransitionTargets(phi, psi);
			
			double minResult;
			double maxResult;

			if (steps.HasValue)
			{
				minResult = CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps(precalculatedTransitionTargets, steps.Value);
				maxResult = CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(precalculatedTransitionTargets, steps.Value);
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
			
			Formula phi;
			Formula psi;
			int? steps;
			ExtractPsiPhiAndBoundFromFormula(formulaToCheck, out phi, out psi, out steps);
			var precalculatedTransitionTargets = CreatePrecalculatedTransitionTargets(phi, psi);

			double minResult;

			if (steps.HasValue)
			{
				minResult = CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps(precalculatedTransitionTargets, steps.Value);
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
			
			Formula phi;
			Formula psi;
			int? steps;
			ExtractPsiPhiAndBoundFromFormula(formulaToCheck, out phi, out psi, out steps);
			var precalculatedTransitionTargets = CreatePrecalculatedTransitionTargets(phi, psi);

			double maxResult;

			if (steps.HasValue)
			{
				maxResult = CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(precalculatedTransitionTargets, steps.Value);
			}
			else
			{
				throw new NotImplementedException();
			}

			stopwatch.Stop();

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(maxResult);
		}

		private PrecalculatedTransitionTarget[] CreatePrecalculatedTransitionTargets(Formula phi, Formula psi)
		{
			// [phi U<=steps psi]

			var psiEvaluator = Ltmdp.CreateFormulaEvaluator(psi);

			var precalculatedTransitionTargets = CreateEmptyPrecalculatedTransitionTargetArray();

			CalculateSatisfiedTargets(precalculatedTransitionTargets, psiEvaluator);
			if (phi != null)
			{
				// excludedStates = Sat(\phi) \Cup Sat(psi)
				var phiEvaluator = Ltmdp.CreateFormulaEvaluator(phi);
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
