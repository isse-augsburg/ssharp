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

namespace ISSE.SafetyChecking.AnalysisModelTraverser
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Formula;
	using TraversalModifiers;
	using Utilities;

	public class FormulaManager
	{
		private readonly HashSet<string> _labelsOfKnownFormulas = new HashSet<string>();

		private readonly List<Formula> _formulasToCheck = new List<Formula>();

		private Formula _terminateEarlyFormula;

		private bool _finished = false;

		private AnalysisConfiguration _configuration;

		/// <summary>
		///   Contains the state labels after the StateFormulasToCheckInBaseModel have been evaluated and all 
		///   TransitionModifiers have been executed .
		/// </summary>
		private readonly List<Formula> _finalStateFormulas = new List<Formula>();

		/// <summary>
		///   Contains the formulas which should be checked in the base model (these are the formulas which are
		///   compiled to a Func{bool}.
		/// </summary>
		private readonly List<Formula> _stateFormulasToCheckInBaseModel = new List<Formula>();
		
		/// <summary>
		///   Contains the TransitionModifiers which should be applied to embed observers and allow EarlyTermination.
		/// </summary>
		private readonly List<Func<ITransitionModifier>> _transitionModifierGenerators = new List<Func<ITransitionModifier>>();

		/// <summary>
		///   Contains the state labels after the StateFormulasToCheckInBaseModel have been evaluated and all 
		///   TransitionModifiers have been executed .
		/// </summary>
		public IEnumerable<string> FinalStateFormulaLabels => FinalStateFormulas.Select(formula => formula.Label);

		/// <summary>
		///   Contains the formulas after the StateFormulasToCheckInBaseModel have been evaluated and all 
		///   TransitionModifiers have been executed .
		/// </summary>
		public IEnumerable<Formula> FinalStateFormulas => _finalStateFormulas;

		/// <summary>
		///   Contains the formulas which should be checked in the base model (these are the formulas which are
		///   compiled to a Func{bool}.
		/// </summary>
		public IEnumerable<Formula> StateFormulasToCheckInBaseModel => _stateFormulasToCheckInBaseModel;

		/// <summary>
		///   Contains the TransitionModifiers which should be applied to embed observers and allow EarlyTermination.
		/// </summary>
		internal IEnumerable<Func<ITransitionModifier>> TransitionModifierGenerators => _transitionModifierGenerators;


		/// <summary>
		///   Contains the TransitionModifiers which should be applied to embed observers and allow EarlyTermination.
		/// </summary>
		internal bool NeedsStutteringState { get; private set; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		public FormulaManager()
		{
		}
		
		public void AddKnownFormulaLabel(string knownFormulaLabel)
		{
			Assert.That(!_finished, $"no new elements may be added after {nameof(Calculate)}");
			if (!_labelsOfKnownFormulas.Contains(knownFormulaLabel))
				_labelsOfKnownFormulas.Add(knownFormulaLabel);
		}

		public void AddKnownFormulaLabels(params string[] knownFormulaLabel)
		{
			AddKnownFormulaLabels((IEnumerable<string>)knownFormulaLabel);
		}

		public void AddKnownFormulaLabels(IEnumerable<string> knownFormulaLabel)
		{
			foreach (var formulaLabel in knownFormulaLabel)
			{
				AddKnownFormulaLabel(formulaLabel);
			}
		}
		
		public void AddKnownFormula(Formula knownFormula)
		{
			AddKnownFormulaLabel(knownFormula.Label);
		}

		public void AddKnownFormulas(params Formula[] knownFormulas)
		{
			AddKnownFormulas((IEnumerable<Formula>)knownFormulas);
		}

		public void AddKnownFormulas(IEnumerable<Formula> knownFormulas)
		{
			foreach (var formula in knownFormulas)
			{
				AddKnownFormula(formula);
			}
		}

		public void AddFormulaToCheck(Formula formulaToCheck)
		{
			Assert.That(!_finished, $"no new elements may be added after {nameof(Calculate)}");
			if (!_formulasToCheck.Contains(formulaToCheck))
				_formulasToCheck.Add(formulaToCheck);
		}

		public void AddFormulasToCheck(params Formula[] formulasToCheck)
		{
			AddFormulasToCheck((IEnumerable<Formula>)formulasToCheck);
		}

		public void AddFormulasToCheck(IEnumerable<Formula> formulasToCheck)
		{
			foreach (var formula in formulasToCheck)
			{
				AddFormulaToCheck(formula);
			}
		}

		public void SetTerminateEarlyFormula(Formula terminateEarlyFormula)
		{
			Assert.That(!_finished, $"terminateEarly formula may not be set after {nameof(Calculate)}");
			_terminateEarlyFormula = terminateEarlyFormula;
		}

		/// <summary>
		/// </summary>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		public void Calculate(AnalysisConfiguration configuration)
		{
			Assert.That(!_finished, $"{nameof(Calculate)} may only be called once");
			_finished = true;
			_configuration = configuration;

			AddStateFormulasToCheckInBaseModel();
			AddDeepestOnceFormulas();
			AddTerminateEarlyFormula();
		}

		private void AddStateFormulasToCheckInBaseModel()
		{
			CollectStateFormulasVisitor stateFormulaCollector;
			if (_configuration.UseAtomarPropositionsAsStateLabels)
				stateFormulaCollector = new CollectAtomarPropositionFormulasVisitor();
			else
				stateFormulaCollector = new CollectMaximalCompilableFormulasVisitor();

			foreach (var stateFormula in _formulasToCheck)
			{
				stateFormulaCollector.VisitNewTopLevelFormula(stateFormula);
			}
			if (_terminateEarlyFormula != null)
			{
				stateFormulaCollector.VisitNewTopLevelFormula(_terminateEarlyFormula);
			}
			var stateFormulas = stateFormulaCollector.CollectedStateFormulas;

			foreach (var formula in stateFormulas)
			{
				_stateFormulasToCheckInBaseModel.Add(formula);

				_finalStateFormulas.Add(formula);
				_labelsOfKnownFormulas.Add(formula.Label);
			}
		}
		
		private void AddDeepestOnceFormulas()
		{
			var alreadyKnownLabels = FinalStateFormulaLabels.ToArray();

			var onceFormulaCollector = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			foreach (var formula in _formulasToCheck)
			{
				onceFormulaCollector.VisitNewTopLevelFormula(formula);
			}
			if (_terminateEarlyFormula != null)
			{
				onceFormulaCollector.VisitNewTopLevelFormula(_terminateEarlyFormula);
			}
			var deepestOnceFormulas =  onceFormulaCollector.DeepestOnceFormulasWithCompilableOperand.ToArray();

			if (deepestOnceFormulas.Length > 0)
			{
				var formulasToObserve = deepestOnceFormulas.Select(formula => formula.Operand).ToArray();

				Func<ObserveFormulasModifier> observeFormulasModifier = () => new ObserveFormulasModifier(alreadyKnownLabels, formulasToObserve);
				_transitionModifierGenerators.Add(observeFormulasModifier);
			}

			foreach (var formula in deepestOnceFormulas)
			{
				Assert.That(formula.Operator == UnaryOperator.Once, "operator of OnceFormula must be Once");
				_finalStateFormulas.Add(formula);
				_labelsOfKnownFormulas.Add(formula.Label);
			}
		}
		
		public void AddTerminateEarlyFormula()
		{
			if (!_configuration.EnableEarlyTermination)
				return;

			// TerminateEarly also works with a OnceFormula when the TransitionModifier is added before the EarlyTerminationModifier
			if (_terminateEarlyFormula == null)
				return;

			NeedsStutteringState = true;
			

			// This method also succeeds with Once formulas, when those Once formulas have been normalized (i.e. a observeFormulasModifier exists)
			var terminateEarlyFunc = StateFormulaSetEvaluatorCompilationVisitor.Compile(FinalStateFormulaLabels.ToArray(), _terminateEarlyFormula);
			_transitionModifierGenerators.Add(() => new EarlyTerminationModifier(terminateEarlyFunc));
		}

		public static void PrintStateFormulas(IEnumerable<Formula> stateFormulas, TextWriter writer)
		{
			writer?.WriteLine("Labels");

			var i = 0;
			using (var enumerator = stateFormulas.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					writer?.WriteLine($"\t {i} {enumerator.Current.Label}: {enumerator.Current}");
					i++;
				}
			}
		}
	}
}
