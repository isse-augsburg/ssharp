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
	using System;
	using System.IO;
	using Formula;
	using Modeling;
	using Utilities;

	/// <summary>
	/// </summary>
	public class ConfigurationDependentLtmcModelChecker : LtmcModelChecker
	{
		private readonly LtmcModelChecker _ltmcModelChecker;

		private readonly DtmcModelChecker _dtmcModelChecker;

		// Note: Should be used with using(var modelchecker = new ...)
		internal ConfigurationDependentLtmcModelChecker(AnalysisConfiguration configuration, LabeledTransitionMarkovChain markovChain, TextWriter output = null)
			: base(markovChain, output)
		{
			switch (configuration.LtmcModelChecker)
			{
				case SafetyChecking.LtmcModelChecker.BuiltInLtmc:
					Requires.That(configuration.UseCompactStateStorage, "Need CompactStateStorage to use this algorithm");
					_ltmcModelChecker = new BuiltinLtmcModelChecker(markovChain, output);
					break;
				case SafetyChecking.LtmcModelChecker.BuiltInDtmc:
					var dtmc = ConvertToMarkovChain(configuration, markovChain);
					_dtmcModelChecker = new BuiltinDtmcModelChecker(dtmc, output);
					break;
				case SafetyChecking.LtmcModelChecker.ExternalMrmc:
					dtmc = ConvertToMarkovChain(configuration, markovChain);
					_dtmcModelChecker = new ExternalDtmcModelCheckerMrmc(dtmc, output);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		internal DiscreteTimeMarkovChain ConvertToMarkovChain(AnalysisConfiguration configuration, LabeledTransitionMarkovChain labeledTransitionMarkovChain)
		{
			var ltmcToMc = new LtmcToDtmc(labeledTransitionMarkovChain);
			var markovChain = ltmcToMc.MarkovChain;
			if (configuration.WriteGraphvizModels)
			{
				configuration.DefaultTraceOutput.WriteLine("Dtmc Model");
				markovChain.ExportToGv(configuration.DefaultTraceOutput);
			}
			return markovChain;
		}

		internal override bool CalculateBoolean(Formula formulaToCheck)
		{
			if (_ltmcModelChecker != null)
				return _ltmcModelChecker.CalculateBoolean(formulaToCheck);
			return _dtmcModelChecker.CalculateBoolean(formulaToCheck);
		}

		public override Probability CalculateProbability(Formula formulaToCheck)
		{
			if (_ltmcModelChecker != null)
				return _ltmcModelChecker.CalculateProbability(formulaToCheck);
			return _dtmcModelChecker.CalculateProbability(formulaToCheck);
		}

		internal override RewardResult CalculateReward(Formula formulaToCheck)
		{
			if (_ltmcModelChecker != null)
				return _ltmcModelChecker.CalculateReward(formulaToCheck);
			return _dtmcModelChecker.CalculateReward(formulaToCheck);
		}

		public override void Dispose()
		{
			_ltmcModelChecker.SafeDispose();
			_dtmcModelChecker.SafeDispose();
		}
	}
}
