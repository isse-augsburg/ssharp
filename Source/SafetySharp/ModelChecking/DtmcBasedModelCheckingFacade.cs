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
using System.Threading.Tasks;

namespace SafetySharp.Analysis.Probabilistic.DtmcBased
{
	using System.Collections.Concurrent;
	using ISSE.SafetyChecking.Modeling;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.Utilities;
	using Modeling;
	using System.Threading.Tasks.Dataflow;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using Runtime;

	// Facade for easier use
	public class DtmcBasedModelCheckingFacade
	{
		//TODO: Safe disposal of BuiltinDtmcModelChecker

		private DtmcFromExecutableModelGenerator<SafetySharpRuntimeModel> _generator;

		private HashSet<Formula> _booleanFormulas;
		private HashSet<Formula> _probabilityFormulas;
		private HashSet<Formula> _rewardFormulas;

		private ConcurrentDictionary<Formula, Task<bool>> _booleanFormulaResults;
		private ConcurrentDictionary<Formula, Task<Probability>> _probabilityFormulaResults;
		private ConcurrentDictionary<Formula, Task<Reward>> _rewardFormulaResults;

		private BufferBlock<DtmcModelChecker> _modelCheckers;

		public DtmcBasedModelCheckingFacade(ModelBase model)
		{
			var createModel = SafetySharpRuntimeModel.CreateExecutedModelFromFormulasCreator(model);

			_generator =new DtmcFromExecutableModelGenerator<SafetySharpRuntimeModel>(createModel);
		}

		private async Task<bool> CheckBoolean(Formula booleanFormula)
		{
			var modelChecker = await _modelCheckers.ReceiveAsync();
			var result = modelChecker.CalculateBoolean(booleanFormula);
			_modelCheckers.Post(modelChecker);
			return result;
		}

		private async Task<Probability> CheckProbability(Formula probabilityFormula)
		{
			var modelChecker = await _modelCheckers.ReceiveAsync();
			var result = modelChecker.CalculateProbability(probabilityFormula);
			_modelCheckers.Post(modelChecker);
			return result;
		}

		private async Task<RewardResult> CheckReward(Formula rewardFormula)
		{
			var modelChecker = await _modelCheckers.ReceiveAsync();
			var result = modelChecker.CalculateReward(rewardFormula);
			_modelCheckers.Post(modelChecker);
			return result;
		}

		public void StartChecking(Formula terminateEarlyCondition=null,int numberOfModelCheckers=1)
		{
			var dtmc=_generator.GenerateMarkovChain(terminateEarlyCondition);

			for (var i = 0; i < numberOfModelCheckers; i++)
			{
				var newModelChecker = new BuiltinDtmcModelChecker(dtmc);
				_modelCheckers.Post(newModelChecker);
			}

			foreach (var booleanFormula in _booleanFormulas)
			{
				Task<bool> task = CheckBoolean(booleanFormula);
				task.Start();
			}
			foreach (var probabilityFormula in _probabilityFormulas)
			{
				Task<Probability> task = CheckProbability(probabilityFormula);
				task.Start();
			}
			foreach (var rewardFormula in _rewardFormulas)
			{
				Task<RewardResult> task = CheckReward(rewardFormula);
				task.Start();
			}
		}

		public void AddBooleanFormula(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulaReturningBoolValueVisitor();
			visitor.Visit(formula);
			if (!visitor.IsFormulaReturningBoolValue)
				throw new InvalidOperationException("Formula must return bool.");
			
			_generator.AddFormulaToCheck(formula);
			_booleanFormulas.Add(formula);
		}

		public void AddProbabilityFormula(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulaReturningBoolValueVisitor();
			visitor.Visit(formula);
			if (!visitor.IsFormulaReturningBoolValue)
				throw new InvalidOperationException("Formula must return bool.");

			_generator.AddFormulaToCheck(formula);
			_probabilityFormulas.Add(formula);
		}

		public void AddRewardFormula(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulaReturningRewardResultVisitor();
			visitor.Visit(formula);
			if (!visitor.IsReturningRewardResult)
				throw new InvalidOperationException("Formula must return reward.");

			_generator.AddFormulaToCheck(formula);
			_rewardFormulas.Add(formula);
		}


		public Task<bool> GetBooleanResult(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));
			if (!_generator.ProbabilityMatrixCreationStarted)
				throw new InvalidOperationException(nameof(GetBooleanResult)+" must be called after "+ nameof(StartChecking));

			return _booleanFormulaResults[formula];
		}

		public Task<Probability> GetProbabilityResult(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));
			if (!_generator.ProbabilityMatrixCreationStarted)
				throw new InvalidOperationException(nameof(GetProbabilityResult) + " must be called after " + nameof(StartChecking));
			
			return _probabilityFormulaResults[formula];
		}

		public Task<Reward> GetRewardResult(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));
			if (!_generator.ProbabilityMatrixCreationStarted)
				throw new InvalidOperationException(nameof(GetRewardResult) + " must be called after " + nameof(StartChecking));
			
			return _rewardFormulaResults[formula];
		}

	}
}
