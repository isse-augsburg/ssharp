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

	public class NmdpFromExecutableModelGenerator<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;

		private readonly ExecutableModelCreator<TExecutableModel> _runtimeModelCreator;
		private readonly List<Formula> _formulasToCheck = new List<Formula>();

		public IEnumerable<Formula> FormulasToCheck => _formulasToCheck;

		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

		public bool ProbabilityMatrixCreationStarted { get; private set; }= false;

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public NmdpFromExecutableModelGenerator(ExecutableModelCreator<TExecutableModel> runtimeModelCreator)
		{
			Requires.NotNull(runtimeModelCreator, nameof(runtimeModelCreator));
			_runtimeModelCreator = runtimeModelCreator;
		}



		/// <summary>
		///   Generates a <see cref="MarkovDecisionProcess" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		private NestedMarkovDecisionProcess GenerateMarkovDecisionProcess(AnalysisModelCreator<TExecutableModel> createModel, Formula terminateEarlyCondition, AtomarPropositionFormula[] executableStateFormulas)
		{
			using (var checker = new LtmdpGenerator<TExecutableModel>(createModel, terminateEarlyCondition, executableStateFormulas, OutputWritten, Configuration))
			{
				var ltmdp = checker.GenerateStateGraph();
				if (Configuration.WriteGraphvizModels)
				{
					Configuration.DefaultTraceOutput.WriteLine("Ltmdp Model");
					ltmdp.ExportToGv(Configuration.DefaultTraceOutput);
				}
					
				var ltmdpToNmdp = new LtmdpToNmdp(ltmdp);
				var nmdp = ltmdpToNmdp.NestedMarkovDecisionProcess;
				if (Configuration.WriteGraphvizModels)
				{
					Configuration.DefaultTraceOutput.WriteLine("Nmdp Model");
					nmdp.ExportToGv(Configuration.DefaultTraceOutput);
				}
				return nmdp;
			}
		}


		/// <summary>
		///   Generates a <see cref="MarkovDecisionProcess" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		public NestedMarkovDecisionProcess GenerateMarkovDecisionProcess(Formula terminateEarlyCondition = null)
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");

			ProbabilityMatrixCreationStarted = true;

			var stateFormulaCollector = new CollectAtomarPropositionFormulasVisitor();
			foreach (var stateFormula in _formulasToCheck)
			{
				stateFormulaCollector.Visit(stateFormula);
			}
			if (terminateEarlyCondition)
			{
				stateFormulaCollector.Visit(terminateEarlyCondition);
			}
			var stateFormulas = stateFormulaCollector.AtomarPropositionFormulas.ToArray();

			ExecutedModel<TExecutableModel> model = null;
			var modelCreator = _runtimeModelCreator.CreateCoupledModelCreator(stateFormulas);
			Func<AnalysisModel<TExecutableModel>> createAnalysisModelFunc = () =>
				model = new LtmdpExecutedModel<TExecutableModel>(modelCreator, 0, Configuration.SuccessorCapacity);
			var createAnalysisModel=new AnalysisModelCreator<TExecutableModel>(createAnalysisModelFunc);

			return GenerateMarkovDecisionProcess(createAnalysisModel, terminateEarlyCondition, stateFormulas);
		}



		public void AddFormulaToCheck(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			Interlocked.MemoryBarrier();
			if ((bool)ProbabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(AddFormulaToCheck) + " must be called before " + nameof(GenerateMarkovDecisionProcess));
			}
			_formulasToCheck.Add(formula);
		}
	}
}