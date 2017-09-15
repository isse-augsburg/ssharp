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

namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System;
	using System.Linq;
	using AnalysisModelTraverser;
	using Formula;
	using ExecutableModel;
	using AnalysisModel;
	using Utilities;

	/// <summary>
	///   Generates a <see cref="LabeledTransitionMarkovChain" /> for an <see cref="AnalysisModel" />.
	/// </summary>
	internal sealed class LtmcGenerator : DisposableObject
	{
		public ModelTraverser ModelTraverser { get; }

		private readonly LabeledTransitionMarkovChain _markovChain;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="executableStateFormulas">The state formulas that can be evaluated over the generated state graph.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal LtmcGenerator(AnalysisModelCreator createModel, Formula terminateEarlyCondition, Formula[] executableStateFormulas,
									 AnalysisConfiguration configuration)
		{
			ModelTraverser = new ModelTraverser(createModel, configuration, LabeledTransitionMarkovChain.TransitionSize, terminateEarlyCondition != null);

			_markovChain = new LabeledTransitionMarkovChain(ModelTraverser.Context.ModelCapacity.NumberOfStates, ModelTraverser.Context.ModelCapacity.NumberOfTransitions);
			_markovChain.StateFormulaLabels = executableStateFormulas.Select(stateFormula=>stateFormula.Label).ToArray();

			ModelTraverser.Context.TraversalParameters.BatchedTransitionActions.Add(() => new LabeledTransitionMarkovChain.LtmcBuilder(_markovChain));
			if (terminateEarlyCondition != null)
			{
				_markovChain.CreateStutteringState(ModelTraverser.Context.StutteringStateIndex);
				if (!terminateEarlyCondition.IsStateFormula())
				{
					configuration.DefaultTraceOutput.WriteLine("Ignoring terminateEarlyCondition (not a StateFormula).");
				}
				else
				{
					var terminateEarlyFunc = StateFormulaSetEvaluatorCompilationVisitor.Compile(_markovChain.StateFormulaLabels, terminateEarlyCondition);
					ModelTraverser.Context.TraversalParameters.TransitionModifiers.Add(() => new EarlyTerminationModifier(terminateEarlyFunc));
				}

			}
		}

		/// <summary>
		///   Generates the state graph.
		/// </summary>
		internal LabeledTransitionMarkovChain GenerateStateGraph()
		{
			ModelTraverser.Context.Output.WriteLine($"Generating labeled transition markov chain.");
			ModelTraverser.TraverseModelAndReport();
			return _markovChain;
		}


		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			ModelTraverser.Context.States.SafeDispose();

			if (!disposing)
				return;
			ModelTraverser.SafeDispose();
		}
	}
}