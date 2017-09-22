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
using System.Linq;

namespace ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized
{
	using System.Diagnostics;
	using AnalysisModel;
	using ExecutedModel;
	using Formula;
	using GenericDataStructures;
	using Utilities;

	internal sealed class NmdpToMdpByNewStates : NmdpToMdp
	{
		// Problem with "For phi until psi" formula. Assume we have a state which has two successor states.
		// One in which phi is true and one in which phi is not true. Shall we set phi to true in the intermediate
		// steps or not? One solution might be to transform the formula into "(phi or intermediate state) until psi".
		// Care has to be taken if generated MDP is used for something else than finally and until.

		// State padding (currently not implemented, because it can better be solved with rewards):
		//     Flag: ConstantDistanceBetweenStates
		//     Delaying should occur earliest possible (before splits)
		//     Newly introduced states are called "PaddingStates"

		public Formula FormulaForArtificalState;

		private StateFormulaSet _stateFormulaSetforArtificialState;

		private readonly int _nmdpStates;

		private int _artificialStates;

		private int _maximalDistanceBetweenStates;

		private readonly AutoResizeBigVector<int> _cidToArtificialStateMapping = new AutoResizeBigVector<int>();

		private readonly AutoResizeBigVector<int> _cidDistanceFromRoot = new AutoResizeBigVector<int>();

		private readonly AutoResizeBigVector<int> _cidMaxDistanceFromLeaf = new AutoResizeBigVector<int>();

		public NmdpToMdpByNewStates(NestedMarkovDecisionProcess nmdp)
			: base(nmdp)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			Console.Out.WriteLine("Starting to convert Nested Markov Decision Process to Markov Decision Process");
			Console.Out.WriteLine($"Nmdp: States {nmdp.States}, ContinuationGraphSize {nmdp.ContinuationGraphSize}");

			var newNumberOfStates = nmdp.ContinuationGraphSize - nmdp.States;

			var modelCapacity = new ModelCapacityByModelSize(newNumberOfStates, nmdp.ContinuationGraphSize);
			MarkovDecisionProcess = new MarkovDecisionProcess(modelCapacity);

			_nmdpStates = _nmdp.States;

			CalculateMaxDistanceBetweenStates();
			CreateArtificalStateFormula();
			ConvertInitialTransitions();
			ConvertStateTransitions();
			SetStateLabelings();

			stopwatch.Stop();
			_nmdp = null;
			Console.Out.WriteLine($"Completed transformation in {stopwatch.Elapsed}");
			Console.Out.WriteLine($"Mdp: States {MarkovDecisionProcess.States}, Transitions {MarkovDecisionProcess.Transitions}");
		}

		public void CreateArtificalStateFormula()
		{
			var indexOfArtificialStateFormula = _nmdp.StateFormulaLabels.Length;
			Requires.That(indexOfArtificialStateFormula<32,"Too many formulas. Cannot create a state formula for artificial states during the mdp transformation");
			var satisfiedOnlyInNewIndexArray = new bool[indexOfArtificialStateFormula + 1];
			satisfiedOnlyInNewIndexArray[indexOfArtificialStateFormula] = true; //other values already initialized to false
			_stateFormulaSetforArtificialState = new StateFormulaSet(satisfiedOnlyInNewIndexArray);
			FormulaForArtificalState = new AtomarPropositionFormula();
			MarkovDecisionProcess.StateFormulaLabels =
				_nmdp.StateFormulaLabels.Concat(new[] { FormulaForArtificalState.Label }).ToArray();
		}

		private void CalculateMaxDistanceBetweenStates()
		{
			_maximalDistanceBetweenStates = 0;
			var cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfInitialState();
			_cidDistanceFromRoot.Clear(cidOfStateRoot);
			_cidMaxDistanceFromLeaf.Clear(cidOfStateRoot);

			_maximalDistanceBetweenStates = CalculateDistanceFromRootAndLeafOfCid(cidOfStateRoot, 0);
			
			for (var state = 0; state < _nmdp.States; state++)
			{
				cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfState(state);
				_cidDistanceFromRoot.Clear(cidOfStateRoot);
				_cidMaxDistanceFromLeaf.Clear(cidOfStateRoot);

				var maxDistranceFromLeaf = CalculateDistanceFromRootAndLeafOfCid(cidOfStateRoot, 0);
				_maximalDistanceBetweenStates = Math.Max(_maximalDistanceBetweenStates, maxDistranceFromLeaf);
			}

			if (_maximalDistanceBetweenStates > 0)
			{
				Console.Out.WriteLine($"Calculated a maximal distance between states of  {_maximalDistanceBetweenStates}");
				Console.Out.WriteLine($"This may skew the results");
				MarkovDecisionProcess.FactorForBoundedAnalysis = 1 + _maximalDistanceBetweenStates;
			}
		}

