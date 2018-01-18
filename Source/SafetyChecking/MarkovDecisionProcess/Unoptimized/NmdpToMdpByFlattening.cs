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
	using AnalysisModel;
	using ExecutedModel;
	using GenericDataStructures;
	using Utilities;

	public sealed class NmdpToMdpByFlattening : NmdpToMdp
	{
		private AutoResizeBigVector<NestedMarkovDecisionProcess.ContinuationGraphLeaf> _continuationGraphLeafOfCid = new AutoResizeBigVector<NestedMarkovDecisionProcess.ContinuationGraphLeaf>();
		
		private AutoResizeBigVector<double> _probabilityOfCid = new AutoResizeBigVector<double>();

		private readonly LtmdpContinuationDistributionMapper _ltmdpContinuationDistributionMapper = new LtmdpContinuationDistributionMapper();
		

		private void CopyStateLabeling()
		{
			for (var i = 0; i < _nmdp.States; i++)
			{
				MarkovDecisionProcess.SetStateLabeling(i, _nmdp.StateLabeling[i]);
			}
		}

		private NestedMarkovDecisionProcess.ContinuationGraphLeaf GetLeafOfCid(long cid)
		{
			return _continuationGraphLeafOfCid[cid];
		}

		private void SetLeafOfCid(long cid, NestedMarkovDecisionProcess.ContinuationGraphLeaf leaf)
		{
			_continuationGraphLeafOfCid[cid] = leaf;
		}

		private double GetProbabilityOfCid(long cid)
		{
			return _probabilityOfCid[cid];
		}

		private void SetProbabilityOfCid(long cid, double probability)
		{
			_probabilityOfCid[cid] = probability;
		}

		private void MultiplyProbabilityOfCid(long cid, double factor)
		{
			_probabilityOfCid[cid] *= factor;
		}

		private void UpdateContinuationDistributionMapperAndCollectLeafs(long currentCid)
		{
			var cge = _nmdp.GetContinuationGraphElement(currentCid);
			if (cge.IsChoiceTypeUnsplitOrFinal)
			{
				var cgl = _nmdp.GetContinuationGraphLeaf(currentCid);
				MultiplyProbabilityOfCid(currentCid, cgl.Probability);
				SetLeafOfCid(currentCid, cgl);
			}
			else
			{
				var cgi = _nmdp.GetContinuationGraphInnerNode(currentCid);
				MultiplyProbabilityOfCid(currentCid, cgi.Probability);
				if (cge.IsChoiceTypeForward)
				{
					// This ChoiceType might be created by ForwardUntakenChoicesAtIndex in ChoiceResolver
					throw new Exception("Forward transitions not supported");
				}
				else if (cge.IsChoiceTypeNondeterministic)
				{
					_ltmdpContinuationDistributionMapper.NonDeterministicSplit(currentCid, cgi.FromCid, cgi.ToCid);
				}
				else if (cge.IsChoiceTypeProbabilitstic)
				{
					_ltmdpContinuationDistributionMapper.ProbabilisticSplit(currentCid, cgi.FromCid, cgi.ToCid);
				}
				var oldProbability = GetProbabilityOfCid(currentCid);
				for (var i = cgi.FromCid; i <= cgi.ToCid; i++)
				{
					SetProbabilityOfCid(i, oldProbability);
					UpdateContinuationDistributionMapperAndCollectLeafs(i);
				}
			}
		}

		private void AddDistribution(int distribution)
		{
			if (_ltmdpContinuationDistributionMapper.IsDistributionEmpty(distribution))
				return;
			MarkovDecisionProcess.StartWithNewDistribution();

			var enumerator = _ltmdpContinuationDistributionMapper.GetContinuationsOfDistributionEnumerator(distribution);
			while (enumerator.MoveNext())
			{
				var leaf = GetLeafOfCid(enumerator.CurrentContinuationId);
				var probability = GetProbabilityOfCid(enumerator.CurrentContinuationId);
				MarkovDecisionProcess.AddTransition(leaf.ToState, probability);
			}
			
			MarkovDecisionProcess.FinishDistribution();
		}


		private void AddInitialDistribution(int distribution)
		{
			if (_ltmdpContinuationDistributionMapper.IsDistributionEmpty(distribution))
				return;

			MarkovDecisionProcess.StartWithNewDistribution();

			var enumerator = _ltmdpContinuationDistributionMapper.GetContinuationsOfDistributionEnumerator(distribution);
			while (enumerator.MoveNext())
			{
				var leaf = GetLeafOfCid(enumerator.CurrentContinuationId);
				var probability = GetProbabilityOfCid(enumerator.CurrentContinuationId);
				MarkovDecisionProcess.AddTransition(leaf.ToState, probability);
			}
			
			MarkovDecisionProcess.FinishDistribution();
		}

		private void Clear(long cidRoot)
		{
			_probabilityOfCid.Clear(cidRoot); // use cidRoot as offset, because it is the smallest element
			_continuationGraphLeafOfCid.Clear(cidRoot);
			_probabilityOfCid[cidRoot] = 1.0;
			_ltmdpContinuationDistributionMapper.Clear();
		}

		private void ConvertStateTransitions()
		{
			for (var state = 0; state < _nmdp.States; state++)
			{
				var cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfState(state);
				Clear(cidOfStateRoot);
				_ltmdpContinuationDistributionMapper.AddInitialDistributionAndContinuation(cidOfStateRoot);

				UpdateContinuationDistributionMapperAndCollectLeafs(cidOfStateRoot);

				MarkovDecisionProcess.StartWithNewDistributions(state);

				var numberOfDistributions = _ltmdpContinuationDistributionMapper.GetNumbersOfDistributions();
				for (var distribution = 0; distribution < numberOfDistributions; distribution++)
				{
					AddDistribution(distribution);
				}

				MarkovDecisionProcess.FinishDistributions();
			}
		}
		
		public void ConvertInitialTransitions()
		{
			var cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfInitialState();
			Clear(cidOfStateRoot);
			_ltmdpContinuationDistributionMapper.AddInitialDistributionAndContinuation(cidOfStateRoot);

			UpdateContinuationDistributionMapperAndCollectLeafs(cidOfStateRoot);

			MarkovDecisionProcess.StartWithInitialDistributions();

			var numberOfDistributions = _ltmdpContinuationDistributionMapper.GetNumbersOfDistributions();
			for (var distribution = 0; distribution < numberOfDistributions; distribution++)
			{
				AddInitialDistribution(distribution);
			}
			
			MarkovDecisionProcess.FinishInitialDistributions();
		}

		public NmdpToMdpByFlattening(NestedMarkovDecisionProcess nmdp)
			: base(nmdp)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			Console.Out.WriteLine("Starting to convert Nested Markov Decision Process to Markov Decision Process");
			Console.Out.WriteLine($"Nmdp: States {nmdp.States}, ContinuationGraphSize {nmdp.ContinuationGraphSize}");
			var modelCapacity = new ModelCapacityByModelSize(nmdp.States, nmdp.ContinuationGraphSize * 1000L);
			MarkovDecisionProcess = new MarkovDecisionProcess(modelCapacity);
			MarkovDecisionProcess.StateFormulaLabels = nmdp.StateFormulaLabels;
			CopyStateLabeling();
			ConvertInitialTransitions();
			ConvertStateTransitions();
			stopwatch.Stop();
			_nmdp = null;
			Console.Out.WriteLine($"Completed transformation in {stopwatch.Elapsed}");
			Console.Out.WriteLine($"Mdp: States {MarkovDecisionProcess.States}, Transitions {MarkovDecisionProcess.Transitions}");
		}
	}
}
