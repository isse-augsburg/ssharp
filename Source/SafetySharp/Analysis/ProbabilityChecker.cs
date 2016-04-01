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
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents the Mrmc model checker.
	/// </summary>
	public class ProbabilityChecker : IDisposable
	{
		public struct FormulaChecker
		{
			public FormulaChecker(Func<Probability> checkWithDefaultChecker,Func<ProbabilisticModelChecker,Probability> checkWithChecker)
			{
				Check = checkWithDefaultChecker;
				CheckWithChecker = checkWithChecker;
			}

			// Check with the DefaultChecker of ProbabilityChecker this FormulaChecker was built in
			public Func<Probability> Check { get; }
			public Func<ProbabilisticModelChecker,Probability> CheckWithChecker { get; }
		}
		

		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;

		private object _probabilityMatrixCreationStarted = false;
		private bool _probabilityMatrixWasCreated = false;

		internal CompactProbabilityMatrix CompactProbabilityMatrix { get; private set; }

		private ModelBase _model;
		private readonly ConcurrentBag<Formula> _formulasToCheck = new ConcurrentBag<Formula>();
		
		public ProbabilisticModelChecker DefaultChecker { get; set; }

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

		private Probability CheckWithDefaultChecker(Formula formulaToCheck)
		{
			if (DefaultChecker == null)
			{
				DefaultChecker = new Mrmc(this);
			}
			return DefaultChecker.ExecuteCalculation(formulaToCheck);
		}

		public void CreateProbabilityMatrix()
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");
			var alreadyStarted = Interlocked.CompareExchange(ref _probabilityMatrixCreationStarted, true, false);
			if ((bool)alreadyStarted)
				return;

			var stopwatch = new Stopwatch();

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(_model, _formulasToCheck.ToArray());

			//return CheckInvariant(serializer.Load);

			using (var checker = new ProbabilityMatrixBuilder(serializer.Load, message => OutputWritten?.Invoke(message), Configuration))
			{
				var initializationTime = stopwatch.Elapsed;
				stopwatch.Restart();
				
				var sparseProbabilityMatrix = checker.CreateProbabilityMatrix();
				var derivedProbabilityMatrix = sparseProbabilityMatrix.DeriveCompactProbabilityMatrix();
				//var compactToSparse = CompactProbabilityMatrix = derivedProbabilityMatrix.Item1;
				CompactProbabilityMatrix = derivedProbabilityMatrix.Item2;
				var creationTime = stopwatch.Elapsed;
				stopwatch.Stop();

				if (true) //Configuration.ProgressReportsOnly
				{
					OutputWritten?.Invoke(String.Empty);
					OutputWritten?.Invoke("===============================================");
					OutputWritten?.Invoke($"Initialization time: {initializationTime}");
					OutputWritten?.Invoke($"Probability matrix creation time: {creationTime}");
					OutputWritten?.Invoke($"States: {CompactProbabilityMatrix.States}");
					OutputWritten?.Invoke($"Transitions: {CompactProbabilityMatrix.NumberOfTransitions}");
					OutputWritten?.Invoke($"{(int)(CompactProbabilityMatrix.States / stopwatch.Elapsed.TotalSeconds):n0} states per second");
					OutputWritten?.Invoke($"{(int)(CompactProbabilityMatrix.NumberOfTransitions / stopwatch.Elapsed.TotalSeconds):n0} transitions per second");
					OutputWritten?.Invoke("===============================================");
					OutputWritten?.Invoke(String.Empty);
				}
			}

			_probabilityMatrixWasCreated = true;
			Interlocked.MemoryBarrier();
		}

		public void AssertProbabilityMatrixWasCreated()
		{
			Requires.That(_probabilityMatrixWasCreated, nameof(CreateProbabilityMatrix) + "must be called before");
		}

		public FormulaChecker CalculateProbabilityToReachStates(Formula formulaValidInRequestedStates)
		{
			Requires.NotNull(formulaValidInRequestedStates, nameof(formulaValidInRequestedStates));

			var visitor = new IsStateFormulaVisitor();
			visitor.Visit(formulaValidInRequestedStates);

			if (!visitor.IsStateFormula)
				throw new InvalidOperationException("Formula must be non-temporal state formulas.");
			
			_formulasToCheck.Add(formulaValidInRequestedStates);

			Interlocked.MemoryBarrier();
			if ((bool)_probabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(CalculateProbabilityToReachStates) + " must be called before " + nameof(CreateProbabilityMatrix));
			}

			var formulaToCheck = formulaValidInRequestedStates;

			Func<Probability> checkWithDefaultChecker = () => CheckWithDefaultChecker(formulaToCheck);
			Func<ProbabilisticModelChecker,Probability> checkWithChecker = customChecker => customChecker.ExecuteCalculation(formulaToCheck);

			var checker = new FormulaChecker(checkWithDefaultChecker, checkWithChecker);
			return checker;
		}

		public void Dispose()
		{
			DefaultChecker.Dispose();
		}
	}
}