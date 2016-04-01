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
	using System.IO;
	using Utilities;

	public class Prism : ProbabilisticModelChecker
	{
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

		public Prism(ProbabilityChecker probabilityChecker) : base(probabilityChecker)
		{
		}

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
		private readonly System.Text.RegularExpressions.Regex _parserResult = new System.Text.RegularExpressions.Regex(@"Result: (?<result>.+?) \(.*\k<nl>");

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
			public string Result;
		}

		private class PrismOutput
		{
			public string Formula;
			public string Constants;
			public string Prob0;
			public string Prob1;
			public string Yes;
			public string No;
			public string Maybe;
			public string Engine;
			public string Enginestats;
			public string Iterations;
			public string TimeMc;
			public PrismResult Result;
		}

		private PrismOutput ParseResult(string input)
		{
			var inputLines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
			var enumerator = ((IEnumerable<string>) inputLines).GetEnumerator();
			var emptyLineReached = false;

			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;

			// Parse "Model checking: /formula/"
			enumerator.MoveNext();
			var formulaMatch = _parserFormula.Match(enumerator.Current);
			if (!formulaMatch.Success)
				return null;
			var formula = formulaMatch.Groups["formula"].Value;

			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;

			// Parse "Prob0: : /prob0/"
			enumerator.MoveNext();
			var prob0Match = _parserProb0.Match(enumerator.Current);
			if (!formulaMatch.Success)
				return null;
			var prob0 = formulaMatch.Groups["prob0"].Value;
			
			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;

			// Parse "Prob1: : /prob1/"
			enumerator.MoveNext();
			var prob1Match = _parserProb1.Match(enumerator.Current);
			if (!formulaMatch.Success)
				return null;
			var prob1 = formulaMatch.Groups["prob1"].Value;
			
			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;

			// Parse "yes = /yes/, no = /no/, maybe = /maybe/"
			enumerator.MoveNext();
			var yesNoMaybeMatch = _parserYesNoMaybe.Match(enumerator.Current);
			if (!formulaMatch.Success)
				return null;
			var yes = formulaMatch.Groups["yes"].Value;
			var no = formulaMatch.Groups["no"].Value;
			var maybe = formulaMatch.Groups["maybe"].Value;

			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;

			// Parse "Computing remaining probabilities..."
			enumerator.MoveNext();
			if (!string.Equals(enumerator.Current, _textRemainingProbabilities))
				return null;

			// Parse "Engine: : /engine/"
			enumerator.MoveNext();
			var engineMatch = _parserEngine.Match(enumerator.Current);
			if (!engineMatch.Success)
				return null;
			var engine = formulaMatch.Groups["engine"].Value;

			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;

			// Parse engineStats until empty line (which is also parsed)
			var engineStatsStringBuilder = new StringBuilder();
			emptyLineReached = false;
			while (!emptyLineReached)
			{
				enumerator.MoveNext();
				if (string.IsNullOrEmpty(enumerator.Current))
				{
					emptyLineReached = true;
				}
				else
				{
					engineStatsStringBuilder.AppendLine(enumerator.Current);
				}
			}
			var enginestats = engineStatsStringBuilder.ToString();

			// Parse "Starting iterations..."
			enumerator.MoveNext();
			if (!string.Equals(enumerator.Current, _textStartingIterations))
				return null;

			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;


			// Parse iterations until empty line (which is also parsed)
			var iterationsStringBuilder = new StringBuilder();
			emptyLineReached = false;
			while (!emptyLineReached)
			{
				enumerator.MoveNext();
				if (string.IsNullOrEmpty(enumerator.Current))
				{
					emptyLineReached = true;
				}
				else
				{
					iterationsStringBuilder.AppendLine(enumerator.Current);
				}
			}
			var iterations = iterationsStringBuilder.ToString();



			// Now here is a split. Either the Quantitative Value was calculated or the qualitative.
			enumerator.MoveNext();
			PrismResult result = null;
			if (_parserSatisfyingValueInInitialState.Match(enumerator.Current).Success)
			{
				//branch quantitative
				var valueInInitialStateMatch = _parserSatisfyingValueInInitialState.Match(enumerator.Current);
				var valueInInitialState = valueInInitialStateMatch.Groups["valueInInitialState"].Value;
				result = new PrismResultQuantitative()
				{
					Result = valueInInitialState
				};
			}
			else
			{
				//branch qualitative
				throw new NotImplementedException();
			}

			// Parse "Time for model checking: /timeMc/"
			enumerator.MoveNext();
			var timeMcMatch = _parserTimeMc.Match(enumerator.Current);
			if (!timeMcMatch.Success)
				return null;
			var timeMc = formulaMatch.Groups["timeMc"].Value;

			// Parse empty line
			enumerator.MoveNext();
			if (string.IsNullOrEmpty(enumerator.Current))
				return null;

			// Parse "Result: /engine/ ..."
			enumerator.MoveNext();
			var resultMatch = _parserResult.Match(enumerator.Current);
			if (!resultMatch.Success)
				return null;
			//var resultString = formulaMatch.Groups["result"].Value;
			
			
			return new PrismOutput()
			{
				Formula = formula,
				//Constants = constants,
				Prob0 = prob0,
				Prob1 = prob1,
				Yes = yes,
				No = no,
				Maybe = maybe,
				Engine = engine,
				Enginestats = enginestats,
				Iterations = iterations,
				TimeMc = timeMc,
				Result = result,
			};
		}

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
			for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
			{
				writer.Write(" & (formula" + i + "' = ");
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

			for (var i = 0; i < CompactProbabilityMatrix.NoOfLabels; i++)
			{
				streamPrism.WriteLine("global formula" + i + " : bool init false;");
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

		internal override Probability ExecuteCalculation(Formula formulaToCheck)
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();

			throw new NotImplementedException();
		}

		private string FindJava()
		{
			var javaCandidate = "C:\\ProgramData\\Oracle\\Java\\javapath\\java.exe";
			return javaCandidate;
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
