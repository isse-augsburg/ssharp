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
	using System.Threading;
	using ExecutableModel;
	using Utilities;
	using AnalysisModel;
	using Formula;
	using AnalysisModelTraverser;
	using System.Linq;
	using ExecutedModel;

	public class MarkovChainFromExecutableModelGenerator<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly ExecutableModelCreator<TExecutableModel> _runtimeModelCreator;
		private readonly List<Formula> _formulasToCheck = new List<Formula>();

		public IEnumerable<Formula> FormulasToCheck => _formulasToCheck;

		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

		public bool ProbabilityMatrixCreationStarted { get; private set; } = false;

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public MarkovChainFromExecutableModelGenerator(ExecutableModelCreator<TExecutableModel> runtimeModelCreator)
		{
			Requires.NotNull(runtimeModelCreator, nameof(runtimeModelCreator));
			_runtimeModelCreator = runtimeModelCreator;
		}
		
		private void PrintStateFormulas(Formula[] stateFormulas)
		{
			if (!Configuration.WriteGraphvizModels)
				return;
			Configuration.DefaultTraceOutput?.WriteLine("Labels");
			for (var i = 0; i < stateFormulas.Length; i++)
			{
				Configuration.DefaultTraceOutput?.WriteLine($"\t {i} {stateFormulas[i].Label}: {stateFormulas[i]}");
			}
		}

		/// <summary>
		///   Generates a <see cref="DiscreteTimeMarkovChain" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		private LabeledTransitionMarkovChain GenerateLtmc(AnalysisModelCreator createModel, Formula terminateEarlyCondition, Formula[] executableStateFormulas)
		{
			using (var checker = new LtmcGenerator(createModel, terminateEarlyCondition, executableStateFormulas, Configuration))
			{
				PrintStateFormulas(executableStateFormulas);

				var labeledTransitionMarkovChain = checker.GenerateStateGraph();
				
				if (Configuration.WriteGraphvizModels)
				{
					Configuration.DefaultTraceOutput.WriteLine("Ltmc Model");
					labeledTransitionMarkovChain.ExportToGv(Configuration.DefaultTraceOutput);
				}
				return labeledTransitionMarkovChain;
			}
		}

		private LabeledTransitionMarkovChain NormalizeLtmc(LabeledTransitionMarkovChain ltmc)
		{
			var retraverseModel = new LtmcRetraverseModel(ltmc, Configuration);
			retraverseModel.AddFormulas(FormulasToCheck);

			var createModel = new AnalysisModelCreator(() => retraverseModel);

			using (var checker = new LtmcGenerator(createModel, null, retraverseModel.Formulas, Configuration))
			{
				PrintStateFormulas(retraverseModel.Formulas);

				var labeledTransitionMarkovChain = checker.GenerateStateGraph();

				if (Configuration.WriteGraphvizModels)
				{
					Configuration.DefaultTraceOutput.WriteLine("Ltmc Model normalized");
					labeledTransitionMarkovChain.ExportToGv(Configuration.DefaultTraceOutput);
				}
				return labeledTransitionMarkovChain;
			}
		}

		private DiscreteTimeMarkovChain ConvertToMarkovChain(LabeledTransitionMarkovChain labeledTransitionMarkovChain)
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

		public LabeledTransitionMarkovChain GenerateLabeledMarkovChain(Formula terminateEarlyCondition = null)
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");

			ProbabilityMatrixCreationStarted = true;

			CollectStateFormulasVisitor stateFormulaCollector;
			if (Configuration.UseAtomarPropositionsAsStateLabels)
				stateFormulaCollector = new CollectAtomarPropositionFormulasVisitor();
			else
				stateFormulaCollector = new CollectMaximalCompilableFormulasVisitor();

			foreach (var stateFormula in _formulasToCheck)
			{
				stateFormulaCollector.VisitNewTopLevelFormula(stateFormula);
			}
			if (terminateEarlyCondition)
			{
				stateFormulaCollector.VisitNewTopLevelFormula(terminateEarlyCondition);
			}
			var stateFormulas = stateFormulaCollector.CollectedStateFormulas.ToArray();

			ExecutedModel<TExecutableModel> model = null;
			var modelCreator = _runtimeModelCreator.CreateCoupledModelCreator(stateFormulas);
			Func<AnalysisModel> createAnalysisModelFunc = () =>
				model = new LtmcExecutedModel<TExecutableModel>(modelCreator, 0, Configuration);
			var createAnalysisModel = new AnalysisModelCreator(createAnalysisModelFunc);

			var ltmc = GenerateLtmc(createAnalysisModel, terminateEarlyCondition, stateFormulas);

			if (Configuration.RetraversalNormalizations != RetraversalNormalizations.None)
				ltmc = NormalizeLtmc(ltmc);

			return ltmc;
		}
		
		public DiscreteTimeMarkovChain GenerateMarkovChain(Formula terminateEarlyCondition = null)
		{
			var ltmc = GenerateLabeledMarkovChain(terminateEarlyCondition);
			return ConvertToMarkovChain(ltmc);
		}


		public void AddFormulaToCheck(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			Interlocked.MemoryBarrier();
			if ((bool)ProbabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(AddFormulaToCheck) + " must be called before " + nameof(GenerateMarkovChain));
			}
			_formulasToCheck.Add(formula);
		}
	}
}