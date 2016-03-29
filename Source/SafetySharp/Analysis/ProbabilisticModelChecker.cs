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
	using Modeling;
	using Utilities;
	using System.IO;
	using System.Globalization;
	using System.Text;
	using System.Threading;

	/// <summary>
								///   Represents a base class for external probabilistic model checker tools.
								/// </summary>
	public abstract class ProbabilisticModelChecker : IDisposable
	{
		public ProbabilityChecker ProbabilityChecker { get; }

		internal CompactProbabilityMatrix CompactProbabilityMatrix => ProbabilityChecker.CompactProbabilityMatrix;

		protected ProbabilisticModelChecker(ProbabilityChecker probabilityChecker)
		{
			ProbabilityChecker = probabilityChecker;
		}

		public abstract void Dispose();

		internal abstract Probability ExecuteCalculation(Formula formulaToCheck);
	}

	public class Mrmc : ProbabilisticModelChecker
	{

		private TemporaryFile FileTransitions;
		private TemporaryFile FileStateLabelings;

		public Mrmc(ProbabilityChecker probabilityChecker) : base (probabilityChecker)
		{
		}
		
		private void WriteProbabilityMatrixToDisk()
		{

			FileTransitions = new TemporaryFile("tra");
			FileStateLabelings = new TemporaryFile("lab");

			var streamTransitions = new StreamWriter(FileTransitions.FilePath);
			streamTransitions.NewLine = "\n";
			var streamStateLabelings = new StreamWriter(FileStateLabelings.FilePath);
			streamStateLabelings.NewLine = "\n";

			streamTransitions.WriteLine("STATES " + CompactProbabilityMatrix.States);
			streamTransitions.WriteLine("TRANSITIONS " + CompactProbabilityMatrix.Transitions.Count);
			foreach (var transition in CompactProbabilityMatrix.Transitions)
			{
				streamTransitions.WriteLine((transition.SourceState + 1) + " " + (transition.TargetState + 1) + " " + transition.Probability);
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
				streamStateLabelings.Write("formula" + i);
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

		private System.Text.RegularExpressions.Regex MrmcResultParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<probability>[0-1]\\.?[0-9]+)$");


		internal override Probability ExecuteCalculation(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();


			using (var fileResults = new TemporaryFile("res"))
			using (var fileCommandScript = new TemporaryFile("cmd"))
			{
				var script = new StringBuilder();
				script.AppendLine("set method_path gauss_jacobi");
				script.AppendLine("P { > 0 } [ tt U formula0 ]");
				script.AppendLine("write_res_file 1");
				script.AppendLine("quit");

				File.WriteAllText(fileCommandScript.FilePath, script.ToString());

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
							var probabilityInState = Double.Parse(parsed.Groups["probability"].Value, CultureInfo.InvariantCulture);
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
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			FileTransitions.SafeDispose();
			FileStateLabelings.SafeDispose();
		}
	}


	public class Cadp : ProbabilisticModelChecker
	{

		private TemporaryFile FileAut;

		public Cadp(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}
		

		private void WriteProbabilityMatrixToDisk()
		{

			FileAut = new TemporaryFile("aut");

			var streamAut = new StreamWriter(FileAut.FilePath);
			streamAut.NewLine = "\n";

			streamAut.WriteLine("STATES " + CompactProbabilityMatrix.States);
			streamAut.WriteLine("TRANSITIONS " + CompactProbabilityMatrix.Transitions.Count);
			foreach (var transition in CompactProbabilityMatrix.Transitions)
			{
				streamAut.WriteLine((transition.SourceState + 1) + " " + (transition.TargetState + 1) + " " + transition.Probability);
			}
			streamAut.Flush();
			streamAut.Close();
		}
		
		internal override Probability ExecuteCalculation(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();

			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			FileAut.SafeDispose();
		}
	}



	public class Prism : ProbabilisticModelChecker
	{

		private TemporaryFile FileModel ;

		public Prism(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}


		private void WriteProbabilityMatrixToDisk()
		{

			FileModel = new TemporaryFile("pm");

			var streamAut = new StreamWriter(FileModel.FilePath);
			streamAut.NewLine = "\n";

			streamAut.WriteLine("STATES " + CompactProbabilityMatrix.States);
			streamAut.WriteLine("TRANSITIONS " + CompactProbabilityMatrix.Transitions.Count);
			foreach (var transition in CompactProbabilityMatrix.Transitions)
			{
				streamAut.WriteLine((transition.SourceState + 1) + " " + (transition.TargetState + 1) + " " + transition.Probability);
			}
			streamAut.Flush();
			streamAut.Close();
		}

		internal override Probability ExecuteCalculation(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();

			throw new NotImplementedException();
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			FileModel.SafeDispose();
		}
	}
}