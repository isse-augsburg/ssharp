// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using System.Diagnostics;
	using System.IO;
	using FormulaVisitors;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	/// <summary>
	///   Represents the LtsMin model checker.
	/// </summary>
	public class LtsMin : ModelChecker
	{
		/// <summary>
		///   Represents the LtsMin process that is currently running.
		/// </summary>
		private ExternalProcess _ltsMin;

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="model" />. Returns a
		///   <see cref="CounterExample" /> if the invariant is violated, <c>null</c> otherwise.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		public override AnalysisResult CheckInvariant(Model model, Formula invariant)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(invariant, nameof(invariant));

			var visitor = new IsStateFormulaVisitor();
			visitor.Visit(invariant);

			if (!visitor.IsStateFormula)
				throw new InvalidOperationException("Invariants must be non-temporal state formulas.");

			var transformationVisitor = new LtsMinLtlTransformer();
			transformationVisitor.Visit(invariant);

			return Check(model, invariant,
				$"--invariant=\"({RuntimeModel.ConstructionStateName} == 1) || ({transformationVisitor.TransformedFormula})\"");
		}

		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		public override AnalysisResult Check(Model model, Formula formula)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsLtlFormulaVisitor();
			visitor.Visit(formula);

			if (!visitor.IsLtlFormula)
				throw new NotSupportedException("CTL model checking is currently not supported with LtsMin.");

			var transformationVisitor = new LtsMinLtlTransformer();
			transformationVisitor.Visit(new UnaryFormula(formula, UnaryOperator.Next));

			return Check(model, formula, $"--ltl=\"{transformationVisitor.TransformedFormula}\"");
		}

		/// <summary>
		///   Interprets the <paramref name="exitCode" /> returned by LtsMin.
		/// </summary>
		/// <param name="exitCode">The exit code that should be interpreted.</param>
		private static bool InterpretExitCode(int exitCode)
		{
			switch (exitCode)
			{
				case 0:
					return true;
				case 1:
					return false;
				case 255:
					throw new InvalidOperationException("Model checking failed due to an error.");
				default:
					throw new InvalidOperationException($"LtsMin exited with an unexpected exit code: {exitCode}.");
			}
		}

		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		/// <param name="checkArgument">The argument passed to LtsMin that indicates which kind of check to perform.</param>
		private AnalysisResult Check(Model model, Formula formula, string checkArgument)
		{
			try
			{
				using (var modelFile = new TemporaryFile("ssharp"))
				using (var counterExampleFile = new TemporaryFile("gcf"))
				{
					using (var stream = new FileStream(modelFile.FilePath, FileMode.Create))
						RuntimeModelSerializer.Save(stream, model, 4, formula);

					CreateProcess(modelFile.FilePath, counterExampleFile.FilePath, checkArgument);
					Run();

					var success = InterpretExitCode(_ltsMin.ExitCode);
					return new AnalysisResult(success ? null : GetCounterExample(modelFile.FilePath, counterExampleFile.FilePath), 0, 0, 0);
				}
			}
			finally
			{
				_ltsMin = null;
			}
		}

		/// <summary>
		///   Outputs the counter example found by the model checker.
		/// </summary>
		private static CounterExample GetCounterExample(string modelFile, string counterExampleFile)
		{
			return CounterExample.LoadLtsMin(File.ReadAllBytes(modelFile), counterExampleFile);
		}

		/// <summary>
		///   Creates a new <see cref="_ltsMin" /> process instance that checks the <paramref name="modelFile" />.
		/// </summary>
		/// <param name="modelFile">The model that should be checked.</param>
		/// <param name="counterExampleFile">The path to the file that should store the counter example.</param>
		/// <param name="checkArgument">The argument passed to LtsMin that indicates which kind of check to perform.</param>
		private void CreateProcess(string modelFile, string counterExampleFile, string checkArgument)
		{
			Requires.That(_ltsMin == null, "An instance of LtsMin is already running.");

			_ltsMin = new ExternalProcess(
				fileName: "pins2lts-seq.exe",
				commandLineArguments:
					$"--loader=SafetySharp.LtsMin.Sequential.dll \"{modelFile}\" {checkArgument} --trace=\"{counterExampleFile}\"",
				outputCallback: output => Output(output.Message));
		}

		/// <summary>
		///   Runs the <see cref="_ltsMin" /> process instance.
		/// </summary>
		private void Run()
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			_ltsMin.Run();

			stopwatch.Stop();

			Output(String.Empty);
			Output("=====================================");
			Output($"Elapsed time: {stopwatch.Elapsed}");
			Output("=====================================");
			Output(String.Empty);
		}
	}
}