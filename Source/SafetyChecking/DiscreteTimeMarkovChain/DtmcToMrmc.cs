// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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


namespace ISSE.SafetyChecking.DiscreteTimeMarkovChain
{
	using System.Globalization;
	using System.IO;

	public static class DtmcToMrmcExtension
	{
		internal static void ExportToMrmc(this DiscreteTimeMarkovChain dtmc, TextWriter streamTransitions, TextWriter streamStateLabelings)
		{
			var dtmcToMrmc= new DtmcToMrmc(dtmc);
			dtmcToMrmc.WriteMarkovChainToStream(streamTransitions,streamStateLabelings);
		}
	}

	public class DtmcToMrmc
	{

		private DiscreteTimeMarkovChain _markovChain;

		public DtmcToMrmc(DiscreteTimeMarkovChain markovChain)
		{
			_markovChain = markovChain;
		}

		public void WriteMarkovChainToStream(TextWriter streamTransitions, TextWriter streamStateLabelings)
		{
			streamTransitions.NewLine = "\n";
			streamStateLabelings.NewLine = "\n";

			streamTransitions.WriteLine("STATES " + _markovChain.States);
			streamTransitions.WriteLine("TRANSITIONS " + _markovChain.Transitions);

			var enumerator = _markovChain.GetEnumerator();
			while(enumerator.MoveNextState())
			{
				while (enumerator.MoveNextTransition())
				{
					var currentColumnValue = enumerator.CurrentTransition;
					streamTransitions.WriteLine((enumerator.CurrentState + 1) + " " + (currentColumnValue.Column+1) + " " + currentColumnValue.Value.ToString(CultureInfo.InvariantCulture)); //index in mrmc is 1-based
				}
			}
			streamTransitions.Flush();
			streamTransitions.Close();

			streamStateLabelings.WriteLine("#DECLARATION");

			var noStateFormulaLabels = _markovChain.StateFormulaLabels.Length;
			for (var i = 0; i < noStateFormulaLabels; i++)
			{
				var label = _markovChain.StateFormulaLabels[i];
				if (i > 0)
				{
					streamStateLabelings.Write(" ");
				}
				streamStateLabelings.Write(label);
			}
			streamStateLabelings.WriteLine();
			streamStateLabelings.WriteLine("#END");
			var stateLabeling = _markovChain.StateLabeling;
			for (var state = 0; state < _markovChain.States; state++)
			{
				streamStateLabelings.Write(state+1); //index in mrmc is 1-based
				for (var i = 0; i < noStateFormulaLabels; i++)
				{
					var label = _markovChain.StateFormulaLabels[i];
					if (stateLabeling[state][i])
						streamStateLabelings.Write(" " + label);
				}
				streamStateLabelings.WriteLine();
			}
			streamStateLabelings.Flush();
			streamStateLabelings.Close();
		}

		/*
		private void WriteRewardsToStream(TextWriter streamRewards,Formula formulaToCheck,out bool useRewards)
		{
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
			var indexOfLabel = Array.FindIndex(MarkovChain.StateRewardRetrieverLabels,label => label.Equals(labelOfReward));

			if (indexOfLabel == 0)
			{
				for (int i = 0; i < MarkovChain.States; i++)
				{
					streamRewards.WriteLine($" {i+1} {MarkovChain.StateRewards0[i].Value()}"); //index in mrmc is 1-based
				}
			}
			if (indexOfLabel == 1)
			{
				for (int i = 0; i < MarkovChain.States; i++)
				{
					streamRewards.WriteLine($" {i+1} {MarkovChain.StateRewards1[i].Value()}"); //index in mrmc is 1-based
				}
			}
			streamRewards.Flush();
			streamRewards.Close();
			useRewards = true;
			return fileStateRewards;
		}
		*/
	}
}
