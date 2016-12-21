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

namespace SafetySharp.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;
	using ModelChecking;
	
	
	public class MdpFromExecutableModelGenerator
	{
		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;
		
		private ModelBase _model;
		private readonly List<Formula> _formulasToCheck = new List<Formula>();

		public IEnumerable<Formula> FormulasToCheck => _formulasToCheck;

		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

		public bool ProbabilityMatrixCreationStarted { get; private set; }= false;

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public MdpFromExecutableModelGenerator(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));
			_model = model;
		}



		/// <summary>
		///   Generates a <see cref="StateGraph" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		private MarkovDecisionProcess GenerateMarkovDecisionProcess(Func<AnalysisModel> createModel, Formula terminateEarlyCondition, ExecutableStateFormula[] executableStateFormulas)
		{
			Requires.That(IntPtr.Size == 8, "State graph generation is only supported in 64bit processes.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Configuration.CpuCount = 4;


			using (var checker = new LtmdpGenerator(createModel, terminateEarlyCondition, executableStateFormulas, OutputWritten, Configuration))
			{
				var mdp = default(MarkovDecisionProcess);
				var initializationTime = stopwatch.Elapsed;
				var mdpGenerationTime= stopwatch.Elapsed;
				stopwatch.Restart();

				try
				{
					var ltmdp = checker.GenerateStateGraph();
					mdpGenerationTime = stopwatch.Elapsed;
					stopwatch.Restart();
					var ltmdpToMdp = new LtmdpToMdp(ltmdp);
					mdp = ltmdpToMdp.MarkovDecisionProcess;
					return mdp;
				}
				finally
				{
					stopwatch.Stop();

					if (!Configuration.ProgressReportsOnly)
					{
						OutputWritten?.Invoke(String.Empty);
						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke($"Initialization time: {initializationTime}");
						OutputWritten?.Invoke($"Labeled Transition Markov Decision Process generation time: {mdpGenerationTime}");
						OutputWritten?.Invoke($"Markov Decision Process conversion time: {stopwatch.Elapsed}");

						if (mdp != null)
						{
							//TODO: OutputWritten?.Invoke($"{(int)(markovChain.StateCount / stopwatch.Elapsed.TotalSeconds):n0} states per second");
							//TODO: OutputWritten?.Invoke($"{(int)(markovChain.TransitionCount / stopwatch.Elapsed.TotalSeconds):n0} transitions per second");
						}

						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke(String.Empty);
					}
				}
			}
		}


		/// <summary>
		///   Generates a <see cref="MarkovDecisionProcess" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		private MarkovDecisionProcess GenerateMarkovDecisionProcess(Func<SafetySharpRuntimeModel> createModel, Formula terminateEarlyCondition, ExecutableStateFormula[] stateFormulas)
		{
			return GenerateMarkovDecisionProcess(() => new LtmdpExecutedModel(createModel, Configuration.SuccessorCapacity), terminateEarlyCondition, stateFormulas);
		}

		/// <summary>
		///   Generates a <see cref="MarkovDecisionProcess" /> for the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model the state graph should be generated for.</param>
		/// <param name="stateFormulas">The state formulas that should be evaluated during state graph generation.</param>
		private MarkovDecisionProcess GenerateMarkovDecisionProcess(ModelBase model, Formula terminateEarlyCondition, params ExecutableStateFormula[] stateFormulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(stateFormulas, nameof(stateFormulas));

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, stateFormulas);

			return GenerateMarkovDecisionProcess((Func<SafetySharpRuntimeModel>)serializer.Load, terminateEarlyCondition, stateFormulas);
		}
		

		public MarkovDecisionProcess GenerateMarkovDecisionProcess(Formula terminateEarlyCondition = null)
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");

			ProbabilityMatrixCreationStarted = true;

			var stateFormulaCollector = new CollectExecutableStateFormulasVisitor();
			foreach (var stateFormula in _formulasToCheck)
			{
				stateFormulaCollector.Visit(stateFormula);
			}
			return GenerateMarkovDecisionProcess(_model, terminateEarlyCondition, stateFormulaCollector.ExecutableStateFormulas.ToArray());
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