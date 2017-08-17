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

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Optimized
{
	using System;
	using System.Linq;
	using AnalysisModelTraverser;
	using Formula;
	using ExecutableModel;
	using AnalysisModel;
	/// <summary>
	///   Generates a <see cref="LabeledTransitionMarkovDecisionProcessOld" /> for an <see cref="AnalysisModel" />.
	/// </summary>
	internal class LtmdpGenerator : ModelTraverser
	{
		private readonly LabeledTransitionMarkovDecisionProcess _mdp;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="executableStateFormulas">The state formulas that can be evaluated over the generated state graph.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal LtmdpGenerator(AnalysisModelCreator createModel, Formula terminateEarlyCondition, Formula[] executableStateFormulas,
									 AnalysisConfiguration configuration)
			: base(createModel, configuration, LabeledTransitionMarkovDecisionProcess.TransitionSize, terminateEarlyCondition != null)
		{
			_mdp = new LabeledTransitionMarkovDecisionProcess(Context.ModelCapacity.NumberOfStates, Context.ModelCapacity.NumberOfTransitions);
			_mdp.StateFormulaLabels = executableStateFormulas.Select(stateFormula=>stateFormula.Label).ToArray();

			Context.TraversalParameters.BatchedTransitionActions.Add(() => new LabeledTransitionMarkovDecisionProcess.LtmdpBuilderDuringTraversal(_mdp, configuration));
			if (terminateEarlyCondition != null)
			{
				_mdp.CreateStutteringState(Context.StutteringStateIndex);
				var terminalteEarlyFunc = StateFormulaSetEvaluatorCompilationVisitor.Compile(_mdp.StateFormulaLabels, terminateEarlyCondition);
				Context.TraversalParameters.TransitionModifiers.Add(() => new EarlyTerminationModifier(terminalteEarlyFunc));
			}
		}

		/// <summary>
		///   Generates the state graph.
		/// </summary>
		internal LabeledTransitionMarkovDecisionProcess GenerateStateGraph()
		{
			Context.Output.WriteLine("Generating labeled transition markov decision process.");
			TraverseModelAndReport();

			return _mdp;
		}
	}
}