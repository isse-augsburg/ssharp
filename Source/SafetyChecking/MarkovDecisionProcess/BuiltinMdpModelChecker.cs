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

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using Modeling;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using Formula;
	using Utilities;

	class BuiltinMdpModelChecker : MdpModelChecker
	{
		private MarkovDecisionProcess.UnderlyingDigraph _underlyingDigraph;

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

		// Note: Should be used with using(var modelchecker = new ...)
		public BuiltinMdpModelChecker(MarkovDecisionProcess mdp, TextWriter output = null)
			: base(mdp, output)
		{
			_underlyingDigraph = MarkovDecisionProcess.CreateUnderlyingDigraph();
		}

		internal Dictionary<int, bool> CalculateSatisfiedStates(Func<int, bool> formulaEvaluator)
		{
			var satisfiedStates = new Dictionary<int, bool>();
			for (var i = 0; i < MarkovDecisionProcess.States; i++)
			{
				if (formulaEvaluator(i))
					satisfiedStates.Add(i, true);
			}
			return satisfiedStates;
		}

		public double CalculateMinimumFinalProbability(double[] initialStateProbabilities)
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

		internal double CalculateMaximumFinalProbability(double[] initialStateProbabilities)
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

		internal double[] MinimumIterator(Dictionary<int, bool> exactlyOneStates, Dictionary<int, bool> exactlyZeroStates, int steps)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			var stateCount = MarkovDecisionProcess.States;
			var enumerator = MarkovDecisionProcess.GetEnumerator();

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

			var stateCount = MarkovDecisionProcess.States;
			var enumerator = MarkovDecisionProcess.GetEnumerator();

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
			var adjustedSteps = AdjustNumberOfStepsForFactor(steps);

			var psiEvaluator = MarkovDecisionProcess.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>(); // change for \phi Until \psi

			var xnew = MinimumIterator(directlySatisfiedStates, excludedStates, adjustedSteps);

			var finalProbability = CalculateMinimumFinalProbability(xnew);

			return finalProbability;
		}

		internal double CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps(Formula psi, int steps)
		{
			var adjustedSteps = AdjustNumberOfStepsForFactor(steps);

			var psiEvaluator = MarkovDecisionProcess.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>(); // change for \phi Until \psi
			
			var xnew = MaximumIterator(directlySatisfiedStates, excludedStates, adjustedSteps);

			var finalProbability = CalculateMaximumFinalProbability(xnew);

			return finalProbability;
		}

		public Dictionary<int, bool> StatesReachableWithProbabilityExactlyZeroWithAllSchedulers(Dictionary<int, bool> directlySatisfiedStates,
																								Dictionary<int, bool> excludedStates)
		{
			// calculate probabilityExactlyZero (prob0a). No matter which scheduler is selected, the probability
			// of the resulting states is zero.
			// This is exact

			// The idea of the algorithm is to calculate probabilityGreaterThanZero
			//     all states where there _exists_ a scheduler such that a directlySatisfiedState
			//     might be reached with a probability > 0.
			//     This is simply the set of all ancestors of directlySatisfiedStates.
			// The complement of probabilityGreaterThanZero is the set of states where _all_ adversaries have a probability
			// to reach a directlySatisfiedState is exactly 0.
			Func<int, bool> nodesToIgnore =
				excludedStates.ContainsKey;

			// based on DFS https://en.wikipedia.org/wiki/Depth-first_search
			var ancestors = new Dictionary<int, bool>();
			var nodesToTraverse = new Stack<int>();
			foreach (var node in directlySatisfiedStates)
			{
				nodesToTraverse.Push(node.Key);
			}

			while (nodesToTraverse.Count > 0)
			{
				var currentNode = nodesToTraverse.Pop();
				var isIgnored = nodesToIgnore(currentNode);
				var alreadyDiscovered = ancestors.ContainsKey(currentNode);
				if (!(isIgnored || alreadyDiscovered))
				{
					ancestors.Add(currentNode, true);
					foreach (var inEdge in _underlyingDigraph.BaseGraph.InEdges(currentNode))
					{
						nodesToTraverse.Push(inEdge.Source);
					}
				}
			}
			var probabilityGreaterThanZero = ancestors;
			// alternatively: var probabilityGreaterThanZero = _underlyingDigraph.BaseGraph.GetAncestors(directlySatisfiedStates, nodesToIgnore);


			var probabilityExactlyZero = CreateComplement(probabilityGreaterThanZero);
			return probabilityExactlyZero;
		}


		public Dictionary<int, bool> StatesReachableWithProbabilityExactlyZeroForAtLeastOneScheduler(
			Dictionary<int, bool> directlySatisfiedStates, Dictionary<int, bool> excludedStates)
		{
			// calculate probabilityExactlyZero (prob0e). There exists a scheduler, for which the probability of
			// the resulting states is zero. The result may be different for another scheduler, but at least there exists one.
			// This is exact

			Dictionary<int, bool> ancestorsFound = null;
			var probabilityGreaterThanZero = directlySatisfiedStates; //we know initially this is satisfied

			var mdpEnumerator = MarkovDecisionProcess.GetEnumerator();
			// The idea of the algorithm is to calculate probabilityGreaterThanZero:
			//     all states where a directlySatisfiedState is reached with a probability > 0
			//     no matter which scheduler is selected (valid for _all_ adversaries).
			// The complement of probabilityGreaterThanZero is the set of states where a scheduler _exists_ for
			//     which the probability to reach a directlySatisfiedState is exactly 0.
			Func<int, bool> nodesToIgnore = source =>
			{
				//nodes found by UpdateAncestors are always SourceNodes of a edge to an ancestor in ancestorsFound
				if (excludedStates.ContainsKey(source))
					return false; //source must not be ignored
				if (directlySatisfiedStates.ContainsKey(source))
					return false; //source must not be ignored

				// must not be cached (, because ancestorsFound might change, even in the same iteration)!!!
				// check if _all_ distributions of source contain at least transition to a ancestor in ancestorsFound
				mdpEnumerator.SelectSourceState(source);
				while (mdpEnumerator.MoveNextDistribution())
				{
					var foundInDistribution = false;
					while (mdpEnumerator.MoveNextTransition() && !foundInDistribution)
					{
						if (ancestorsFound.ContainsKey(mdpEnumerator.CurrentTransition.Column))
							foundInDistribution = true;
					}
					if (!foundInDistribution)
						return true; // the distribution does not have a targetState in ancestorsFound, so source must be ignored
				}
				return false; //source must not be ignored
			};

			// initialize probabilityGreaterThanZero to the states where we initially know the probability is greater than zero
			var fixpointReached = false;

			while (!fixpointReached)
			{
				// Calculate fix point of probabilityGreaterThanZero
				// Should be finished in one iteration, but I do have not proved it yet, so repeat it until fixpoint is reached for sure.
				// (The proof relies on details of the algorithm GetAncestors. Intuition: When a state s was not added to the set of
				//  ancestors it is because one distribution d' has no target state in the ancestors found yet. If the state is in the
				//  final set of ancestors, the reason is that the state s' of the distribution d', which was responsible for declining
				//  s has not yet been added to ancestors. When s' is added all its ancestors are traversed again and s is found.)
				// Note:
				//   UpdateAncestors must be used, because nodesToIgnore requires access to the current information about the ancestors
				//   (ancestorsFound), if it should work in one iteration.
				ancestorsFound = new Dictionary<int, bool>();
					//Note: We reuse ancestorsFound, which is also known and used by nodesToIgnore. The side effects are on purpose.
				// based on DFS https://en.wikipedia.org/wiki/Depth-first_search
				var nodesToTraverse = new Stack<int>();
				foreach (var node in probabilityGreaterThanZero)
				{
					nodesToTraverse.Push(node.Key);
				}

				while (nodesToTraverse.Count > 0)
				{
					var currentNode = nodesToTraverse.Pop();
					var isIgnored = nodesToIgnore(currentNode);
					var alreadyDiscovered = ancestorsFound.ContainsKey(currentNode);
					if (!(isIgnored || alreadyDiscovered))
					{
						ancestorsFound.Add(currentNode, true);
						foreach (var inEdge in _underlyingDigraph.BaseGraph.InEdges(currentNode))
						{
							nodesToTraverse.Push(inEdge.Source);
						}
					}
				}

				if (probabilityGreaterThanZero.Count == ancestorsFound.Count)
					fixpointReached = true;
				probabilityGreaterThanZero = ancestorsFound;
			}

			var probabilityExactlyZero = CreateComplement(probabilityGreaterThanZero);
			return probabilityExactlyZero;
		}

		public Dictionary<int, bool> StatesReachableWithProbabilityExactlyOneForAtLeastOneScheduler(Dictionary<int, bool> directlySatisfiedStates,
																									Dictionary<int, bool> excludedStates)
		{
			// calculate probabilityExactlyOne (prob1e). There exists a scheduler, for which the probability of
			// the resulting states is exactly 1. The result may be different for another scheduler, but at least there exists one.
			// This is exact

			// The algorithm works this way: It looks at a set of states probabilityMightBeExactlyOne which are initially all states.
			// Then it iterates until a fixpoint is found. In each iteration states are removed from probabilityMightBeExactlyOne for
			// which a scheduler _must_ switch to a state where the probability is < 1.
			// The removal process works this way: In each iteration a backwards search is started.
			// A distribution from a predecessor is removed, if not every transition of the distribution leads to a
			// state in probabilityMightBeExactlyOne (Reason: It is possible from there to go to a state where probability < 1).
			// The fixpoint is the result.

			Func<int, bool> nodesToIgnore = excludedStates.ContainsKey;
			var probabilityMightBeExactlyOne = CreateComplement(new Dictionary<int, bool>()); //all states

			var _isDistributionIncludedCache = new Dictionary<int, bool>();
			var mdpEnumerator = MarkovDecisionProcess.GetEnumerator();
			Action resetDistributionIncludedCacheForNewIteration = () =>
			{
				// One possible optimization.
				// Only true entries must be deleted because the eligible distributions get less and less each iteration
				// On the other hand: Clearing the whole data structure makes the dictionary smaller and access faster.
				_isDistributionIncludedCache.Clear();
			};
			Func<int, bool> isDistributionIncluded = rowOfDistribution =>
			{
				if (_isDistributionIncludedCache.ContainsKey(rowOfDistribution))
					return _isDistributionIncludedCache[rowOfDistribution];

				mdpEnumerator.MoveToDistribution(rowOfDistribution);
				var includeDistribution = true;
				while (includeDistribution && mdpEnumerator.MoveNextTransition())
				{
					var targetState = mdpEnumerator.CurrentTransition.Column;
					// if targetstate is not found in probabilityMightBeExactlyOne then the complete distribution has to be removed
					if (!probabilityMightBeExactlyOne.ContainsKey(targetState))
						includeDistribution = false;
				}
				return includeDistribution;
			};

			var fixpointReached = false;
			while (!fixpointReached)
			{
				resetDistributionIncludedCacheForNewIteration();
				var ancestorsFound = new Dictionary<int, bool>(); //Note: ancestorsFound must not be reused

				// based on DFS https://en.wikipedia.org/wiki/Depth-first_search
				var nodesToTraverse = new Stack<int>();
				foreach (var node in directlySatisfiedStates)
				{
					nodesToTraverse.Push(node.Key);
				}

				while (nodesToTraverse.Count > 0)
				{
					var currentNode = nodesToTraverse.Pop();
					var isIgnored = nodesToIgnore(currentNode);
					var alreadyDiscovered = ancestorsFound.ContainsKey(currentNode);
					if (!(isIgnored || alreadyDiscovered))
					{
						ancestorsFound.Add(currentNode, true);
						foreach (var inEdge in _underlyingDigraph.BaseGraph.InEdges(currentNode))
						{
							if (isDistributionIncluded(inEdge.Data.RowOfDistribution))
								nodesToTraverse.Push(inEdge.Source);
						}
					}
				}

				if (probabilityMightBeExactlyOne.Count == ancestorsFound.Count)
					fixpointReached = true;
				Assert.That(probabilityMightBeExactlyOne.Count >= ancestorsFound.Count,"bug!");
				probabilityMightBeExactlyOne = ancestorsFound;
			}

			return probabilityMightBeExactlyOne;
		}

		public Dictionary<int, bool> SubsetOfStatesReachableWithProbabilityExactlyOneWithAllSchedulers(Dictionary<int, bool> directlySatisfiedStates, Dictionary<int, bool> excludedStates)
		{
			// calculate probabilityExactlyOne (prob1a). No matter which scheduler is selected, the probability
			// of the resulting states is exactly 1.
			// This is only a subset. More states with this property may be possible
			//better precalculation for mtbdds: https://github.com/prismmodelchecker/prism-svn/blob/master/prism/src/mtbdd/PM_Prob1A.cc
			var exactlyOneStates = directlySatisfiedStates;
			return exactlyOneStates;
		}

		private double CalculateMaximumProbabilityToReachStateFormula(Formula psi)
		{
			// same algorithm as CalculateMaximumProbabilityToReachStateFormulaInBoundedSteps with different
			// directlySatisfiedStates and excludedStates
			var maxSteps = AdjustNumberOfStepsForFactor(50);

			var psiEvaluator = MarkovDecisionProcess.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>();  // change for \phi Until \psi

			var exactlyZeroStates = StatesReachableWithProbabilityExactlyZeroWithAllSchedulers(directlySatisfiedStates,excludedStates);
			var exactlyOneStates = StatesReachableWithProbabilityExactlyOneForAtLeastOneScheduler(directlySatisfiedStates, excludedStates); //cannot perform a better pre calculation

			var xnew = MaximumIterator(exactlyOneStates, exactlyZeroStates, maxSteps);

			var finalProbability = CalculateMaximumFinalProbability(xnew);

			return finalProbability;
		}



		internal double CalculateMinimumProbabilityToReachStateFormula(Formula psi)
		{
			// same algorithm as CalculateMinimumProbabilityToReachStateFormulaInBoundedSteps with different
			// directlySatisfiedStates and excludedStates
			var maxSteps = AdjustNumberOfStepsForFactor(50);

			var psiEvaluator = MarkovDecisionProcess.CreateFormulaEvaluator(psi);

			var directlySatisfiedStates = CalculateSatisfiedStates(psiEvaluator);
			var excludedStates = new Dictionary<int, bool>();  // change for \phi Until \psi

			var exactlyZeroStates = StatesReachableWithProbabilityExactlyZeroForAtLeastOneScheduler(directlySatisfiedStates, excludedStates);
			var exactlyOneStates = SubsetOfStatesReachableWithProbabilityExactlyOneWithAllSchedulers(directlySatisfiedStates, excludedStates); // this algorithm is only an approximation

			var xnew = MinimumIterator(exactlyOneStates, exactlyZeroStates, maxSteps);

			var finalProbability = CalculateMinimumFinalProbability(xnew);

			return finalProbability;
		}

		private int AdjustNumberOfStepsForFactor(int usualNoOfSteps)
		{
			var adjustmentForUsualSteps = usualNoOfSteps * MarkovDecisionProcess.FactorForBoundedAnalysis;
			// The former initial step is now divided into the 1 initial mdp state and (FactorForBoundedAnalysis-1) normal mdp states
			var adjustmentForInitialSteps = MarkovDecisionProcess.FactorForBoundedAnalysis -1;
			return adjustmentForUsualSteps+adjustmentForInitialSteps;
		}

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

			_output?.WriteLine($"Built-in probabilistic model checker model checking time: {stopwatch.Elapsed}");
			return new Probability(result);
		}*/

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
				minResult = CalculateMinimumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
				maxResult = CalculateMaximumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
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
				minResult = CalculateMinimumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
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
				maxResult = CalculateMaximumProbabilityToReachStateFormula(finallyUnboundFormula.Operand);
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
