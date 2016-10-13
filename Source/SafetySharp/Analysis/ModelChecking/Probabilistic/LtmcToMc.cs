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

namespace SafetySharp.Runtime
{
	using Analysis;

	internal sealed class LtmcToMc
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

		public int States = 0;


		public MarkovChain MarkovChain { get; private set; }

		private readonly Dictionary<StateStorageEntry, int> _mapper = new Dictionary<StateStorageEntry, int>();
		private readonly AutoResizeVector<StateStorageEntry> _backMapper = new AutoResizeVector<StateStorageEntry>();

		private void CreateStates(LabeledTransitionMarkovChain ltmc)
		{
			var enumerator = ltmc.GetTransitionChainEnumerator();
			while (enumerator.MoveNext())
			{
				var entry = new StateStorageEntry(enumerator.CurrentFormulas, enumerator.CurrentTargetState);
				if (!_mapper.ContainsKey(entry))
				{
					_mapper.Add(entry, States);
					_backMapper[States] = entry;
					States++;
				}
			}
		}

		private void SetStateLabeling()
		{
			for (var i = 0; i < States; i++)
			{
				MarkovChain.SetStateLabeling(i, _backMapper[i].Formula);
			}
		}

		public void ConvertTransitions(LabeledTransitionMarkovChain ltmc)
		{
			for (var i = 0; i < States; i++)
			{
				var sourceEntry = _backMapper[i];
				MarkovChain.SetMarkovChainSourceStateOfUpcomingTransitions(i);

				var enumerator = ltmc.GetTransitionEnumerator(sourceEntry.StateStorageState);
				while (enumerator.MoveNext())
				{
					var targetEntry = new StateStorageEntry(enumerator.CurrentFormulas, enumerator.CurrentTargetState);
					var targetState = _mapper[targetEntry];
					MarkovChain.AddTransition(targetState,enumerator.CurrentProbability);
				}
				MarkovChain.FinishSourceState();
			}
		}

		public void ConvertInitialStates(LabeledTransitionMarkovChain ltmc)
		{
			MarkovChain.StartWithInitialStates();
			var enumerator = ltmc.GetInitialDistributionEnumerator();
			while (enumerator.MoveNext())
			{
				var targetEntry = new StateStorageEntry(enumerator.CurrentFormulas, enumerator.CurrentTargetState);
				var targetState = _mapper[targetEntry];
				MarkovChain.AddInitialState(targetState, enumerator.CurrentProbability);
			}
			MarkovChain.FinishInitialStates();
		}

		public LtmcToMc(LabeledTransitionMarkovChain ltmc)
		{
			Console.Error.WriteLine("Starting to convert labeled transition Markov chain to Markov chain");
			Console.Error.WriteLine($"Ltmc: States {ltmc.SourceStates.Count}, Transitions {ltmc.Transitions}");
			CreateStates(ltmc);
			MarkovChain=new MarkovChain(States);
			MarkovChain.StateFormulaLabels = ltmc.StateFormulaLabels;
			SetStateLabeling();
			ConvertInitialStates(ltmc);
			ConvertTransitions(ltmc);
			Console.Error.WriteLine($"Mc: States {MarkovChain.States}, Transitions {MarkovChain.Transitions}");
		}
	}
}
