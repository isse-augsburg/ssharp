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

namespace Tests.SimpleExecutableModel
{
	using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
	using System;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.AnalysisModelTraverser;
	using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
	using ISSE.SafetyChecking.FaultMinimalKripkeStructure;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.MarkovDecisionProcess;
	using ISSE.SafetyChecking.Simulator;

	
	public sealed class SimpleSafetyAnalysis : SafetyAnalysis<SimpleExecutableModel>
	{
		public SafetyAnalysisResults<SimpleExecutableModel> ComputeMinimalCriticalSets(SimpleModelBase model, Formula collision, int maxCardinality = Int32.MaxValue)
		{
			var modelCreator = SimpleExecutableModel.CreateExecutedModelCreator(model, collision);
			return ComputeMinimalCriticalSets(modelCreator, collision, maxCardinality);
		}

		public static SafetyAnalysisResults<SimpleExecutableModel> AnalyzeHazard(SimpleModelBase model, Formula hazard, int maxCardinality = Int32.MaxValue,
														  SafetyAnalysisBackend backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly)
		{
			var modelCreator = SimpleExecutableModel.CreateExecutedModelCreator(model, hazard);
			return AnalyzeHazard(modelCreator, hazard, maxCardinality, backend);
		}
	}


	public sealed class SimpleOrderAnalysis : OrderAnalysis<SimpleExecutableModel>
	{
		public SimpleOrderAnalysis(SafetyAnalysisResults<SimpleExecutableModel> results, AnalysisConfiguration configuration)
			: base(results, configuration)
		{
		}
	}

	public sealed class SimpleQualitativeChecker : QualitativeChecker<SimpleExecutableModel>
	{
		public SimpleQualitativeChecker(SimpleModelBase model, params Formula[] formulas)
			: base(SimpleExecutableModel.CreateExecutedModelCreator(model, formulas))
		{
			
		}
	}

	public sealed class SimpleMarkovChainFromExecutableModelGenerator : MarkovChainFromExecutableModelGenerator<SimpleExecutableModel>
	{
		public SimpleMarkovChainFromExecutableModelGenerator(SimpleModelBase model) : base(SimpleExecutableModel.CreateExecutedModelFromFormulasCreator(model))
		{
		}
	}

	public sealed class SimpleNmdpFromExecutableModelGenerator : NmdpFromExecutableModelGenerator<SimpleExecutableModel>
	{
		public SimpleNmdpFromExecutableModelGenerator(SimpleModelBase model) : base(SimpleExecutableModel.CreateExecutedModelFromFormulasCreator(model))
		{
		}
	}
}
