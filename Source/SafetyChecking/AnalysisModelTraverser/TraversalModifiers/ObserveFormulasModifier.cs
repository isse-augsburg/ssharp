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

namespace ISSE.SafetyChecking.AnalysisModelTraverser.TraversalModifiers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using Formula;
	using Utilities;

	/// <summary>
	///   Observe formulas and embed the result into the state space and on the transitions.
	/// </summary>
	internal sealed unsafe class ObserveFormulasModifier : ITransitionModifier
	{
		public int ExtraBytesInStateVector { get; } = sizeof(int);

		public int ExtraBytesOffset { get; set; }

		private readonly string[] _previousFormulaLabels;

		private Func<StateFormulaSet, int, bool>[] _newEnrichmentEvaluator;

		// Formula evaluator needs to know the labeling on the transition and the enrichment the _current_ state.
		// For an initial transition, the enrichment 0 should be provided
		private Func<StateFormulaSet, int, bool>[] _newFormulaEvaluators;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="previousFormulaLabels">formulas already existing in StateFormulaSet.</param>
		/// <param name="formulasToObserve">formulas to generate an observer for.</param>
		public ObserveFormulasModifier(string[] previousFormulaLabels, Formula[] formulasToObserve)
		{
			Assert.That(previousFormulaLabels.Length + formulasToObserve.Length <= 32, "Too many Formulas");

			_previousFormulaLabels = previousFormulaLabels;

			GenerateEvaluators(formulasToObserve);
		}

		private void GenerateEvaluators(Formula[] formulasToObserve)
		{
			_newEnrichmentEvaluator = formulasToObserve.Select(CreateEnrichmentEvaluator).ToArray();

			var newFormulaEvaluators = new List<Func<StateFormulaSet, int, bool>>();
			for (var i = 0; i < _previousFormulaLabels.Length; i++)
			{
				var newFormulaEvaluator = CreatePreviousFormulaEvaluator(i);
				newFormulaEvaluators.Add(newFormulaEvaluator);
			}
			for (var i = 0; i < formulasToObserve.Length; i++)
			{
				var newFormulaEvaluator = CreateFormulaToObserveEvaluator(i);
				newFormulaEvaluators.Add(newFormulaEvaluator);
			}
			_newFormulaEvaluators = newFormulaEvaluators.ToArray();
		}

		private Func<StateFormulaSet, int,  bool> CreateEnrichmentEvaluator(Formula formulaToObserve, int indexOfFormulaToObserve)
		{
			var isCurrentlySatisfiedEvaluator =
				StateFormulaSetEvaluatorCompilationVisitor.Compile(_previousFormulaLabels, formulaToObserve);

			Func<StateFormulaSet, int, bool> enrichmentEvaluator =
				(oldStateFormulaSet, oldEnrichment) =>
				{
					var isBitAlreadySet = (oldEnrichment & (1 << indexOfFormulaToObserve)) != 0;
					if (isBitAlreadySet)
						return true;
					var setNewBit = isCurrentlySatisfiedEvaluator(oldStateFormulaSet);
					return setNewBit;
				};
			
			return enrichmentEvaluator;
		}

		private Func<StateFormulaSet, int, bool> CreatePreviousFormulaEvaluator(int indexOfPreviousFormula)
		{
			Func<StateFormulaSet, int, bool> newFormulaEvaluator =
				(oldTargetStateFormulaSet, targetEnrichment) => oldTargetStateFormulaSet[indexOfPreviousFormula];
			return newFormulaEvaluator;
		}

		private Func<StateFormulaSet, int, bool> CreateFormulaToObserveEvaluator(int indexOfFormulaToObserve)
		{
			Func<StateFormulaSet, int, bool> newFormulaEvaluator =
				(oldTargetStateFormulaSet, targetEnrichment) => (targetEnrichment & (1 << indexOfFormulaToObserve)) != 0;
			return newFormulaEvaluator;
		}

		/// <summary>
		///   Embed evaluated once formulas into the state as observers.
		/// </summary>
		/// <param name="context">The context of the model traversal.</param>
		/// <param name="worker">The worker that found the transition.</param>
		/// <param name="transitions">The transitions that should be checked.</param>
		/// <param name="sourceState">The source state of the transitions.</param>
		/// <param name="sourceStateIndex">The unique index of the transition's source state.</param>
		/// <param name="isInitial">Indicates whether the transitions are initial transitions not starting in any valid source state.</param>
		public void ModifyTransitions(TraversalContext context, Worker worker, TransitionCollection transitions, byte* sourceState,
									  int sourceStateIndex, bool isInitial)
		{
			var currentEnrichment = isInitial ? 0 : *(int*)(sourceState + ExtraBytesOffset);

			foreach (CandidateTransition* transition in transitions)
				ConvertTransition(currentEnrichment, transition);
		}

		private void ConvertTransition(int currentEnrichment, CandidateTransition* transition)
		{
			var targetEnrichment = DeriveNewEnrichment(transition->Formulas, currentEnrichment);
			*(int*)(transition->TargetStatePointer + ExtraBytesOffset) = targetEnrichment;

			var targetStateFormulaSet = DeriveNewStateFormulaSet(transition->Formulas, targetEnrichment);
			transition->Formulas = targetStateFormulaSet;
		}

		private int DeriveNewEnrichment(StateFormulaSet oldStateFormulaSet, int oldEnrichments)
		{
			int newEnrichment = oldEnrichments;
			for (var i = 0; i < _newEnrichmentEvaluator.Length; i++)
			{
				var setNewBit = _newEnrichmentEvaluator[i](oldStateFormulaSet, oldEnrichments);
				if (setNewBit)
				{
					newEnrichment |= 1 << i;
				}
			}
			return newEnrichment;
		}

		private StateFormulaSet DeriveNewStateFormulaSet(StateFormulaSet oldStateFormulaSet, int newEnrichment)
		{
			var evaluatedStateFormulas = new bool[_newFormulaEvaluators.Length];
			for (var i = 0; i < _newFormulaEvaluators.Length; i++)
			{
				var setNewBit = _newFormulaEvaluators[i](oldStateFormulaSet, newEnrichment);
				evaluatedStateFormulas[i] = setNewBit;
			}
			return new StateFormulaSet(evaluatedStateFormulas);
		}
	}
}