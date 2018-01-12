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

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System;
	using System.IO;
	using Formula;
	using Modeling;
	using Utilities;
	
	/// <summary>
	/// </summary>
	public class ConfigurationDependentLtmdpModelChecker : LtmdpModelChecker
	{
		private readonly LtmdpModelChecker _ltmdpModelChecker;

		private readonly BuiltinNmdpModelChecker _nmdpModelChecker;

		private readonly MdpModelChecker _mdpModelChecker;
		
		// Note: Should be used with using(var modelchecker = new ...)
		public ConfigurationDependentLtmdpModelChecker(AnalysisConfiguration configuration, LabeledTransitionMarkovDecisionProcess markovChain, TextWriter output = null)
			: base(markovChain, output)
		{
			switch (configuration.LtmdpModelChecker)
			{
				case SafetyChecking.LtmdpModelChecker.BuiltInLtmdp:
					Requires.That(configuration.UseCompactStateStorage, "Need CompactStateStorage to use this algorithm");
					_ltmdpModelChecker = new BuiltinLtmdpModelChecker(Ltmdp, output);
					break;
				case SafetyChecking.LtmdpModelChecker.BuiltInNmdp:
					var nmdp = ConvertToNmdp(configuration, Ltmdp);
					_nmdpModelChecker = new BuiltinNmdpModelChecker(nmdp,output);
					break;
				case SafetyChecking.LtmdpModelChecker.BuildInMdpWithNewStates:
					nmdp = ConvertToNmdp(configuration, Ltmdp);
					var mdp = ConvertToMdpWithNewStates(configuration, nmdp);
					_mdpModelChecker = new BuiltinMdpModelChecker(mdp, output);
					break;
				case SafetyChecking.LtmdpModelChecker.BuildInMdpWithFlattening:
					nmdp = ConvertToNmdp(configuration, Ltmdp);
					mdp = ConvertToMdpWithFlattening(configuration, nmdp);
					_mdpModelChecker = new BuiltinMdpModelChecker(mdp, output);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		private NestedMarkovDecisionProcess ConvertToNmdp(AnalysisConfiguration configuration, LabeledTransitionMarkovDecisionProcess ltmdp)
		{
			var ltmdpToNmdp = new LtmdpToNmdp(ltmdp);
			var nmdp = ltmdpToNmdp.NestedMarkovDecisionProcess;
			if (configuration.WriteGraphvizModels)
			{
				configuration.DefaultTraceOutput.WriteLine("Nmdp Model");
				nmdp.ExportToGv(configuration.DefaultTraceOutput);
			}
			return nmdp;
		}

		private MarkovDecisionProcess ConvertToMdpWithNewStates(AnalysisConfiguration configuration, NestedMarkovDecisionProcess nmdp)
		{
			var nmdpToMpd = new NmdpToMdpByNewStates(nmdp);
			var mdp = nmdpToMpd.MarkovDecisionProcess;
			if (configuration.WriteGraphvizModels)
			{
				configuration.DefaultTraceOutput.WriteLine("Mdp Model");
				mdp.ExportToGv(configuration.DefaultTraceOutput);
			}
			return mdp;
		}

		private MarkovDecisionProcess ConvertToMdpWithFlattening(AnalysisConfiguration configuration, NestedMarkovDecisionProcess nmdp)
		{
			var nmdpToMpd = new NmdpToMdpByFlattening(nmdp);
			var mdp = nmdpToMpd.MarkovDecisionProcess;
			if (configuration.WriteGraphvizModels)
			{
				configuration.DefaultTraceOutput.WriteLine("Mdp Model");
				mdp.ExportToGv(configuration.DefaultTraceOutput);
			}
			return mdp;
		}

		internal override bool CalculateFormula(Formula formulaToCheck)
		{
			throw new NotImplementedException();
		}

		internal override Probability CalculateMaximalProbability(Formula formulaToCheck)
		{
			if (_ltmdpModelChecker != null)
				return _ltmdpModelChecker.CalculateMaximalProbability(formulaToCheck);
			if (_nmdpModelChecker != null)
				return _nmdpModelChecker.CalculateMaximalProbability(formulaToCheck);
			return _mdpModelChecker.CalculateMaximalProbability(formulaToCheck);
		}

		internal override Probability CalculateMinimalProbability(Formula formulaToCheck)
		{
			if (_ltmdpModelChecker != null)
				return _ltmdpModelChecker.CalculateMinimalProbability(formulaToCheck);
			if (_nmdpModelChecker != null)
				return _nmdpModelChecker.CalculateMinimalProbability(formulaToCheck);
			return _mdpModelChecker.CalculateMinimalProbability(formulaToCheck);
		}

		public override ProbabilityRange CalculateProbabilityRange(Formula formulaToCheck)
		{
			if (_ltmdpModelChecker != null)
				return _ltmdpModelChecker.CalculateProbabilityRange(formulaToCheck);
			if (_nmdpModelChecker != null)
				return _nmdpModelChecker.CalculateProbabilityRange(formulaToCheck);
			return _mdpModelChecker.CalculateProbabilityRange(formulaToCheck);
		}

		internal override RewardResult CalculateReward(Formula formulaToCheck)
		{
			if (_ltmdpModelChecker != null)
				return _ltmdpModelChecker.CalculateReward(formulaToCheck);
			if (_nmdpModelChecker != null)
				return _nmdpModelChecker.CalculateReward(formulaToCheck);
			return _mdpModelChecker.CalculateReward(formulaToCheck);
		}

		public override void Dispose()
		{
			_ltmdpModelChecker.SafeDispose();
			_nmdpModelChecker.SafeDispose();
			_mdpModelChecker.SafeDispose();
		}
	}
}
