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

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using AnalysisModelTraverser;
	using ExecutableModel;
	using Formula;
	using Utilities;
	using AnalysisModel;
	using ExecutedModel;

	public class MarkovDecisionProcessFromExecutableModelGenerator<TExecutableModel> : MarkovDecisionProcessGenerator where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly ExecutableModelCreator<TExecutableModel> _runtimeModelCreator;

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public MarkovDecisionProcessFromExecutableModelGenerator(ExecutableModelCreator<TExecutableModel> runtimeModelCreator)
		{
			Requires.NotNull(runtimeModelCreator, nameof(runtimeModelCreator));
			_runtimeModelCreator = runtimeModelCreator;
		}
		

		/// <summary>
		///   Generates a <see cref="MarkovDecisionProcess" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		public LabeledTransitionMarkovDecisionProcess GenerateLabeledTransitionMarkovDecisionProcess()
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");

			ProbabilityMatrixCreationStarted = true;
			
			FormulaManager.Calculate(Configuration);
			var stateFormulasToCheckInBaseModel = FormulaManager.StateFormulasToCheckInBaseModel.ToArray();

			ExecutedModel<TExecutableModel> model = null;
			var modelCreator = _runtimeModelCreator.CreateCoupledModelCreator(stateFormulasToCheckInBaseModel);
			Func<AnalysisModel> createAnalysisModelFunc = () =>
				model = new LtmdpExecutedModel<TExecutableModel>(modelCreator, Configuration);
			var createAnalysisModel = new AnalysisModelCreator(createAnalysisModelFunc);

			var ltmdp = GenerateLtmdp(createAnalysisModel);
			return ltmdp;
		}
		
		/// <summary>
		///   Generates a <see cref="MarkovDecisionProcess" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		public NestedMarkovDecisionProcess GenerateNestedMarkovDecisionProcess()
		{
			var ltmdp = GenerateLabeledTransitionMarkovDecisionProcess();
			return ConvertToNmdp(ltmdp);
		}

	}
}