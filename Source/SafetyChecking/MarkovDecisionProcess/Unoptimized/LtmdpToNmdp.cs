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

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using AnalysisModel;
	using ExecutedModel;
	using GenericDataStructures;
	using Utilities;

	internal sealed class LtmdpToNmdp
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
		public NestedMarkovDecisionProcess NestedMarkovDecisionProcess { get; private set; }

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
				NestedMarkovDecisionProcess.SetStateLabeling(i, _backMapper[i].Formula);
			}
		}

		private void AddNodesOfContinuationId(long continuationId, long locationForContinuationGraphElement)
		{
			var choice = _ltmdp.GetContinuationGraphElement(continuationId);

			if (choice.IsChoiceTypeUnsplitOrFinal)
			{
				Assert.That(choice.To<=int.MaxValue, "choice.To must be smaller than int.MaxValue");

				var transitionTarget = _ltmdp.GetTransitionTarget( (int) choice.To);
				var targetEntry = new StateStorageEntry(transitionTarget.Formulas, transitionTarget.TargetState);
				var targetState = _mapper[targetEntry];

				NestedMarkovDecisionProcess.AddContinuationGraphLeaf(locationForContinuationGraphElement, targetState, choice.Probability);
				return;
			}

			var offsetTo = choice.To - choice.From;
			var numberOfChildren = offsetTo + 1;

			var placesForChildren = NestedMarkovDecisionProcess.GetPlaceForNewContinuationGraphElements(numberOfChildren);

			NestedMarkovDecisionProcess.AddContinuationGraphInnerNode(locationForContinuationGraphElement, choice.ChoiceType, placesForChildren,placesForChildren+offsetTo, choice.Probability);

			for (var currentChildNo = 0; currentChildNo < numberOfChildren; currentChildNo++)
			{
				var originalContinuationId = choice.From + currentChildNo;
				var newLocation = placesForChildren + currentChildNo;
				AddNodesOfContinuationId(originalContinuationId, newLocation);
			}
		}

		public void ConvertTransitions()
		{
			for (var i = 0; i < MdpStates; i++)
			{
				var sourceEntry = _backMapper[i];
				var cid = _ltmdp.GetRootContinuationGraphLocationOfState(sourceEntry.StateStorageState);
				var locationOfStateRoot = NestedMarkovDecisionProcess.GetPlaceForNewContinuationGraphElements(1);
				NestedMarkovDecisionProcess.SetRootContinuationGraphLocationOfState(i, locationOfStateRoot);
				AddNodesOfContinuationId(cid, locationOfStateRoot);
			}
		}

		public void ConvertInitialStates()
		{
			var cid = _ltmdp.GetRootContinuationGraphLocationOfInitialState();
			var locationOfStateRoot = NestedMarkovDecisionProcess.GetPlaceForNewContinuationGraphElements(1);
			NestedMarkovDecisionProcess.SetRootContinuationGraphLocationOfInitialState(locationOfStateRoot);
			AddNodesOfContinuationId(cid, locationOfStateRoot);
		}

		public LtmdpToNmdp(LabeledTransitionMarkovDecisionProcess ltmdp)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			Console.Out.WriteLine("Starting to convert labeled transition Markov Decision Process to Nested Markov Decision Process");
			Console.Out.WriteLine($"Ltmdp: States {ltmdp.SourceStates.Count}, TransitionTargets {ltmdp.TransitionTargets}, ContinuationGraphSize {ltmdp.ContinuationGraphSize}");
			_ltmdp = ltmdp;
			CreateStates();
			var modelCapacity = new ModelCapacityByModelSize(MdpStates, ltmdp.ContinuationGraphSize * 8L);
			NestedMarkovDecisionProcess = new NestedMarkovDecisionProcess(modelCapacity);
			NestedMarkovDecisionProcess.StateFormulaLabels = ltmdp.StateFormulaLabels;
			SetStateLabeling();
			ConvertInitialStates();
			ConvertTransitions();
			stopwatch.Stop();
			_ltmdp = null;
			Console.Out.WriteLine($"Completed transformation in {stopwatch.Elapsed}");
			Console.Out.WriteLine($"Nmdp: States {NestedMarkovDecisionProcess.States}, ContinuationGraphSize {NestedMarkovDecisionProcess.ContinuationGraphSize}");
		}
	}
}
