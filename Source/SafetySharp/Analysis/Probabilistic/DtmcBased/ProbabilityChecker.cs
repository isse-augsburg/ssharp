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
	using System.Collections.Concurrent;
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
	


	public class ProbabilityChecker : IDisposable
	{
		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;

		private object _probabilityMatrixCreationStarted = false;
		private bool _probabilityMatrixWasCreated = false;

		internal DiscreteTimeMarkovChain MarkovChain { get; private set; }

		private ModelBase _model;
		private readonly ConcurrentBag<Formula> _formulasToCheck = new ConcurrentBag<Formula>();

		public DtmcModelChecker ModelChecker { get; set; }

		public void InitializeDefaultChecker()
		{
			AssertProbabilityMatrixWasCreated();
			if (ModelChecker == null)
			{
				ModelChecker = new Mrmc(MarkovChain);
			}
		}


		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;
		

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public ProbabilityChecker(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));
			_model = model;
		}
		
		private Probability CalculateProbabilityWithDefaultChecker(Formula formulaToCheck)
		{
			InitializeDefaultChecker();
			return ModelChecker.CalculateProbability(formulaToCheck);
		}

		private bool CalculateFormulaWithDefaultChecker(Formula formulaToCheck)
		{
			InitializeDefaultChecker();
			return ModelChecker.CalculateFormula(formulaToCheck);
		}


		private RewardResult CalculateRewardWithDefaultChecker(Formula formulaToCheck)
		{
			InitializeDefaultChecker();
			return ModelChecker.CalculateReward(formulaToCheck);
		}



		
		/// <summary>
		///   Generates a <see cref="StateGraph" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal DiscreteTimeMarkovChain GenerateMarkovChain(Func<AnalysisModel> createModel, Formula terminateEarlyCondition, StateFormula[] stateFormulas)
		{
			Requires.That(IntPtr.Size == 8, "State graph generation is only supported in 64bit processes.");

			var stopwatch = new Stopwatch();
			stopwatch.Start();

			Configuration.CpuCount = 4;


			using (var checker = new LtmcGenerator(createModel, terminateEarlyCondition, stateFormulas, OutputWritten, Configuration))
			{
				var markovChain = default(DiscreteTimeMarkovChain);
				var initializationTime = stopwatch.Elapsed;
				var markovChainGenerationTime= stopwatch.Elapsed;
				stopwatch.Restart();

				try
				{
					var labeledTransitionMarkovChain = checker.GenerateStateGraph();
					markovChainGenerationTime = stopwatch.Elapsed;
					stopwatch.Restart();
					var ltmcToMc = new LtmcToDtmc(labeledTransitionMarkovChain);
					markovChain = ltmcToMc.MarkovChain;
					return markovChain;
				}
				finally
				{
					stopwatch.Stop();

					if (!Configuration.ProgressReportsOnly)
					{
						OutputWritten?.Invoke(String.Empty);
						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke($"Initialization time: {initializationTime}");
						OutputWritten?.Invoke($"Labeled Transition Markov chain generation time: {markovChainGenerationTime}");
						OutputWritten?.Invoke($"Markov chain conversion time: {stopwatch.Elapsed}");

						if (markovChain != null)
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
		///   Generates a <see cref="MarkovChain" /> for the model created by <paramref name="createModel" />.
		/// </summary>
		internal DiscreteTimeMarkovChain GenerateMarkovChain(Func<RuntimeModel> createModel, Formula terminateEarlyCondition, StateFormula[] stateFormulas)
		{
			return GenerateMarkovChain(() => new LtmcExecutedModel(createModel, Configuration.SuccessorCapacity), terminateEarlyCondition, stateFormulas);
		}

		/// <summary>
		///   Generates a <see cref="MarkovChain" /> for the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model the state graph should be generated for.</param>
		/// <param name="stateFormulas">The state formulas that should be evaluated during state graph generation.</param>
		internal DiscreteTimeMarkovChain GenerateMarkovChain(ModelBase model, Formula terminateEarlyCondition, params StateFormula[] stateFormulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(stateFormulas, nameof(stateFormulas));

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(model, stateFormulas);

			return GenerateMarkovChain((Func<RuntimeModel>)serializer.Load, terminateEarlyCondition, stateFormulas);
		}





		public void CreateMarkovChain(Formula terminateEarlyCondition = null)
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");
			var alreadyStarted = Interlocked.CompareExchange(ref _probabilityMatrixCreationStarted, true, false);
			if ((bool)alreadyStarted)
				return;


			var stateFormulaCollector = new CollectStateFormulasVisitor();
			foreach (var stateFormula in _formulasToCheck)
			{
				stateFormulaCollector.Visit(stateFormula);
			}

			//StateFormulaSetEvaluatorCompilationVisitor

			MarkovChain = GenerateMarkovChain(_model, terminateEarlyCondition, stateFormulaCollector.StateFormulas.ToArray());

			_probabilityMatrixWasCreated = true;
			Interlocked.MemoryBarrier();
		}

		public void AssertProbabilityMatrixWasCreated()
		{
			Requires.That(_probabilityMatrixWasCreated, nameof(CreateMarkovChain) + "must be called before");
		}

		public ProbabilityCalculator CalculateProbability(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulaReturningProbabilityVisitor();
			visitor.Visit(formula);
			if (!visitor.IsReturningProbability)
				throw new InvalidOperationException("Formula must return probability.");
			
			Interlocked.MemoryBarrier();
			if ((bool)_probabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(CalculateProbability) + " must be called before " + nameof(CreateMarkovChain));
			}

			_formulasToCheck.Add(formula);
			var formulaToCheck = formula;

			Func<Probability> useDefaultChecker = () => CalculateProbabilityWithDefaultChecker(formulaToCheck);
			Func<DtmcModelChecker,Probability> useCustomChecker = customChecker => customChecker.CalculateProbability(formulaToCheck);

			var checker = new ProbabilityCalculator(useDefaultChecker);
			return checker;
		}

		public FormulaCalculator CalculateFormula(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			Interlocked.MemoryBarrier();
			if ((bool)_probabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(CalculateFormula) + " must be called before " + nameof(CreateMarkovChain));
			}

			_formulasToCheck.Add(formula);
			var formulaToCheck = formula;

			Func<bool> useDefaultChecker = () => CalculateFormulaWithDefaultChecker(formulaToCheck);
			Func<DtmcModelChecker, bool> useCustomChecker = customChecker => customChecker.CalculateFormula(formulaToCheck);

			var checker = new FormulaCalculator(useDefaultChecker);
			return checker;
		}

		public RewardCalculator CalculateReward(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulaReturningRewardResultVisitor();
			visitor.Visit(formula);
			if (!visitor.IsReturningRewardResult)
				throw new InvalidOperationException("Formula must return reward.");

			_formulasToCheck.Add(formula);
			var formulaToCheck = formula;

			Interlocked.MemoryBarrier();
			if ((bool)_probabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(CalculateReward) + " must be called before " + nameof(CreateMarkovChain));
			}
			

			Func<RewardResult> useDefaultChecker = () => CalculateRewardWithDefaultChecker(formulaToCheck);
			Func<DtmcModelChecker, RewardResult> useCustomChecker = customChecker => customChecker.CalculateReward(formulaToCheck);

			var checker = new RewardCalculator(useDefaultChecker);
			return checker;
		}

		public void Dispose()
		{
			ModelChecker?.Dispose();
		}
	}
}