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
using System.Text;

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System.Globalization;
	using System.IO;
	using Utilities;

	class PrismProcess
	{
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
		

		public class PrismOutput
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
			public double ResultingProbability;
			public IEnumerable<string> CompleteOutput;
		}

		private TextWriter _output;

		private PrismOutput ParseOutput(List<string> inputLines)
		{
			foreach (var inputLine in inputLines)
			{
				if (inputLine.StartsWith("Error:"))
					throw new Exception("Prism-" + inputLine);
			}
			var doComputingRemainingProbabilities = false;
			foreach (var inputLine in inputLines)
			{
				if (inputLine.StartsWith(_textRemainingProbabilities))
					doComputingRemainingProbabilities = true;
			}

			//var inputLines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			var enumerator = ((IEnumerable<string>)inputLines).GetEnumerator();
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
				(pattern, stringBuilder) =>
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
			if (!parseUntilIncludingString(_textDelimiter, null))
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
			if (!parseUntilIncludingRegex(_parserSatisfyingValueInInitialState, null))
				throw new Exception("Parsing prism output failed");
			var valueInInitialState = match.Groups["valueInInitialState"].Value;
			var resultingProbability = double.Parse(valueInInitialState, CultureInfo.InvariantCulture);

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
				ResultingProbability = resultingProbability,
				CompleteOutput = inputLines
			};
		}

		private readonly List<string> _prismProcessOutput = new List<string>();

		public PrismProcess(TextWriter output)
		{
			_output = output;
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
			if (path == null)
				throw new Exception("Set the environmental variable PRISM_DIR to the PRISM top level directory. You can download PRISM from http://www.prismmodelchecker.org/");
			var pathOfBin = System.IO.Path.Combine(path, "bin", "prism.bat");
			if (!System.IO.File.Exists(pathOfBin))
				throw new Exception("Environmental variable PRISM_DIR should point to the PRISM top level directory. You can download PRISM from http://www.prismmodelchecker.org/");
			return path;
		}

		private void CheckPrismVersion(string pathToJavaExe, string pathToPrism)
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
			var prismIsInPath = Array.Exists(dirsInPath, elem => elem == prismLibDir);
			if (!prismIsInPath)
			{
				// Add libdir to PATH
				// http://stackoverflow.com/questions/2998343/adding-a-directory-temporarily-to-windows-7s-dll-search-paths
				System.Environment.SetEnvironmentVariable("PATH", System.Environment.GetEnvironmentVariable("PATH") + ";" + prismLibDir);
			}
		}

		private ExternalProcess ExecutePrismProcess(string prismArguments)
		{
			var javaExe = FindJava();
			var prismDir = FindPrism();
			CheckPrismVersion(javaExe, prismDir);
			AddPrismLibToPath(prismDir);

			var classpath = string.Format(@"{0}\lib\prism.jar;{0}\classes;{0};{0}\lib\pepa.zip;{0}\lib\*", prismDir);

			var argument = $"-Djava.library.path=\"{prismDir}\\lib\" -classpath \"{classpath}\" prism.PrismCL {prismArguments}";

			Action<string> addToOutput;
			if (_output==null)
				addToOutput = (output) => _prismProcessOutput.Add(output);
			else
				addToOutput = (output) => { _output.WriteLine(output); _prismProcessOutput.Add(output); };

			var process = new ExternalProcess(javaExe, argument, addToOutput);
			return process;
		}

		public PrismOutput ExecutePrismAndParseResult(string prismArguments)
		{
			var process = ExecutePrismProcess(prismArguments);
			_prismProcessOutput.Clear();
			process.Run();

			var result = ParseOutput(_prismProcessOutput);
			return result;
		}
	}
}
