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
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;

	public class ExternalDtmcModelCheckerMrmc : DtmcModelChecker
	{

		private TemporaryFile _fileTransitions;
		private TemporaryFile _fileStateLabelings;
		
		public ExternalDtmcModelCheckerMrmc(DiscreteTimeMarkovChain markovChain, TextWriter output = null) : base(markovChain,output)
		{
		}
		
		private Dictionary<int, double>  _initialStates;

		private void WriteMarkovChainToDisk()
		{
			_fileTransitions = new TemporaryFile("tra");
			_fileStateLabelings = new TemporaryFile("lab");

			var streamTransitions = new StreamWriter(_fileTransitions.FilePath);
			var streamStateLabelings = new StreamWriter(_fileStateLabelings.FilePath);

			MarkovChain.ExportToMrmc(streamTransitions,streamStateLabelings);
		}

		/*
		private TemporaryFile WriteRewardsToFile(Formula formulaToCheck,out bool useRewards)
		{
			var fileStateRewards = new TemporaryFile("rew");
			var streamRewards = new StreamWriter(fileStateRewards.FilePath);
		}
		*/

		private TemporaryFile WriteRewardsToFile(Formula formulaToCheck, out bool useRewards)
		{
			useRewards = false;
			return null;
		}

		private readonly System.Text.RegularExpressions.Regex MrmcProbabilityParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<probability>([0-1]\\.?[0-9]+)|(inf))$");
		private readonly System.Text.RegularExpressions.Regex MrmcFormulaParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<satisfied>(TRUE)|(FALSE))$");
		private readonly System.Text.RegularExpressions.Regex MrmcRewardParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<reward>([0-9]+\\.?[0-9]+)|(inf))$");

		private TemporaryFile WriteFilesAndExecuteMrmc(Formula formulaToCheck,bool outputExactResult) //returns result file
		{
			WriteMarkovChainToDisk();
			
			var transformationVisitor = new MrmcTransformer();
			transformationVisitor.Visit(formulaToCheck);
			var formulaToCheckString = transformationVisitor.TransformedFormula;
			
			bool useRewards;
			_initialStates = new Dictionary<int, double>();
			var enumerator = MarkovChain.GetEnumerator();
			enumerator.SelectInitialDistribution();
			while (enumerator.MoveNextTransition())
			{
				//enumerate initial states
				var entry = enumerator.CurrentTransition;
				var index = entry.Column + 1; //index in mrmc is 1-based
				if (_initialStates.ContainsKey(index))
					_initialStates.Add(index, _initialStates[index]+ entry.Value);
				else
					_initialStates.Add(index, entry.Value);
			}

			var fileResults = new TemporaryFile("res");
			using (var fileCommandScript = new TemporaryFile("cmd"))
			using (var fileStateRewards = WriteRewardsToFile(formulaToCheck, out useRewards))
			{
				var script = new StringBuilder();
				script.AppendLine("set method_path gauss_jacobi"); //warning: gauss_seidel seems to be buggy in MRMC
				script.AppendLine("set error_bound 1.0E-6");
				script.AppendLine(formulaToCheckString);

				//write result of every initial state to file
				if (outputExactResult)
					script.Append("write_res_file_result");
				else
					script.Append("write_res_file_state");
				foreach (var initialState in _initialStates)
				{
					script.Append(" " + (initialState.Key));
				}

				script.AppendLine();
				script.AppendLine("quit");

				File.WriteAllText(fileCommandScript.FilePath, script.ToString());

				var commandLineMode = useRewards ? "dmrm" : "dtmc";
				var commandLineArgumentTransitionFile = $" {_fileTransitions.FilePath}";
				var commandLineArgumentStateLabelingFile = $" {_fileStateLabelings.FilePath}";
				var commandLineArgumentRewardFile = fileStateRewards != null ? $" {fileStateRewards.FilePath}" : "";
				var commandLineArgumentCommandScriptFile = $" {fileCommandScript.FilePath}";
				var commandLineArgumentResultsFile = $" {fileResults.FilePath}";

				var commandLineArguments =
					commandLineMode
					+ commandLineArgumentTransitionFile
					+ commandLineArgumentStateLabelingFile
					+ commandLineArgumentRewardFile
					+ commandLineArgumentCommandScriptFile
					+ commandLineArgumentResultsFile;

				var pureModelCheckingTime = "";
				var iterations = "";
				Action<string> extractInterestingOutput =
					output =>
					{
						if (output.StartsWith("The Total Elapsed Model-Checking Time"))
							pureModelCheckingTime = output;

						else if (output.StartsWith("Gauss Jacobi V_B: The number of Gauss-Jacobi iterations"))
							iterations = output;
					};
				

				var mrmc = new ExternalProcess("mrmc.exe", commandLineArguments, extractInterestingOutput);

				var stopwatch = new Stopwatch();
				stopwatch.Start();
				mrmc.Run();
				stopwatch.Stop();

				_output?.WriteLine(iterations);
				_output?.WriteLine(pureModelCheckingTime);
				_output?.WriteLine($"MRMC total model checking time: {stopwatch.Elapsed}");
			}
			return fileResults;
		}

		internal override Probability CalculateProbability(Formula formulaToCheck)
		{
			var workaroundFormula = new ProbabilitisticFormula(formulaToCheck, ProbabilisticComparator.BiggerThan, 0.0);

			using (var fileResults = WriteFilesAndExecuteMrmc(workaroundFormula, true))
			{
				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();
				
				var probability = 0.0;
				while (resultEnumerator.MoveNext())
				{
					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcProbabilityParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = _initialStates[state]; //index in mrmc is 1-based
							double probabilityInState;
							// Mrmc may return a probability in a state of PositiveInfinity. This is clearly undesired and might be because of a wrong probability matrix
							if (parsed.Groups["probability"].Value == "inf")
								probabilityInState = double.PositiveInfinity;
							else
								probabilityInState = Double.Parse(parsed.Groups["probability"].Value, CultureInfo.InvariantCulture);
							probability += probabilityOfState * probabilityInState;
						}
						else
						{
							throw new Exception("Expected different output of MRMC");
						}
					}
				}
				return new Probability(probability);
			}
		}

		internal override bool CalculateBoolean(Formula formulaToCheck)
		{
			var isFormulaReturningBoolValueVisitor = new IsFormulaReturningBoolValueVisitor();
			isFormulaReturningBoolValueVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningBoolValueVisitor.IsFormulaReturningBoolValue)
			{
				throw new Exception("expected formula which returns true or false");
			}

			using (var fileResults = WriteFilesAndExecuteMrmc(formulaToCheck, false))
			{
				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();
				
				var satisfied = true;
				while (resultEnumerator.MoveNext())
				{
					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcFormulaParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = _initialStates[state];
							if (probabilityOfState>0.0)
							{
								var isSatisfiedResult = parsed.Groups["satisfied"].Value.Equals("TRUE");
								satisfied &= isSatisfiedResult; // TODO: Check PCTL semantics: should formula be true for all initial states >0.0? Or should the initial probability be taken into account for the probability value of the outermost operator.
							}
						}
						else
						{
							throw new Exception("Expected different output of MRMC");
						}
					}
				}
				return satisfied;
			}
		}

		internal override RewardResult CalculateReward(Formula formulaToCheck)
		{
			var isFormulaReturningRewardResultVisitor = new IsFormulaReturningRewardResultVisitor();
			isFormulaReturningRewardResultVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningRewardResultVisitor.IsReturningRewardResult)
			{
				throw new Exception("expected formula which returns reward");
			}

			using (var fileResults = WriteFilesAndExecuteMrmc(formulaToCheck, true))
			{
				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();
				
				var rewardResultValue = 0.0;
				while (resultEnumerator.MoveNext())
				{
					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcRewardParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = _initialStates[state];
							double rewardOfState;
							if (parsed.Groups["reward"].Value == "inf")
								rewardOfState = double.PositiveInfinity;
							else
								rewardOfState = Double.Parse(parsed.Groups["reward"].Value, CultureInfo.InvariantCulture);
							rewardResultValue += probabilityOfState * rewardOfState; //TODO: Calculate reward _in_ initial state?
						}
						else
						{
							throw new Exception("Expected different output of MRMC");
						}
					}
				}
				var rewardResult = new RewardResult();
				rewardResult.Value = rewardResultValue;
				return rewardResult;
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
