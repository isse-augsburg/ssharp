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
	///   Plainly integrate formula into the state. With labeled transitions this is not really useful.
	///   But it serves for evaluation purposes.
	///   Thus we also do not introduce new transition labels.
	/// </summary>
	internal sealed unsafe class PlainlyIntegrateFormulaIntoStateModifier : ITransitionModifier
	{
		public int ExtraBytesInStateVector { get; } = sizeof(int);

		public int ExtraBytesOffset { get; set; }

		public int RelevantStateVectorSize { get; set; }

		private readonly string[] _previousFormulaLabels;

		private Func<StateFormulaSet, bool>[] _newEnrichmentEvaluator;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="previousFormulaLabels">formulas already existing in StateFormulaSet.</param>
		/// <param name="formulasToObserve">formulas to generate an observer for.</param>
		public PlainlyIntegrateFormulaIntoStateModifier(string[] previousFormulaLabels, Formula[] formulasToObserve)
		{
			Assert.That(previousFormulaLabels.Length + formulasToObserve.Length <= 32, "Too many Formulas");

			_previousFormulaLabels = previousFormulaLabels;

			GenerateEvaluators(formulasToObserve);
		}

		private void GenerateEvaluators(Formula[] formulasToObserve)
		{
			_newEnrichmentEvaluator = formulasToObserve.Select(CreateEnrichmentEvaluator).ToArray();
		}

		private Func<StateFormulaSet, bool> CreateEnrichmentEvaluator(Formula formulaToObserve, int indexOfFormulaToObserve)
		{
			var isCurrentlySatisfiedEvaluator =
				StateFormulaSetEvaluatorCompilationVisitor.Compile(_previousFormulaLabels, formulaToObserve);

			Func<StateFormulaSet, bool> enrichmentEvaluator =
				(oldStateFormulaSet) =>
				{
					var setNewBit = isCurrentlySatisfiedEvaluator(oldStateFormulaSet);
					return setNewBit;
				};
			
			return enrichmentEvaluator;
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
			foreach (CandidateTransition* transition in transitions)
				ConvertTransition(transition);
		}

		private void ConvertTransition(CandidateTransition* transition)
		{
			var targetEnrichment = DeriveNewEnrichment(transition->Formulas);
			*(int*)(transition->TargetStatePointer + ExtraBytesOffset) = targetEnrichment;
		}

		private int DeriveNewEnrichment(StateFormulaSet oldStateFormulaSet)
		{
			int newEnrichment = 0;
			for (var i = 0; i < _newEnrichmentEvaluator.Length; i++)
			{
				var setNewBit = _newEnrichmentEvaluator[i](oldStateFormulaSet);
				if (setNewBit)
				{
					newEnrichment |= 1 << i;
				}
			}
			return newEnrichment;
		}
	}
}