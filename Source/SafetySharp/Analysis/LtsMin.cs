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
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using FormulaVisitors;
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
		///   Gets or sets a value indicating whether a symbolic or an explicit state model checking algorithm should be used.
		/// </summary>
		public bool SymbolicChecking { get; set; }

		/// <summary>
		///   Gets the outputs that occurred during the execution of LtsMin.
		/// </summary>
		public IEnumerable<string> Outputs { get; private set; }

		/// <summary>
		///   Checks whether the <paramref name="hazard" /> occurs in any state of the <paramref name="model" />. Returns a
		///   <see cref="CounterExample" /> if the hazard occurs, <c>null</c> otherwise.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="hazard">[LiftExpression] The hazard that should be checked.</param>
		public override CounterExample CheckHazard(Model model, Func<bool> hazard)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(hazard, nameof(hazard));

			var stateFormula = new StateFormula(hazard);
			return Check(model, stateFormula, $"--invariant=\"!{stateFormula.Label}\"");
		}

		/// <summary>
		///   Checks whether the <paramref name="invariant" /> holds in all states of the <paramref name="model" />. Returns a
		///   <see cref="CounterExample" /> if the invariant is violated, <c>null</c> otherwise.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="invariant">The invariant that should be checked.</param>
		public override CounterExample CheckInvariant(Model model, Func<bool> invariant)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(invariant, nameof(invariant));

			var stateFormula = new StateFormula(invariant);
			return Check(model, stateFormula, $"--invariant=\"{stateFormula.Label}\"");
		}

		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		public override CounterExample Check(Model model, Formula formula)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsLtlFormulaVisitor();
			visitor.Visit(formula);

			if (visitor.IsLtlFormula)
			{
				var transformationVisitor = new LtsMinLtlTransformer();
				transformationVisitor.Visit(formula);

				return Check(model, formula, $"--ltl=\"{transformationVisitor.TransformedFormula}\"");
			}
			else
			{
				var transformationVisitor = new LtsMinMuCalculusTransformer();
				transformationVisitor.Visit(formula);

				return Check(model, formula, $"--ltl=\"{transformationVisitor.TransformedFormula}\"");
			}
		}

		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten;

		/// <summary>
		///   Gets the name of the LtsMin executable.
		/// </summary>
		private string GetExecutableName()
		{
			return SymbolicChecking ? "pins2lts-sym.exe" : "pins2lts-seq.exe";
		}

		/// <summary>
		///   Gets the name of the S# LtsMin assembly.
		/// </summary>
		private string GetSafetySharpLtsMinAssemblyName()
		{
			return SymbolicChecking ? "SafetySharp.LtsMin.Symbolic.dll" : "SafetySharp.LtsMin.Sequential.dll";
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
		private CounterExample Check(Model model, Formula formula, string checkArgument)
		{
			try
			{
				Outputs = Enumerable.Empty<string>();

				using (var modelFile = new TemporaryFile("ssharp"))
				using (var counterExampleFile = new TemporaryFile("gcf"))
				{
					using (var stream = new FileStream(modelFile.FilePath, FileMode.Create))
						RuntimeModelSerializer.Save(stream, model, formula);

					CreateProcess(modelFile.FilePath, counterExampleFile.FilePath, checkArgument);
					Run();

					Outputs = _ltsMin.Outputs.Select(output => output.Message).ToArray();
					var success = InterpretExitCode(_ltsMin.ExitCode);

					if (success)
						return null;

					return GetCounterExample(modelFile.FilePath, counterExampleFile.FilePath);
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
			using (var stream = new FileStream(modelFile, FileMode.Open))
			{
				var model = RuntimeModelSerializer.Load(stream);
				var counterExample = new CounterExample(model);
				counterExample.LoadLtsMin(counterExampleFile);
				return counterExample;
			}
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

			var assembly = GetSafetySharpLtsMinAssemblyName();
			_ltsMin = new ExternalProcess(
				fileName: GetExecutableName(),
				commandLineArguments: $"--loader={assembly} \"{modelFile}\" {checkArgument} --trace=\"{counterExampleFile}\"",
				outputCallback: output => Output(output.Message));
		}

		/// <summary>
		///   Forwards the output <paramref name="message" />.
		/// </summary>
		/// <param name="message">The message that should be output.</param>
		private void Output(string message)
		{
			Console.WriteLine(message);
			OutputWritten?.Invoke(message);
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
			Output("==========================");
			Output($"Elapsed time: {stopwatch.Elapsed.TotalMilliseconds}ms");
			Output("==========================");
			Output(String.Empty);
		}
	}
}