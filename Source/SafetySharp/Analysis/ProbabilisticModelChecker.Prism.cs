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
	using Runtime.Serialization;
	using Utilities;

	public class Prism : ProbabilisticModelChecker
	{
		public Prism(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}

		/************************************************/
		/*            OUTPUT PARSER                     */
		/************************************************/

		private readonly string _textDelimiter = @"---------------------------------------------------------------------";
		private readonly System.Text.RegularExpressions.Regex _parserFormula = new System.Text.RegularExpressions.Regex(@"Model checking: (?<formula>.*)");
		private readonly System.Text.RegularExpressions.Regex _parserConstants = new System.Text.RegularExpressions.Regex(@"Property constants: (?<constants>.*)");
		private readonly System.Text.RegularExpressions.Regex _parserProb0 = new System.Text.RegularExpressions.Regex(@"Prob0: (?<prob0>.*)");
		private readonly System.Text.RegularExpressions.Regex _parserProb1 = new System.Text.RegularExpressions.Regex(@"Prob1: (?<prob1>.*)");
		private readonly System.Text.RegularExpressions.Regex _parserYesNoMaybe = new System.Text.RegularExpressions.Regex(@"yes = (?<yes>.*), no = (?<no>.*), maybe = (?<maybe>.*)");
		private readonly string _textRemainingProbabilities = @"Computing remaining probabilities...";
		private readonly System.Text.RegularExpressions.Regex _parserEngine = new System.Text.RegularExpressions.Regex(@"Engine: (?<engine>.*)");
		private readonly string _textStartingIterations = @"Starting iterations...";
		private readonly System.Text.RegularExpressions.Regex _parserSatisfyingStates = new System.Text.RegularExpressions.Regex(@"Number of states satisfying.*: (?<satisfyingStates>.*)");
		private readonly System.Text.RegularExpressions.Regex _parserSatisfyingSatisfiedInInitialStates = new System.Text.RegularExpressions.Regex(@"Property satisfied in (?<satisfyingInitialStates>.*) of(?<initialStates>.*) initial states[.]");
		private readonly System.Text.RegularExpressions.Regex _parserSatisfyingValueInInitialState = new System.Text.RegularExpressions.Regex(@"Value in the initial state: (?<valueInInitialState>.*)");
		private readonly System.Text.RegularExpressions.Regex _parserTimeMc = new System.Text.RegularExpressions.Regex(@"Time for model checking: (?<timeMc>.*)[.]");
		private readonly System.Text.RegularExpressions.Regex _parserResult = new System.Text.RegularExpressions.Regex(@"Result: (?<result>.+?)");

		private class PrismResult
		{
		}

		private class PrismResultQualitative : PrismResult
		{
			public string SatisfyingStates;
			public string SatisfyingInitialStates;
			public string InitialStates;
			public string Result;
			public string Probabilities;
		}

		private class PrismResultQuantitative : PrismResult
		{
			public double Result;
		}

		private class PrismOutput
		{
			public string Formula;
			//public string Constants;
			//public string Prob0;
			//public string Prob1;
			public string Yes;
			public string No;
			public string Maybe;
			//public string Engine;
			//public string Enginestats;
			//public string Iterations;
			public string TimeMc;
			public PrismResult Result;
		}

		private PrismOutput ParseOutput(List<string> inputLines)
		{
			foreach (var inputLine in inputLines)
			{
				if (inputLine.StartsWith("Error:"))
					throw new Exception("Prism-"+inputLine);
			}
			var doComputingRemainingProbabilities = false;
			foreach (var inputLine in inputLines)
			{
				if (inputLine.StartsWith(_textRemainingProbabilities))
					doComputingRemainingProbabilities = true;
			}

			//var inputLines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			var enumerator = ((IEnumerable<string>) inputLines).GetEnumerator();
			System.Text.RegularExpressions.Match match = null;

			string peekedValue = null;

			Func<System.Text.RegularExpressions.Regex, StringBuilder, bool> parseUntilExcludingRegex =
				(pattern, stringBuilder) =>
				{
					while (enumerator.MoveNext())
					{
						peekedValue = enumerator.Current;
						match = pattern.Match(peekedValue);
						if (match.Success)
							return true;
						stringBuilder?.AppendLine(enumerator.Current);
					}
					return false;
				};
			Func<System.Text.RegularExpressions.Regex, StringBuilder, bool> parseUntilIncludingRegex =
				(pattern,stringBuilder) =>
				{
					if (peekedValue != null)
					{
						// first consume peekedValue
						match = pattern.Match(peekedValue);
						if (match.Success)
							return true;
						stringBuilder?.AppendLine(peekedValue);
						peekedValue = null;
					}
					while (enumerator.MoveNext())
					{
						match = pattern.Match(enumerator.Current);
						if (match.Success)
							return true;
						stringBuilder?.AppendLine(enumerator.Current);
					}
					return false;
				};
			Func<string, StringBuilder, bool> parseUntilIncludingString =
				(pattern, stringBuilder) =>
				{
					if (peekedValue != null)
					{
						// first consume peekedValue
						if (string.Equals(peekedValue, pattern))
							return true;
						stringBuilder?.AppendLine(peekedValue);
						peekedValue = null;
					}
					while (enumerator.MoveNext())
					{
						if (string.Equals(enumerator.Current, pattern))
							return true;
						stringBuilder?.AppendLine(enumerator.Current);
					}
					return false;
				};
			
			// Parse until delimiter
			if (!parseUntilIncludingString(_textDelimiter,null))
				throw new Exception("Parsing prism output failed");

			// Parse "Model checking: /formula/"
			if (!parseUntilIncludingRegex(_parserFormula, null))
				throw new Exception("Parsing prism output failed");
			var formula = match.Groups["formula"].Value;

			/*
			// Parse "Prob0: : /prob0/"
			if (!parseUntilIncludingRegex(_parserProb0, null))
				throw new Exception("Parsing prism output failed");
			var prob0 = match.Groups["prob0"].Value;
			*/

			/*
			// Parse "Prob1: : /prob1/"
			if (!parseUntilIncludingRegex(_parserProb1, null))
				throw new Exception("Parsing prism output failed");
			var prob1 = match.Groups["prob1"].Value;
			*/
			
			// Parse "yes = /yes/, no = /no/, maybe = /maybe/"
			if (!parseUntilIncludingRegex(_parserYesNoMaybe, null))
				throw new Exception("Parsing prism output failed");
			var yes = match.Groups["yes"].Value;
			var no = match.Groups["no"].Value;
			var maybe = match.Groups["maybe"].Value;

			if (doComputingRemainingProbabilities)
			{
				// Parse "Computing remaining probabilities..."
				if (!parseUntilIncludingString(_textRemainingProbabilities, null))
					throw new Exception("Parsing prism output failed");

				// Parse "Engine: : /engine/"
				if (!parseUntilIncludingRegex(_parserEngine, null))
					throw new Exception("Parsing prism output failed");
				var engine = match.Groups["engine"].Value;

				// Parse engineStats until including "Starting iterations..."
				var engineStatsStringBuilder = new StringBuilder();
				if (!parseUntilIncludingString(_textStartingIterations, engineStatsStringBuilder))
					throw new Exception("Parsing prism output failed");
				var enginestats = engineStatsStringBuilder.ToString();


				// Parse iterations until excluding _parserSatisfyingValueInInitialState
				var iterationsStringBuilder = new StringBuilder();
				if (!parseUntilExcludingRegex(_parserSatisfyingValueInInitialState, iterationsStringBuilder))
					throw new Exception("Parsing prism output failed");
				var iterations = iterationsStringBuilder.ToString();
			}

			// Now here is a split. Either the Quantitative Value was calculated or the qualitative.
			// TODO: implement qualitative
			//branch quantitative
			PrismResult result = null;
			if (!parseUntilIncludingRegex(_parserSatisfyingValueInInitialState, null))
				throw new Exception("Parsing prism output failed");
			var valueInInitialState = match.Groups["valueInInitialState"].Value;
			result = new PrismResultQuantitative()
			{
				Result = double.Parse(valueInInitialState, CultureInfo.InvariantCulture)
			};

			// Parse "Time for model checking: /timeMc/"
			if (!parseUntilIncludingRegex(_parserTimeMc, null))
				throw new Exception("Parsing prism output failed");
			var timeMc = match.Groups["timeMc"].Value;

			// Parse "Result: /engine/ ..."
			if (!parseUntilIncludingRegex(_parserResult, null))
				throw new Exception("Parsing prism output failed");
			var resultString = match.Groups["result"].Value;
			
			
			return new PrismOutput()
			{
				Formula = formula,
				//Constants = constants,
				//Prob0 = prob0,
				//Prob1 = prob1,
				Yes = yes,
				No = no,
				Maybe = maybe,
				//Engine = engine,
				//Enginestats = enginestats,
				//Iterations = iterations,
				TimeMc = timeMc,
				Result = result,
			};
		}

		/************************************************/
		/*         PRISM MODEL WRITER                   */
		/************************************************/

		// There are two simple ways to transform the state space into a pm file.
		// 1.) Add variables for each state label. Initial "active" labels
		//     depend on the initial states. Every time we enter a state, we
		//     set the state label, accordingly. Only reachable states are in the state space.
		//     State vector is quite big.
		// 2.) Add for every label a big formula which is defined as big OR above all
		//     states where the label is true. Formula might get big. Might be
		//     inefficient for Prism to evaluate the formula in each state
		// Currently, we use transformation 1 as it seems the better way. Don't really know which
		// transformation performs better. A third possibility is to use the computation
		// engines of Prism directly
		//  -> http://www.prismmodelchecker.org/manual/ConfiguringPRISM/ComputationEngines

		private TemporaryFile _filePrism;


		private void WriteCommandSourcePart(StreamWriter writer, int sourceState)
		{
			writer.Write("\t [] currentState=" + sourceState + " -> ");
		}

		private void WriteCommandTransitionToState(StreamWriter writer, TupleStateProbability transition, bool firstTransitionOfCommand)
		{
			if (!firstTransitionOfCommand)
				writer.Write(" + ");
			writer.Write(transition.Probability);
			writer.Write(":(");
			writer.Write("currentState'=");
			writer.Write(transition.State);
			writer.Write(")");

			var stateFormulaSet = CompactProbabilityMatrix.StateLabeling[transition.State];
			var noStateFormulaLabels = CompactProbabilityMatrix.StateFormulaLabels.Length;
			for (var i = 0; i < noStateFormulaLabels; i++)
			{
				var label = CompactProbabilityMatrix.StateFormulaLabels[i];
				writer.Write(" & (" + label + "' = ");
				if (stateFormulaSet[i])
					writer.Write("true");
				else
					writer.Write("false");
				writer.Write(")");
			}
		}

		private void WriteCommandEnd(StreamWriter writer)
		{
			writer.Write(";");
			writer.WriteLine();
		}

		private void WriteProbabilityMatrixToDisk()
		{
			_filePrism = new TemporaryFile("prism");

			var streamPrism = new StreamWriter(_filePrism.FilePath) { NewLine = "\n" };

			streamPrism.WriteLine("dtmc");
			streamPrism.WriteLine("");
			streamPrism.WriteLine("global currentState : [0.." + CompactProbabilityMatrix.States + "] init 0;"); // 0 is artificial initial state.

			foreach (var label in CompactProbabilityMatrix.StateFormulaLabels)
				{
				streamPrism.WriteLine("global " + label + " : bool init false;");
			}
			streamPrism.WriteLine("");
			streamPrism.WriteLine("module systemModule");

			// From artificial initial state to real initial states
			var artificialSourceState = 0;
			WriteCommandSourcePart(streamPrism, artificialSourceState);
			var firstTransitionOfCommand = true;
			foreach (var tupleStateProbability in CompactProbabilityMatrix.InitialStates)
			{
				WriteCommandTransitionToState(streamPrism, tupleStateProbability, firstTransitionOfCommand);
				firstTransitionOfCommand = false;
			}
			WriteCommandEnd(streamPrism);

			foreach (var transitionList in CompactProbabilityMatrix.TransitionGroups)
			{
				var sourceState = transitionList.Key;
				WriteCommandSourcePart(streamPrism, sourceState);
				firstTransitionOfCommand = true;
				foreach (var transition in transitionList.Value)
				{
					WriteCommandTransitionToState(streamPrism, transition, firstTransitionOfCommand);
					firstTransitionOfCommand = false;
				}
				WriteCommandEnd(streamPrism);
			}
			streamPrism.WriteLine("endmodule");

			streamPrism.Flush();
			streamPrism.Close();
		}

		/************************************************/
		/*         PRISM EXECUTION                      */
		/************************************************/

		private readonly List<string> _prismProcessOutput = new List<string>();

		internal override Probability CalculateProbability(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();
			
			var isFormulaReturningProbabilityVisitor = new IsFormulaReturningProbabilityVisitor();
			isFormulaReturningProbabilityVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningProbabilityVisitor.IsReturningProbability)
			{
				throw new Exception("expected formula which returns a probability");
			}

			var transformationVisitor = new PrismTransformer();
			transformationVisitor.Visit(formulaToCheck);
			var formulaToCheckString = transformationVisitor.TransformedFormula;

			using (var fileProperties = new TemporaryFile("props"))
			{
				File.WriteAllText(fileProperties.FilePath, formulaToCheckString);

				var prismArguments = _filePrism.FilePath + " " + fileProperties.FilePath;

				var prism = ExecutePrism(prismArguments);
				_prismProcessOutput.Clear();
				prism.Run();

				var result = ParseOutput(_prismProcessOutput);
				var quantitativeResult = (PrismResultQuantitative)result.Result;
				
				return new Probability(quantitativeResult.Result);
			}
		}

		internal override bool CalculateFormula(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		internal override RewardResult CalculateReward(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		private string FindJava()
		{
			var javaCandidate = "C:\\ProgramData\\Oracle\\Java\\javapath\\java.exe";
			if (!System.IO.File.Exists(javaCandidate))
				throw new Exception("Please install the JAVA runtime");
			return javaCandidate;
		}

		private string FindPrism()
		{
			var path = System.Environment.GetEnvironmentVariable("PRISM_DIR");
			if (path==null)
				throw new Exception("Set the environmental variable PRISM_DIR to the PRISM top level directory. You can download PRISM from http://www.prismmodelchecker.org/");
			var pathOfBin = System.IO.Path.Combine(path, "bin", "prism.bat");
			if (!System.IO.File.Exists(pathOfBin))
				throw new Exception("Environmental variable PRISM_DIR should point to the PRISM top level directory. You can download PRISM from http://www.prismmodelchecker.org/");
			return path;
		}

		private void CheckPrismVersion(string pathToJavaExe,string pathToPrism)
		{
			var javaMachineCode = ExternalProcess.GetDllMachineType(pathToJavaExe);
			var fileNameToPrismDll = System.IO.Path.Combine(pathToPrism, "lib", "prism.dll");
			var prismMachineCode = ExternalProcess.GetDllMachineType(fileNameToPrismDll);
			if (javaMachineCode != prismMachineCode)
			{
				throw new Exception("JAVA VM and PRISM version are not compiled for the same architecture.");
			}
		}

		private void AddPrismLibToPath(string pathToPrism)
		{
			var prismLibDir = System.IO.Path.Combine(pathToPrism, "lib");
			var dirsInPath = System.Environment.GetEnvironmentVariable("PATH").Split(';');
			var prismIsInPath = Array.Exists(dirsInPath,elem => elem==prismLibDir);
			if (!prismIsInPath)
			{
				// Add libdir to PATH
				// http://stackoverflow.com/questions/2998343/adding-a-directory-temporarily-to-windows-7s-dll-search-paths
				System.Environment.SetEnvironmentVariable("PATH", System.Environment.GetEnvironmentVariable("PATH") + ";" + prismLibDir);
			}
		}

		private ExternalProcess ExecutePrism(string prismArguments)
		{
			var javaExe = FindJava();
			var prismDir = FindPrism();
			CheckPrismVersion(javaExe, prismDir);
			AddPrismLibToPath(prismDir);

			var classpath = string.Format(@"{0}\lib\prism.jar;{0}\classes;{0};{0}\lib\pepa.zip;{0}\lib\*", prismDir);

			var argument = $"-Djava.library.path=\"{prismDir}\\lib\" -classpath \"{classpath}\" prism.PrismCL {prismArguments}";

			Action<string> addToOutput = (output) => _prismProcessOutput.Add(output);

			var process = new ExternalProcess(javaExe,argument, addToOutput);
			return process;
		}
		

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public override void Dispose()
		{
			_filePrism.SafeDispose();
		}
	}
}