		private int CalculateDistanceFromRootAndLeafOfCid(long currentCid,int currentDistanceFromRoot)
		{
			//returns maxDistanceFromLeaf
			_cidDistanceFromRoot[currentCid] = currentDistanceFromRoot;
			var maxDistanceFromLeaf = 0;

			var cge = _nmdp.GetContinuationGraphElement(currentCid);
			if (!cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgi = _nmdp.GetContinuationGraphInnerNode(currentCid);

				if (cge.IsChoiceTypeForward)
				{
					maxDistanceFromLeaf = CalculateDistanceFromRootAndLeafOfCid(cgi.ToCid, currentDistanceFromRoot);
				}
				else
				{
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						var maxDistanceFromLeafOfi = CalculateDistanceFromRootAndLeafOfCid(i, currentDistanceFromRoot + 1);
						maxDistanceFromLeaf = Math.Max(maxDistanceFromLeaf, maxDistanceFromLeafOfi+1);
					}
				}
			}
			_cidMaxDistanceFromLeaf[currentCid] = maxDistanceFromLeaf;
			return maxDistanceFromLeaf;
		}

		public void ConvertInitialTransitions()
		{
			var cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfInitialState();
			_cidToArtificialStateMapping.Clear(cidOfStateRoot);
			
			ConvertRootCid(null,cidOfStateRoot);
		}

		public void ConvertStateTransitions()
		{
			for (var state = 0; state < _nmdp.States; state++)
			{
				var cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfState(state);
				_cidToArtificialStateMapping.Clear(cidOfStateRoot);
				
				ConvertRootCid(state,cidOfStateRoot);
			}
		}

		private void AddDestination(long cidToAdd)
		{
			var cge = _nmdp.GetContinuationGraphElement(cidToAdd);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = _nmdp.GetContinuationGraphLeaf(cidToAdd);
				MarkovDecisionProcess.AddTransition(cgl.ToState,cgl.Probability);
			}
			else if (cge.IsChoiceTypeForward)
			{
				// This ChoiceType might be created by ForwardUntakenChoicesAtIndex in ChoiceResolver
				// We assume that the node to forward to has already been encountered
				var cgi = _nmdp.GetContinuationGraphInnerNode(cidToAdd);
				var nodeToForwardTo = _nmdp.GetContinuationGraphElement(cgi.ToCid);
				int mdpStateToForwardTo;				
				if (nodeToForwardTo.IsChoiceTypeUnsplitOrFinal)
					mdpStateToForwardTo = nodeToForwardTo.AsLeaf.ToState;
				else
					mdpStateToForwardTo = _cidToArtificialStateMapping[cgi.ToCid];
				MarkovDecisionProcess.AddTransition(mdpStateToForwardTo, cge.Probability);
			}
			else
			{
				var newArtificialMarkovState = CreateNewArtificialState(cidToAdd);
				MarkovDecisionProcess.AddTransition(newArtificialMarkovState, cge.Probability);
			}
		}

		private void ConvertRootCid(int? sourceState, long currentCid)
		{
			var cge = _nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				// if a state leads directly into a new state, add this state directly
				if (sourceState.HasValue)
				{
					var mdpState = sourceState.Value;
					MarkovDecisionProcess.StartWithNewDistributions(mdpState);
					MarkovDecisionProcess.StartWithNewDistribution();
					AddDestination(currentCid);
					MarkovDecisionProcess.FinishDistribution();
					MarkovDecisionProcess.FinishDistributions();
				}
				else
				{
					MarkovDecisionProcess.StartWithInitialDistributions();
					MarkovDecisionProcess.StartWithNewDistribution();
					AddDestination(currentCid);
					MarkovDecisionProcess.FinishDistribution();
					MarkovDecisionProcess.FinishDistributions();
				}
			}
			else
			{
				var cgi = _nmdp.GetContinuationGraphInnerNode(currentCid);

				if (sourceState.HasValue)
				{
					var mdpState = sourceState.Value;
					MarkovDecisionProcess.StartWithNewDistributions(mdpState);
				}
				else
				{
					MarkovDecisionProcess.StartWithInitialDistributions();
				}

				if (cge.IsChoiceTypeForward)
				{
					// This ChoiceType might be created by ForwardUntakenChoicesAtIndex in ChoiceResolver
					throw new Exception("Bug: RootCid cannot be a forward node");
				}
				else if (cge.IsChoiceTypeNondeterministic)
				{
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						MarkovDecisionProcess.StartWithNewDistribution();
						AddDestination(i);
						MarkovDecisionProcess.FinishDistribution();
					}
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					MarkovDecisionProcess.StartWithNewDistribution();
					for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
					{
						AddDestination(i);
					}
					MarkovDecisionProcess.FinishDistribution();
				}
				
				MarkovDecisionProcess.FinishDistributions();
				
				for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
				{
					ConvertChildCid(i);
				}
			}
		}

		private void ConvertChildCid(long currentCid)
		{
			var cge = _nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal || cge.IsChoiceTypeForward)
				return;
			var mdpState = _cidToArtificialStateMapping[currentCid];
			MarkovDecisionProcess.StartWithNewDistributions(mdpState);

			var cgi = _nmdp.GetContinuationGraphInnerNode(currentCid);
			if (cge.IsChoiceTypeNondeterministic)
			{
				for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
				{
					MarkovDecisionProcess.StartWithNewDistribution();
					AddDestination(i);
					MarkovDecisionProcess.FinishDistribution();
				}
			}
			else if (cge.IsChoiceTypeProbabilitstic)
			{
				MarkovDecisionProcess.StartWithNewDistribution();
				for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
				{
					AddDestination(i);
				}
				MarkovDecisionProcess.FinishDistribution();
			}

			MarkovDecisionProcess.FinishDistributions();

			for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
			{
				ConvertChildCid(i);
			}
		}

		private int CreateNewArtificialState(long cid)
		{
			var freshIndexInMdp = _artificialStates+_nmdpStates;
			_artificialStates++;
			_cidToArtificialStateMapping[cid] = freshIndexInMdp;
			return freshIndexInMdp;
		}

		private void SetStateLabelings()
		{
			for (var i = 0; i < _nmdpStates; i++)
			{
				// Copy old labels
				var mdpState = i;
				MarkovDecisionProcess.StateLabeling[mdpState] = _nmdp.StateLabeling[i];
			}
			for (var i = 0; i < _artificialStates; i++)
			{
				// Set Artificial state label
				var mdpState = _nmdpStates + i;
				MarkovDecisionProcess.StateLabeling[mdpState] = _stateFormulaSetforArtificialState;
			}
		}
	}
}
