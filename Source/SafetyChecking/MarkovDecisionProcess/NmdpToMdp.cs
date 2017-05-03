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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISSE.SafetyChecking.MarkovDecisionProcess
{
	using System.Diagnostics;
	using AnalysisModel;
	using ExecutedModel;
	using GenericDataStructures;
	/*
	internal sealed class LtmdpToMdp
	{
		internal struct StateStorageEntry
		{
			internal StateStorageEntry(StateFormulaSet formula, int stateStorageState)
			{
				Formula = formula;
				StateStorageState = stateStorageState;
			}

			public readonly StateFormulaSet Formula;
			public readonly int StateStorageState;

			public bool Equals(StateStorageEntry other)
			{
				return Formula.Equals(other.Formula) && StateStorageState == other.StateStorageState;
			}
			
			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj))
					return false;
				return obj is StateStorageEntry && Equals((StateStorageEntry)obj);
			}

			public override int GetHashCode()
			{
				unchecked
				{
					return (Formula.GetHashCode() * 397) ^ StateStorageState;
				}
			}
			

		}

		public int MdpStates = 0;

		private readonly LabeledTransitionMarkovDecisionProcess _ltmdp;
		public MarkovDecisionProcess MarkovDecisionProcess { get; private set; }

		private readonly Dictionary<StateStorageEntry, int> _mapper = new Dictionary<StateStorageEntry, int>();
		private readonly AutoResizeVector<StateStorageEntry> _backMapper = new AutoResizeVector<StateStorageEntry>();

		private void CreateStates()
		{
			var enumerator = _ltmdp.GetTransitionTargetEnumerator();
			while (enumerator.MoveNext())
			{
				var entry = new StateStorageEntry(enumerator.CurrentFormulas, enumerator.CurrentTargetState);
				if (!_mapper.ContainsKey(entry))
				{
					_mapper.Add(entry, MdpStates);
					_backMapper[MdpStates] = entry;
					MdpStates++;
				}
			}
		}

		private void SetStateLabeling()
		{
			for (var i = 0; i < MdpStates; i++)
			{
				MarkovDecisionProcess.SetStateLabeling(i, _backMapper[i].Formula);
			}
		}

		private void CreateFlattenedTransitionsRecursive(long currentCid)
		{
			LabeledTransitionMarkovDecisionProcess.ContinuationGraphElement cge = _ltmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				action(cge);
			}
			else
			{
				for (var i = cge.From; i <= cge.To; i++)
				{
					ApplyActionWithRecursionBasedAlgorithmInnerRecursion(action, i);
				}
			}
		}

		private void DeriveChoice(int cidOfChoice)
		{
			var choice = CurrentGraph.GetChoiceOfCid(cidOfChoice);
			if (choice.IsChoiceTypeUnsplitOrFinal)
				return;
			if (choice.IsChoiceTypeDeterministic ||
				choice.IsChoiceTypeNondeterministic)
			{
				LtmdpContinuationDistributionMapper.NonDeterministicSplit(cidOfChoice, choice.From, choice.To);
			}
			else if (choice.IsChoiceTypeProbabilitstic)
			{
				LtmdpContinuationDistributionMapper.ProbabilisticSplit(cidOfChoice, choice.From, choice.To);
			}

			for (var i = choice.From; i <= choice.To; i++)
			{
				DeriveChoice(i);
			}
		}

		public void CreateFlattenedTransitions(int mdpState)
		{
			CreateFlattenedTransitionsRecursive(action, ParentContinuationId);
		}

		public void ConvertFlattenedTransitions(int mdpState)
		{
			var sourceEntry = _backMapper[i];
			MarkovDecisionProcess.StartWithNewDistributions(i);

			var distEnumerator = _ltmdp.GetDistributionsEnumerator(sourceEntry.StateStorageState);
			while (distEnumerator.MoveNext())
			{
				MarkovDecisionProcess.StartWithNewDistribution();
				var transEnumerator = distEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					var targetEntry = new StateStorageEntry(transEnumerator.CurrentFormulas, transEnumerator.CurrentTargetState);
					var targetState = _mapper[targetEntry];
					MarkovDecisionProcess.AddTransition(targetState, transEnumerator.CurrentProbability);
				}
				MarkovDecisionProcess.FinishDistribution();
			}
			MarkovDecisionProcess.FinishDistributions();
		}

		public void ConvertTransitions()
		{
			for (var mdpState = 0; mdpState < MdpStates; mdpState++)
			{
				CreateFlattenedTransitions(mdpState);
				ConvertFlattenedTransitions(mdpState);
			}
		}

		public void ConvertInitialStates()
		{
			/*
			MarkovDecisionProcess.StartWithInitialDistributions();
			var distEnumerator = ltmdp.GetInitialDistributionsEnumerator();
			while (distEnumerator.MoveNext())
			{
				MarkovDecisionProcess.StartWithNewInitialDistribution();
				var transEnumerator = distEnumerator.GetLabeledTransitionEnumerator();
				while (transEnumerator.MoveNext())
				{
					var targetEntry = new StateStorageEntry(transEnumerator.CurrentFormulas, transEnumerator.CurrentTargetState);
					var targetState = _mapper[targetEntry];
					MarkovDecisionProcess.AddTransitionToInitialDistribution(targetState, transEnumerator.CurrentProbability);
				}
				MarkovDecisionProcess.FinishInitialDistribution();
			}
			MarkovDecisionProcess.FinishInitialDistributions();
			*//*
		}

		public LtmdpToMdp(LabeledTransitionMarkovDecisionProcess ltmdp)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			Console.Out.WriteLine("Starting to convert labeled transition Markov Decision Process to Markov Decision Process");
			Console.Out.WriteLine($"Ltmdp: States {ltmdp.SourceStates.Count}, TransitionTargets {ltmdp.TransitionTargets}, ContinuationGraphSize {ltmdp.ContinuationGraphSize}");
			_ltmdp = ltmdp;
			CreateStates();
			var modelCapacity = new ModelCapacityByModelSize(MdpStates, (ltmdp.ContinuationGraphSize + ltmdp.TransitionTargets) * 8L, (ltmdp.ContinuationGraphSize + ltmdp.TransitionTargets) * 8L);
			MarkovDecisionProcess = new MarkovDecisionProcess(modelCapacity);
			MarkovDecisionProcess.StateFormulaLabels = ltmdp.StateFormulaLabels;
			SetStateLabeling();
			ConvertInitialStates();
			ConvertTransitions();
			stopwatch.Stop();
			_ltmdp = null;
			Console.Out.WriteLine($"Completed transformation in {stopwatch.Elapsed}");
			Console.Out.WriteLine($"Mc: States {MarkovDecisionProcess.States}, Transitions {MarkovDecisionProcess.Transitions}");
		}
	}*/
}
