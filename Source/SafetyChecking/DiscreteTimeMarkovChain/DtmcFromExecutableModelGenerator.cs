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

	public class DtmcFromExecutableModelGenerator<TExecutableModel> where TExecutableModel : ExecutableModel<TExecutableModel>
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

		public bool ProbabilityMatrixCreationStarted { get; private set; } = false;

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public DtmcFromExecutableModelGenerator(ExecutableModelCreator<TExecutableModel> runtimeModelCreator)
		{
			Requires.NotNull(runtimeModelCreator, nameof(runtimeModelCreator));
			_runtimeModelCreator = runtimeModelCreator;
		}
		

		/// <summary>
		///   Generates a <see cref="StateGraph" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		private DiscreteTimeMarkovChain GenerateMarkovChain(Func<AnalysisModel<TExecutableModel>> createModel, Formula terminateEarlyCondition, AtomarPropositionFormula[] executableStateFormulas)
		{
			using (var checker = new LtmcGenerator<TExecutableModel>(createModel, terminateEarlyCondition, executableStateFormulas, OutputWritten, Configuration))
			{
				var labeledTransitionMarkovChain = checker.GenerateStateGraph();
				var ltmcToMc = new LtmcToDtmc(labeledTransitionMarkovChain);
				var markovChain = ltmcToMc.MarkovChain;
				return markovChain;
			}
		}
		

		public DiscreteTimeMarkovChain GenerateMarkovChain(Formula terminateEarlyCondition = null)
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
			Func<AnalysisModel<TExecutableModel>> createAnalysisModel = () =>
				model = new LtmcExecutedModel<TExecutableModel>(modelCreator, 0, Configuration.SuccessorCapacity);
			
			return GenerateMarkovChain(createAnalysisModel,terminateEarlyCondition, stateFormulas);
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