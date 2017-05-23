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

	internal sealed class NmdpToMdp
	{
		private readonly NestedMarkovDecisionProcess _nmdp;
		public MarkovDecisionProcess MarkovDecisionProcess { get; private set; }

		private long _currentOffset;
		private AutoResizeVector<NestedMarkovDecisionProcess.ContinuationGraphLeaf> _continuationGraphLeafOfCid = new AutoResizeVector<NestedMarkovDecisionProcess.ContinuationGraphLeaf>();
		
		private AutoResizeVector<double> _probabilityOfCid = new AutoResizeVector<double>();

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
			var internalCid = cid - _currentOffset;
			Assert.That(internalCid <= int.MaxValue, "internalCid<=int.MaxValue");
			return _continuationGraphLeafOfCid[(int)internalCid];
		}

		private void SetLeafOfCid(long cid, NestedMarkovDecisionProcess.ContinuationGraphLeaf leaf)
		{
			var internalCid = cid - _currentOffset;
			Assert.That(internalCid <= int.MaxValue, "internalCid<=int.MaxValue");
			_continuationGraphLeafOfCid[(int)internalCid]= leaf;
		}

		private double GetProbabilityOfCid(long cid)
		{
			var internalCid = cid - _currentOffset;
			Assert.That(internalCid <= int.MaxValue, "internalCid<=int.MaxValue");
			return _probabilityOfCid[(int)internalCid];
		}

		private void SetProbabilityOfCid(long cid, double probability)
		{
			var internalCid = cid - _currentOffset;
			Assert.That(internalCid <= int.MaxValue, "internalCid<=int.MaxValue");
			_probabilityOfCid[(int)internalCid] = probability;
		}

		private void MultiplyProbabilityOfCid(long cid, double factor)
		{
			var internalCid = cid - _currentOffset;
			Assert.That(internalCid <= int.MaxValue, "internalCid<=int.MaxValue");
			_probabilityOfCid[(int)internalCid] *= factor;
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
				if (cge.IsChoiceTypeDeterministic)
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
				MarkovDecisionProcess.AddTransition(leaf.ToState, GetProbabilityOfCid(enumerator.CurrentContinuationId));
			}
			
			MarkovDecisionProcess.FinishDistribution();
		}


		private void AddInitialDistribution(int distribution)
		{
			if (_ltmdpContinuationDistributionMapper.IsDistributionEmpty(distribution))
				return;

			MarkovDecisionProcess.StartWithNewInitialDistribution();

			var enumerator = _ltmdpContinuationDistributionMapper.GetContinuationsOfDistributionEnumerator(distribution);
			while (enumerator.MoveNext())
			{
				var leaf = GetLeafOfCid(enumerator.CurrentContinuationId);
				var probability = GetProbabilityOfCid(enumerator.CurrentContinuationId);
				MarkovDecisionProcess.AddTransitionToInitialDistribution(leaf.ToState, probability);
			}
			
			MarkovDecisionProcess.FinishInitialDistribution();
		}

		private void Clear()
		{
			_probabilityOfCid.Clear();
			_probabilityOfCid[0] = 1.0;
			_ltmdpContinuationDistributionMapper.Clear();
		}

		private void ConvertStateTransitions()
		{
			for (var state = 0; state < _nmdp.States; state++)
			{
				Clear();
				var cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfState(state);
				_currentOffset = cidOfStateRoot;
				_ltmdpContinuationDistributionMapper.AddInitialDistributionAndContinuation(_currentOffset);

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
			Clear();
			var cidOfStateRoot = _nmdp.GetRootContinuationGraphLocationOfInitialState();
			_currentOffset = cidOfStateRoot;
			_ltmdpContinuationDistributionMapper.AddInitialDistributionAndContinuation(_currentOffset);

			UpdateContinuationDistributionMapperAndCollectLeafs(cidOfStateRoot);

			MarkovDecisionProcess.StartWithInitialDistributions();

			var numberOfDistributions = _ltmdpContinuationDistributionMapper.GetNumbersOfDistributions();
			for (var distribution = 0; distribution < numberOfDistributions; distribution++)
			{
				AddInitialDistribution(distribution);
			}
			
			MarkovDecisionProcess.FinishInitialDistributions();
		}

		public NmdpToMdp(NestedMarkovDecisionProcess nmdp)
		{
			var stopwatch = new Stopwatch();
			stopwatch.Start();
			Console.Out.WriteLine("Starting to convert Nested Markov Decision Process to Markov Decision Process");
			Console.Out.WriteLine($"Nmdp: States {nmdp.States}, ContinuationGraphSize {nmdp.ContinuationGraphSize}");
			_nmdp = nmdp;
			var modelCapacity = new ModelCapacityByModelSize(nmdp.States, nmdp.ContinuationGraphSize * 8L);
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
