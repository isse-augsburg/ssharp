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
	using System.ComponentModel;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;
	using ModelChecking;
	/// <summary>
	///   Represents the LtsMin model checker.
	/// </summary>
	public class LtsMin
	{
		/// <summary>
		///   The unique name of the construction state.
		/// </summary>
		internal const string ConstructionStateName = "constructionState259C2EE0D9884B92989DF442BA268E8E";

		/// <summary>
		///   Represents the LtsMin process that is currently running.
		/// </summary>
		private ExternalProcess _ltsMin;

		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;

		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		internal AnalysisResult<SafetySharpRuntimeModel> CheckInvariant(SafetySharpRuntimeModel model, Formula invariant)
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
				$"--invariant=\"({ConstructionStateName} == 1) || ({transformationVisitor.TransformedFormula})\"");
		}
		

		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		public AnalysisResult<SafetySharpRuntimeModel> Check(SafetySharpRuntimeModel model, Formula formula)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsLtlFormulaVisitor();
			visitor.Visit(formula);

			if (!visitor.IsLtlFormula)
				throw new NotSupportedException("CTL model checking is currently not supported with LtsMin.");

			var transformationVisitor = new LtsMinLtlTransformer();
			transformationVisitor.Visit(new UnaryFormula(formula, UnaryOperator.Next));
			
			return Check(model,  formula, $"--ltl=\"{transformationVisitor.TransformedFormula}\"");
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
		/// <param name="checkArgument">The argument passed to LtsMin that indicates which kind of check to perform.</param>
		private AnalysisResult<SafetySharpRuntimeModel> Check(SafetySharpRuntimeModel model, Formula formula, string checkArgument)
		{
			try
			{
				using (var modelFile = new TemporaryFile("ssharp"))
				{
					File.WriteAllBytes(modelFile.FilePath, RuntimeModelSerializer.Save(model.Model, formula));

					try
					{
						CreateProcess(modelFile.FilePath, checkArgument);
						Run();
					}
					catch (Win32Exception e)
					{
						throw new InvalidOperationException(
							"Failed to start LTSMin. Ensure that pins2lts-seq.exe can be found by either copying it next " +
							"to the executing assembly or by adding it to the system path. The required cygwin dependencies " +
							$"must also be available. The original error message was: {e.Message}", e);
					}

					var success = InterpretExitCode(_ltsMin.ExitCode);
					return new AnalysisResult<SafetySharpRuntimeModel> { FormulaHolds = success };
				}
			}
			finally
			{
				_ltsMin = null;
			}
		}

		/// <summary>
		///   Creates a new <see cref="_ltsMin" /> process instance that checks the <paramref name="modelFile" />.
		/// </summary>
		/// <param name="modelFile">The model that should be checked.</param>
		/// <param name="checkArgument">The argument passed to LtsMin that indicates which kind of check to perform.</param>
		private void CreateProcess(string modelFile, string checkArgument)
		{
			Requires.That(_ltsMin == null, "An instance of LtsMin is already running.");

			var loaderAssembly = Path.Combine(Environment.CurrentDirectory, "SafetySharp.LtsMin.dll");

			_ltsMin = new ExternalProcess(
				fileName: "pins2lts-seq.exe",
				commandLineArguments: $"--loader=\"{loaderAssembly}\" \"{modelFile}\" {checkArgument}",
				outputCallback: output => OutputWritten?.Invoke(output))
			{
				WorkingDirectory = Environment.CurrentDirectory
			};
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

			OutputWritten?.Invoke(String.Empty);
			OutputWritten?.Invoke("=====================================");
			OutputWritten?.Invoke($"Elapsed time: {stopwatch.Elapsed}");
			OutputWritten?.Invoke("=====================================");
			OutputWritten?.Invoke(String.Empty);
		}
	}
}