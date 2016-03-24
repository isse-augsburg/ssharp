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
	using System.Runtime.Remoting.Metadata.W3cXsd2001;
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
	public class Mrmc : IDisposable
	{
		private object _probabilityMatrixCreationStarted = false;
		private bool _probabilityMatrixWasCreated = false;

		private CompactProbabilityMatrix CompactProbabilityMatrix;

		private TemporaryFile FileTransitions;
		private TemporaryFile FileStateLabelings;

		private ModelBase _model;
		private readonly ConcurrentBag<Formula> _formulasToCheck = new ConcurrentBag<Formula>();
		
		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;
		

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public Mrmc(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));
			_model = model;
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

				try
				{
					var sparseProbabilityMatrix = checker.CreateProbabilityMatrix();
					var derivedProbabilityMatrix = sparseProbabilityMatrix.DeriveCompactProbabilityMatrix();
					//var compactToSparse = CompactProbabilityMatrix = derivedProbabilityMatrix.Item1;
					CompactProbabilityMatrix = derivedProbabilityMatrix.Item2;
				}
				finally
				{
					var creationTime = stopwatch.Elapsed;
					stopwatch.Stop();

					if (true) //Configuration.ProgressReportsOnly
					{
						OutputWritten?.Invoke(String.Empty);
						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke($"Initialization time: {initializationTime}");
						OutputWritten?.Invoke($"Probability matrix creation time: {creationTime}");
						//OutputWritten?.Invoke($"{(int)(_probabilityMatrix.StateCount / stopwatch.Elapsed.TotalSeconds):n0} states per second");
						//OutputWritten?.Invoke($"{(int)(_probabilityMatrix.TransitionCount / stopwatch.Elapsed.TotalSeconds):n0} transitions per second");
						OutputWritten?.Invoke("===============================================");
						OutputWritten?.Invoke(String.Empty);
					}
				}
			}
			WriteProbabilityMatrixToDisk();

			_probabilityMatrixWasCreated = true;
			Interlocked.MemoryBarrier();
		}

		private void WriteProbabilityMatrixToDisk()
		{

			FileTransitions = new TemporaryFile("tra");
			FileStateLabelings = new TemporaryFile("lab");

			var streamTransitions = new StreamWriter(FileTransitions.FilePath);
			streamTransitions.NewLine = "\n";
			var streamStateLabelings = new StreamWriter(FileStateLabelings.FilePath);
			streamStateLabelings.NewLine = "\n";

			streamTransitions.WriteLine("STATES "+CompactProbabilityMatrix.States);
			streamTransitions.WriteLine("TRANSITIONS " + CompactProbabilityMatrix.Transitions.Count);
			foreach (var transition in CompactProbabilityMatrix.Transitions)
			{
				streamTransitions.WriteLine(transition.SourceState + " " + transition.TargetState + " " + transition.Probability);
			}
			streamTransitions.Flush();
			streamTransitions.Close();

			streamStateLabelings.WriteLine("#DECLARATION");
			//bool firstElement = true;
			for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
			{
				if (i > 0)
				{
					streamStateLabelings.Write(" ");
				}
				streamStateLabelings.Write("formula"+i);
			}
			streamStateLabelings.WriteLine();
			streamStateLabelings.WriteLine("#END");
			foreach (var stateFormulaSet in CompactProbabilityMatrix.StateLabeling)
			{
				streamStateLabelings.Write(stateFormulaSet.Key);
				//stateFormulaSet.Value.
				for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
				{
					if (stateFormulaSet.Value[i])
						streamStateLabelings.Write(" formula" + i);
				}
				streamStateLabelings.WriteLine();
			}
			streamStateLabelings.Flush();
			streamStateLabelings.Close();

		}

		public Func<Probability> CalculateProbabilityToReachStates(Formula formulaValidInRequestedStates)
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

			Func<Probability> executeCalculation = () => ExecuteCalculation(formulaToCheck);
			return executeCalculation;
		}

		private System.Text.RegularExpressions.Regex MrmcResultParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<probability>[0-1]\\.?[0-9]+)$");

		

		private Probability ExecuteCalculation(Formula formulaToCheck)
		{
			Interlocked.MemoryBarrier();
			Requires.That(_probabilityMatrixWasCreated, nameof(CreateProbabilityMatrix) + "must be called before");


			using (var fileResults = new TemporaryFile("res"))
			using (var fileCommandScript = new TemporaryFile("cmd"))
			{
				var script = new StringBuilder();
				script.AppendLine("set method_path gauss_jacobi");
				script.AppendLine("P { > 0 } [ tt U formula0 ]");
				script.AppendLine("write_res_file 1");
				script.AppendLine("quit");
				
				File.WriteAllText(fileCommandScript.FilePath,script.ToString());

				var commandlinearguments = "dtmc " + FileTransitions.FilePath + " " + FileStateLabelings.FilePath + " " + fileCommandScript.FilePath + " " + fileResults.FilePath;

				var mrmc = new ExternalProcess("mrmc.exe", commandlinearguments);
				mrmc.Run();

				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();

				var index = 0;
				var probability = Probability.Zero;
				while (resultEnumerator.MoveNext())
				{
					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcResultParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = Probability.One;
							var probabilityInState = Double.Parse(parsed.Groups["probability"].Value,CultureInfo.InvariantCulture);
							probability += probabilityOfState * probabilityInState;
						}
						else
						{
							throw new Exception("Expected different output of MRMC");
						}
					}
				}
				return probability;
			}
		}
		

		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;
		
		
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
					throw new InvalidOperationException($"Mrmc exited with an unexpected exit code: {exitCode}.");
			}
		}

		/*
		/// <summary>
		///   Checks whether the <paramref name="formula" /> holds in all states of the <paramref name="model" />.
		/// </summary>
		/// <param name="model">The model that should be checked.</param>
		/// <param name="formula">The formula that should be checked.</param>
		/// <param name="checkArgument">The argument passed to LtsMin that indicates which kind of check to perform.</param>
		private AnalysisResult Check(ModelBase model, Formula formula, string checkArgument)
		{
			try
			{
				using (var modelFile = new TemporaryFile("ssharp"))
				{
					File.WriteAllBytes(modelFile.FilePath, RuntimeModelSerializer.Save(model, formula));

					try
					{
						CreateProcess(modelFile.FilePath, checkArgument);
						Run();
					}
					catch (Win32Exception e)
					{
						throw new InvalidOperationException(
							"Failed to start MRMC. Ensure that mrmc.exe can be found by either copying it next " +
							"to the executing assembly or by adding it to the system path. The required cygwin dependencies " +
							$"must also be available. The original error message was: {e.Message}", e);
					}

					var success = InterpretExitCode(_ltsMin.ExitCode);
					return new AnalysisResult(success, null, 0, 0, 0, 0);
				}
			}
			finally
			{
				mrmc = null;
			}
		}
		*/

		/*
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
				outputCallback: output => OutputWritten?.Invoke(output.Message))
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
		}*/

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			FileTransitions.SafeDispose();
			FileStateLabelings.SafeDispose();
		}
	}


}