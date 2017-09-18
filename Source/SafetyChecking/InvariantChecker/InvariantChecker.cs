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

namespace ISSE.SafetyChecking.ExecutedModel
{
	using System;
	using System.Linq;
	using ExecutableModel;
	using AnalysisModel;
	using AnalysisModelTraverser;
	using FaultMinimalKripkeStructure;
	using Utilities;
	using Formula;
	using StateGraphModel;

	/// <summary>
	///   Checks whether an invariant holds for all states of an <see cref="AnalysisModel" />.
	/// </summary>
	internal sealed class InvariantChecker : DisposableObject
	{
		public ModelTraverser ModelTraverser { get; }

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		/// <param name="formulaIndex">The zero-based index of the analyzed formula.</param>
		internal InvariantChecker(AnalysisModelCreator createModel, AnalysisConfiguration configuration, int formulaIndex)
		{
			ModelTraverser = new ModelTraverser(createModel, configuration, 0, false);
			ModelTraverser.Context.TraversalParameters.TransitionActions.Add(() => new InvariantViolationByIndexAction(formulaIndex));
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		/// <param name="stateFormula">The analyzed stateFormula.</param>
		internal InvariantChecker(AnalysisModelCreator createModel, AnalysisConfiguration configuration, Formula stateFormula)
		{
			ModelTraverser = new ModelTraverser(createModel, configuration, 0, false);
			var formulasToCheck = ModelTraverser.AnalyzedModels.First().Formulas;

			ModelTraverser.Context.TraversalParameters.TransitionActions.Add(() => new InvariantViolationAction(formulasToCheck,stateFormula));
		}

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		internal InvariantAnalysisResult Check()
		{
			ModelTraverser.Context.Output.WriteLine("Performing invariant check.");

			ModelTraverser.TraverseModelAndReport();

			if (!ModelTraverser.Context.FormulaIsValid && !ModelTraverser.Context.Configuration.ProgressReportsOnly)
				ModelTraverser.Context.Output.WriteLine("Invariant violation detected.");

			return new InvariantAnalysisResult
			{
				FormulaHolds = ModelTraverser.Context.FormulaIsValid,
				CounterExample = ModelTraverser.Context.CounterExample,
				StateCount = ModelTraverser.Context.StateCount,
				TransitionCount = ModelTraverser.Context.TransitionCount,
				ComputedTransitionCount = ModelTraverser.Context.ComputedTransitionCount,
				LevelCount = ModelTraverser.Context.LevelCount
			};
		}


		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			// StateStorage must be freed manually. Reason is that invariant checker does not free up the
			// space, because it might be necessary for other usages of the ModelTraversers (e.g. StateGraphGenerator
			// which keeps the States for the StateGraph)
			ModelTraverser.Context.States.SafeDispose();

			if (!disposing)
				return;
			ModelTraverser.SafeDispose();
		}
	}
}