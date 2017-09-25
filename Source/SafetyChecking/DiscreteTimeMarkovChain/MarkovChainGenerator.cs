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
	public abstract class MarkovChainGenerator
	{
		private LabeledTransitionMarkovChain _markovChain;
		
		public bool ProbabilityMatrixCreationStarted { get; protected set; } = false;

		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;
		
		protected readonly FormulaManager FormulaManager = new FormulaManager();

		/// <summary>
		///   Generates a <see cref="LabeledTransitionMarkovChain" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal LabeledTransitionMarkovChain GenerateLtmc(AnalysisModelCreator createModel)
		{
			using (var modelTraverser = new ModelTraverser(createModel, Configuration, LabeledTransitionMarkovChain.TransitionSize, FormulaManager.NeedsStutteringState))
			{
				_markovChain = new LabeledTransitionMarkovChain(modelTraverser.Context.ModelCapacity.NumberOfStates, modelTraverser.Context.ModelCapacity.NumberOfTransitions);
				_markovChain.StateFormulaLabels = FormulaManager.FinalStateFormulaLabels.ToArray();
				
				if (FormulaManager.NeedsStutteringState)
					_markovChain.CreateStutteringState(modelTraverser.Context.StutteringStateIndex);

				modelTraverser.Context.TraversalParameters.TransitionModifiers.AddRange(FormulaManager.TransitionModifierGenerators);
				//modelTraverser.Context.TraversalParameters.TransitionModifiers.Add(() => new ConsolidateTransitionsModifier());
				modelTraverser.Context.TraversalParameters.BatchedTransitionActions.Add(() => new LabeledTransitionMarkovChain.LtmcBuilder(_markovChain));

				modelTraverser.Context.Output.WriteLine($"Generating labeled transition markov chain.");
				modelTraverser.TraverseModelAndReport();

				// StateStorage must be freed manually. Reason is that invariant checker does not free up the
				// space, because it might be necessary for other usages of the ModelTraversers (e.g. StateGraphGenerator
				// which keeps the States for the StateGraph)
				modelTraverser.Context.States.SafeDispose();
			}

			if (Configuration.WriteGraphvizModels)
			{
				FormulaManager.PrintStateFormulas(FormulaManager.FinalStateFormulas, Configuration.DefaultTraceOutput);
				Configuration.DefaultTraceOutput.WriteLine("Ltmc Model");
				_markovChain.ExportToGv(Configuration.DefaultTraceOutput);
			}

			return _markovChain;
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
			FormulaManager.AddFormulaToCheck(formula);
		}
	}
}