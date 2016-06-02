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
			var noStateFormulaLabels = CompactProbabilityMatrix.StateFormulaLabels.Length;
			for (var i = 0; i < noStateFormulaLabels; i++)
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
				for (var i = 0; i < noStateFormulaLabels; i++)
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

		private TemporaryFile WriteRewardsToFile(Formula formulaToCheck,out bool useRewards)
		{
			var fileStateRewards = new TemporaryFile("rew");
			var streamRewards = new StreamWriter(fileStateRewards.FilePath);
			streamRewards.NewLine = "\n";

			var rewardRetrieverCollector = new RewardRetrieverCollector();
			formulaToCheck.Visit(rewardRetrieverCollector);

			if (rewardRetrieverCollector.RewardRetrievers.Count == 0)
			{
				useRewards = false;
				return null;
			}
			if (rewardRetrieverCollector.RewardRetrievers.Count > 1)
				throw new Exception("Mrmc can currently only handle one reward in each formula");

			var labelOfReward = rewardRetrieverCollector.RewardRetrievers.Single();
			var indexOfLabel = Array.FindIndex(CompactProbabilityMatrix.StateRewardRetrieverLabels,label => label.Equals(labelOfReward));
			

			foreach (var stateReward in CompactProbabilityMatrix.StateRewards)
			{
				streamRewards.WriteLine($" {stateReward.Key} {stateReward.Value[indexOfLabel].Value()}");
			}
			streamRewards.Flush();
			streamRewards.Close();
			useRewards = true;
			return fileStateRewards;
		}

		private readonly System.Text.RegularExpressions.Regex MrmcProbabilityParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<probability>([0-1]\\.?[0-9]+)|(inf))$");
		private readonly System.Text.RegularExpressions.Regex MrmcFormulaParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<satisfied>(TRUE)|(FALSE))$");
		private readonly System.Text.RegularExpressions.Regex MrmcRewardParser = new System.Text.RegularExpressions.Regex("^(?<state>\\d\\d*)\\s(?<reward>([0-9]+\\.?[0-9]+)|(inf))$");

		private TemporaryFile WriteFilesAndExecuteMrmc(Formula formulaToCheck,bool outputExactResult) //returns result file
		{
			ProbabilityChecker.AssertProbabilityMatrixWasCreated();
			WriteProbabilityMatrixToDisk();
			
			var transformationVisitor = new MrmcTransformer();
			transformationVisitor.Visit(formulaToCheck);
			var formulaToCheckString = transformationVisitor.TransformedFormula;

			var initialStatesWithProbabilities = ProbabilityChecker.CompactProbabilityMatrix.InitialStates;
			var initialStates = initialStatesWithProbabilities.Select(tuple => tuple.State);
			bool useRewards;

			var fileResults = new TemporaryFile("res");
			using (var fileCommandScript = new TemporaryFile("cmd"))
			using (var fileStateRewards = WriteRewardsToFile(formulaToCheck, out useRewards))
			{
				var script = new StringBuilder();
				script.AppendLine("set method_path gauss_jacobi");
				script.AppendLine(formulaToCheckString);
				if (outputExactResult)
					script.Append("write_res_file_result");
				else
					script.Append("write_res_file_state");
				foreach (var initialState in initialStates)
				{
					script.Append(" " + initialState);
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

				var mrmc = new ExternalProcess("mrmc.exe", commandLineArguments);
				mrmc.Run();
			}
			return fileResults;
		}

		internal override Probability CalculateProbability(Formula formulaToCheck)
		{
			var isFormulaReturningProbabilityVisitor = new IsFormulaReturningProbabilityVisitor();
			isFormulaReturningProbabilityVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningProbabilityVisitor.IsReturningProbability)
			{
				throw new Exception("expected formula which returns a probability");
			}
			using (var fileResults = WriteFilesAndExecuteMrmc(formulaToCheck,true))
			{
				var initialStatesWithProbabilities = ProbabilityChecker.CompactProbabilityMatrix.InitialStates;
				var initialStates = initialStatesWithProbabilities.Select(tuple => tuple.State);
				var probabilities = initialStatesWithProbabilities.Select(tuple => tuple.Probability);

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
						var parsed = MrmcProbabilityParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = probabilityEnumerator.Current;
							double probabilityInState;
							// Mrmc may return a probability in a state of PositiveInfinity. This is clearly undesired and is most likely a result of double imprecision.
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
				return probability;
			}
		}

		internal override bool CalculateFormula(Formula formulaToCheck)
		{
			var isFormulaReturningBoolValueVisitor = new IsFormulaReturningBoolValueVisitor();
			isFormulaReturningBoolValueVisitor.Visit(formulaToCheck);
			if (!isFormulaReturningBoolValueVisitor.IsFormulaReturningBoolValue)
			{
				throw new Exception("expected formula which returns true or false");
			}

			using (var fileResults = WriteFilesAndExecuteMrmc(formulaToCheck, false))
			{
				var initialStatesWithProbabilities = ProbabilityChecker.CompactProbabilityMatrix.InitialStates;
				var initialStates = initialStatesWithProbabilities.Select(tuple => tuple.State);
				var probabilities = initialStatesWithProbabilities.Select(tuple => tuple.Probability);

				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();

				var index = 0;
				var satisfied = true;
				var probabilityEnumerator = probabilities.GetEnumerator();
				while (resultEnumerator.MoveNext())
				{
					probabilityEnumerator.MoveNext();

					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcFormulaParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = probabilityEnumerator.Current;
							if (probabilityOfState.Greater(0.0))
							{
								var isSatisfiedResult = parsed.Groups["satisfied"].Value.Equals("TRUE");
								satisfied &= isSatisfiedResult;
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
				var initialStatesWithProbabilities = ProbabilityChecker.CompactProbabilityMatrix.InitialStates;
				var initialStates = initialStatesWithProbabilities.Select(tuple => tuple.State);
				var probabilities = initialStatesWithProbabilities.Select(tuple => tuple.Probability);

				var resultEnumerator = File.ReadLines(fileResults.FilePath).GetEnumerator();

				var index = 0;
				var rewardResultValue = 0.0;
				var probabilityEnumerator = probabilities.GetEnumerator();
				while (resultEnumerator.MoveNext())
				{
					probabilityEnumerator.MoveNext();

					var result = resultEnumerator.Current;
					if (!String.IsNullOrEmpty(result))
					{
						var parsed = MrmcRewardParser.Match(result);
						if (parsed.Success)
						{
							var state = Int32.Parse(parsed.Groups["state"].Value);
							var probabilityOfState = probabilityEnumerator.Current.Value;
							double rewardOfState;
							if (parsed.Groups["reward"].Value == "inf")
								rewardOfState = double.PositiveInfinity;
							else
								rewardOfState = Double.Parse(parsed.Groups["reward"].Value, CultureInfo.InvariantCulture);
							rewardResultValue += probabilityOfState * rewardOfState;
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
