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
	using System.Collections.Generic;
	using System.Linq;
	using AnalysisModelTraverser;
	using Formula;
	using AnalysisModel;
	using AnalysisModelTraverser.TraversalModifiers;
	using Utilities;

	/// <summary>
	///   Generates a <see cref="LabeledTransitionMarkovChain" /> for an <see cref="AnalysisModel" />.
	/// </summary>
	public class LtmcGenerator : DisposableObject
	{
		internal ModelTraverser ModelTraverser { get; private set; }

		private LabeledTransitionMarkovChain _markovChain;
		
		public bool ProbabilityMatrixCreationStarted { get; protected set; } = false;

		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;


		protected readonly List<Formula> _formulasToCheck = new List<Formula>();

		public IEnumerable<Formula> FormulasToCheck => _formulasToCheck;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="createModel">Creates the model that should be checked.</param>
		/// <param name="executableStateFormulas">The state formulas that can be evaluated over the generated state graph.</param>
		/// <param name="output">The callback that should be used to output messages.</param>
		/// <param name="configuration">The analysis configuration that should be used.</param>
		internal void InitializeLtmcGenerator(AnalysisModelCreator createModel, Formula terminateEarlyCondition, Formula[] executableStateFormulas,
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
		///   Generates a <see cref="DiscreteTimeMarkovChain" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal LabeledTransitionMarkovChain GenerateLtmc(AnalysisModelCreator createModel, Formula terminateEarlyCondition, Formula[] executableStateFormulas)
		{
			// Embed Once-Formulas
			var onceFormulaCollector = new CollectDeepestOnceFormulasWithCompilableOperandVisitor();
			foreach (var formula in _formulasToCheck)
			{
				onceFormulaCollector.VisitNewTopLevelFormula(formula);
			}
			var onceFormulas = onceFormulaCollector.DeepestOnceFormulasWithCompilableOperand;

			LabeledTransitionMarkovChain labeledTransitionMarkovChain;

			InitializeLtmcGenerator(createModel, terminateEarlyCondition, executableStateFormulas, Configuration);
			{
				foreach (var onceFormula in onceFormulas)
				{
					Assert.That(onceFormula.Operator == UnaryOperator.Once, "operator of OnceFormula must be Once");
				}
				var onceFormulaLabels = onceFormulas.Select(formula => formula.Label).ToArray();
				var formulasToObserve = onceFormulas.Select(formula => formula.Operand).ToArray();

				if (onceFormulas.Count > 0)
				{
					Func<ObserveFormulasModifier> observeFormulasModifier = () => new ObserveFormulasModifier(executableStateFormulas, formulasToObserve);
					ModelTraverser.Context.TraversalParameters.TransitionModifiers.Add(observeFormulasModifier);
				}

				labeledTransitionMarkovChain = GenerateStateGraph();

				labeledTransitionMarkovChain.StateFormulaLabels =
					labeledTransitionMarkovChain.StateFormulaLabels.Concat(onceFormulaLabels).ToArray();
			}

			if (Configuration.WriteGraphvizModels)
			{
				PrintStateFormulas(executableStateFormulas);
				Configuration.DefaultTraceOutput.WriteLine("Ltmc Model");
				labeledTransitionMarkovChain.ExportToGv(Configuration.DefaultTraceOutput);
			}

			return labeledTransitionMarkovChain;
		}
		

		internal DiscreteTimeMarkovChain ConvertToMarkovChain(LabeledTransitionMarkovChain labeledTransitionMarkovChain)
		{
			var ltmcToMc = new LtmcToDtmc(labeledTransitionMarkovChain);
			var markovChain = ltmcToMc.MarkovChain;
			if (Configuration.WriteGraphvizModels)
			{
				Configuration.DefaultTraceOutput.WriteLine("Dtmc Model");
				markovChain.ExportToGv(Configuration.DefaultTraceOutput);
			}
			return markovChain;
		}


		public void AddFormulaToCheck(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			if (ProbabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(AddFormulaToCheck) + " must be called before the traversal of the model started!");
			}
			_formulasToCheck.Add(formula);
		}

		protected void PrintStateFormulas(Formula[] stateFormulas)
		{
			Configuration.DefaultTraceOutput?.WriteLine("Labels");
			for (var i = 0; i < stateFormulas.Length; i++)
			{
				Configuration.DefaultTraceOutput?.WriteLine($"\t {i} {stateFormulas[i].Label}: {stateFormulas[i]}");
			}
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