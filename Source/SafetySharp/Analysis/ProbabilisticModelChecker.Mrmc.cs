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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SafetySharp.Analysis
{
	using System.Globalization;
	using System.IO;
	using FormulaVisitors;
	using Modeling;
	using Utilities;

	public class Mrmc : ProbabilisticModelChecker
	{

		private TemporaryFile _fileTransitions;
		private TemporaryFile _fileStateLabelings;

		public Mrmc(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}

		private void WriteProbabilityMatrixToDisk()
		{

			_fileTransitions = new TemporaryFile("tra");
			_fileStateLabelings = new TemporaryFile("lab");

			var streamTransitions = new StreamWriter(_fileTransitions.FilePath);
			streamTransitions.NewLine = "\n";
			var streamStateLabelings = new StreamWriter(_fileStateLabelings.FilePath);
			streamStateLabelings.NewLine = "\n";

			streamTransitions.WriteLine("STATES " + CompactProbabilityMatrix.States);
			streamTransitions.WriteLine("TRANSITIONS " + CompactProbabilityMatrix.NumberOfTransitions);
			foreach (var transitionList in CompactProbabilityMatrix.TransitionGroups)
			{
				var sourceState = transitionList.Key;
				foreach (var transition in transitionList.Value)
				{
					streamTransitions.WriteLine(sourceState + " " + transition.State + " " + transition.Probability);
				}
			}
			streamTransitions.Flush();
			streamTransitions.Close();

			streamStateLabelings.WriteLine("#DECLARATION");
			//bool firstElement = true;
			for (var i = 0; i < CompactProbabilityMatrix.NoOfStateFormulaLabels; i++)
			{
				var label = CompactProbabilityMatrix.StateFormulaLabels[i];
				if (i > 0)
				{
					streamStateLabelings.Write(" ");
				}
				streamStateLabelings.Write(label);
			}
			streamStateLabelings.WriteLine();
			streamStateLabelings.WriteLine("#END");
			foreach (var stateFormulaSet in CompactProbabilityMatrix.StateLabeling)
			{
				streamStateLabelings.Write(stateFormulaSet.Key);
				//stateFormulaSet.Value.
				for (var i = 0; i < CompactProbabilityMatrix.NoOfStateFormulaLabels; i++)
				{
					var label = CompactProbabilityMatrix.StateFormulaLabels[i];
					if (stateFormulaSet.Value[i])
						streamStateLabelings.Write(" " + label);
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

			//var formulaAsString = "P { > 0 } [ tt U formula0 ]";
			var transformationVisitor = new MrmcTransformer();
			transformationVisitor.Visit(formulaToCheck);

			var formulaAsString = "P { > 0 } [ tt U "+ transformationVisitor.TransformedFormula +" ]";

			var initialStatesWithProbabilities = ProbabilityChecker.CompactProbabilityMatrix.InitialStates;
			var initialStates = initialStatesWithProbabilities.Select(tuple => tuple.State);
			var probabilities = initialStatesWithProbabilities.Select(tuple => tuple.Probability);

			using (var fileResults = new TemporaryFile("res"))
			using (var fileCommandScript = new TemporaryFile("cmd"))
			{
				var script = new StringBuilder();
				script.AppendLine("set method_path gauss_jacobi");
				script.AppendLine(formulaAsString);
				script.Append("write_res_file");
				foreach (var initialState in initialStates)
				{
					script.Append(" " + initialState);
				}
				script.AppendLine();
				script.AppendLine("quit");

				File.WriteAllText(fileCommandScript.FilePath, script.ToString());

				var commandlinearguments = "dtmc " + _fileTransitions.FilePath + " " + _fileStateLabelings.FilePath + " " + fileCommandScript.FilePath + " " + fileResults.FilePath;

				var mrmc = new ExternalProcess("mrmc.exe", commandlinearguments);
				mrmc.Run();

				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();

				var index = 0;
				var probability = Probability.Zero;
				var probabilityEnumerator = probabilities.GetEnumerator();
				while (resultEnumerator.MoveNext())
				{
					probabilityEnumerator.MoveNext();

					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcResultParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = probabilityEnumerator.Current;
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
			_fileTransitions.SafeDispose();
			_fileStateLabelings.SafeDispose();
		}
	}
}
