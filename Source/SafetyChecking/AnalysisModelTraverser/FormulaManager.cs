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
	using System.IO;
	using Formula;
	using TraversalModifiers;
	using Utilities;

	public class FormulaManager
	{
		public FormulaManager()
		{
		}

		public void AddFormula()
		{
			var onceFormulaCollector = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			foreach (var formula in _formulasToCheck)
			{
				onceFormulaCollector.VisitNewTopLevelFormula(formula);
			}
			var onceFormulas = onceFormulaCollector.DeepestOnceFormulasWithCompilableOperand;
		}

		public void Formulas2()
		{
			foreach (var onceFormula in onceFormulas)
			{
				Assert.That(onceFormula.Operator == UnaryOperator.Once, "operator of OnceFormula must be Once");
			}
			var onceFormulaLabels = onceFormulas.Select(formula => formula.Label).ToArray();
			var formulasToObserve = onceFormulas.Select(formula => formula.Operand).ToArray();
		}

		public void Formulas3()
		{
			if (onceFormulas.Count > 0)
			{
				Func<ObserveFormulasModifier> observeFormulasModifier = () => new ObserveFormulasModifier(executableStateFormulas, formulasToObserve);
				ModelTraverser.Context.TraversalParameters.TransitionModifiers.Add(observeFormulasModifier);
			}
		}

		public void Formula4()
		{
			labeledTransitionMarkovChain.StateFormulaLabels =
				labeledTransitionMarkovChain.StateFormulaLabels.Concat(onceFormulaLabels).ToArray();
		}

		public static void PrintStateFormulas(Formula[] stateFormulas, TextWriter writer)
		{
			writer.WriteLine("Labels");
			for (var i = 0; i < stateFormulas.Length; i++)
			{
				writer?.WriteLine($"\t {i} {stateFormulas[i].Label}: {stateFormulas[i]}");
			}
		}
	}
}
