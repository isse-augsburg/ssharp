// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.Analysis.ModelChecking
{
	using System;
	using System.Linq;
	using ModelTraversal.TraversalModifiers;
	using Utilities;

	/// <summary>
	///   Checks whether an invariant holds for all states of an <see cref="AnalysisModel" />.
	/// </summary>
	internal class InvariantChecker : ModelTraverser
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		/// <param name="formulaIndex">The zero-based index of the analyzed formula.</param>
		internal InvariantChecker(Func<AnalysisModel> createModel, Action<string> output, AnalysisConfiguration configuration, int formulaIndex)
			: base(createModel, output, configuration)
		{
			Context.TraversalParameters.TransitionActions.Add(() => new InvariantViolationAction(formulaIndex));
		}

		/// <summary>
		///   Checks whether the model's invariant holds for all states.
		/// </summary>
		internal AnalysisResult Check()
		{
			if (!Context.Configuration.ProgressReportsOnly)
			{
				Context.Output($"Performing invariant check using {AnalyzedModels.Count()} CPU cores.");
				Context.Output($"State vector has {AnalyzedModels.First().StateVectorSize} bytes.");
			}

			TraverseModel();

			if (!Context.Configuration.ProgressReportsOnly)
				Context.Report();

			RethrowTraversalException();

			if (!Context.FormulaIsValid && !Context.Configuration.ProgressReportsOnly)
				Context.Output("Invariant violation detected.");

			return new AnalysisResult
			{
				FormulaHolds = Context.FormulaIsValid,
				CounterExample = Context.CounterExample,
				StateCount = Context.StateCount,
				TransitionCount = Context.TransitionCount,
				ComputedTransitionCount = Context.ComputedTransitionCount,
				LevelCount = Context.LevelCount,
			};
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			Context.States.SafeDispose();
			base.OnDisposing(disposing);
		}
	}
}