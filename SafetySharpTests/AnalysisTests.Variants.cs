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


namespace Tests
{
	using System;
	using System.IO;
	using System.Linq;
	using ISSE.SafetyChecking;
	using ISSE.SafetyChecking.ExecutableModel;
	using SafetySharp.Analysis;
	using SafetySharp.ModelChecking;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Utilities;
	using ISSE.SafetyChecking.AnalysisModel;
	using ISSE.SafetyChecking.ExecutedModel;
	using ISSE.SafetyChecking.FaultMinimalKripkeStructure;
	using ISSE.SafetyChecking.Formula;
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using ISSE.SafetyChecking.Modeling;

	public abstract class AnalysisTestsVariant
	{
		public abstract void SetModelCheckerParameter(bool suppressCounterExampleGeneration, TextWriter output);

		public abstract void SetExecutionParameter(bool allowFaultsOnInitialTransitions);

		public abstract InvariantAnalysisResult[] CheckInvariants(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants);

		public abstract InvariantAnalysisResult CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula);

		public abstract InvariantAnalysisResult Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula);
	}

	public class AnalysisTestsWithLtsMin: AnalysisTestsVariant
	{
		private LtsMin _modelChecker;

		public override void SetModelCheckerParameter(bool suppressCounterExampleGeneration, TextWriter output)
		{
			// QualitativeChecker<SafetySharpRuntimeModel>
			// LtsMin
			_modelChecker = new LtsMin();
			_modelChecker.Output= output;
		}

		public override void SetExecutionParameter(bool allowFaultsOnInitialTransitions)
		{
			//TODO
		}

		public override InvariantAnalysisResult Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			return _modelChecker.Check(createModel, formula);
		}

		public override InvariantAnalysisResult CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			return _modelChecker.CheckInvariant(createModel, formula);
		}

		public override InvariantAnalysisResult[] CheckInvariants(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants)
		{
			throw new NotImplementedException();
		}
	}

	public class AnalysisTestsWithQualitative : AnalysisTestsVariant
	{
		private bool _suppressCounterExampleGeneration;
		private AnalysisConfiguration _analysisConfiguration;
		
		public override void SetModelCheckerParameter(bool suppressCounterExampleGeneration, TextWriter output)
		{
			_suppressCounterExampleGeneration = suppressCounterExampleGeneration;
			_analysisConfiguration = AnalysisConfiguration.Default;
			_analysisConfiguration.DefaultTraceOutput = output;
			_analysisConfiguration.ModelCapacity = ModelCapacityByMemorySize.Small;
			_analysisConfiguration.GenerateCounterExample = !suppressCounterExampleGeneration;
		}

		public override void SetExecutionParameter(bool allowFaultsOnInitialTransitions)
		{
			_analysisConfiguration.AllowFaultsOnInitialTransitions = allowFaultsOnInitialTransitions;
		}

		public override InvariantAnalysisResult Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			throw new NotImplementedException();
		}

		public override InvariantAnalysisResult CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel,Formula formula)
		{
			var checker = new QualitativeChecker<SafetySharpRuntimeModel>(createModel);
			checker.Configuration = _analysisConfiguration;
			return checker.CheckInvariant(formula);
		}

		public override InvariantAnalysisResult[] CheckInvariants(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants)
		{
			var checker = new QualitativeChecker<SafetySharpRuntimeModel>(createModel);
			checker.Configuration = _analysisConfiguration;
			return checker.CheckInvariants(invariants);
		}
	}

	public class AnalysisTestsWithQualitativeWithIndex : AnalysisTestsVariant
	{
		private bool _suppressCounterExampleGeneration;
		private AnalysisConfiguration _analysisConfiguration;

		public override void SetModelCheckerParameter(bool suppressCounterExampleGeneration, TextWriter output)
		{
			_suppressCounterExampleGeneration = suppressCounterExampleGeneration;
			_analysisConfiguration = AnalysisConfiguration.Default;
			_analysisConfiguration.DefaultTraceOutput = output;
			_analysisConfiguration.ModelCapacity=ModelCapacityByMemorySize.Small;
			_analysisConfiguration.GenerateCounterExample = !suppressCounterExampleGeneration;
		}

		public override void SetExecutionParameter(bool allowFaultsOnInitialTransitions)
		{
			_analysisConfiguration.AllowFaultsOnInitialTransitions = allowFaultsOnInitialTransitions;
		}

		public override InvariantAnalysisResult Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			throw new NotImplementedException();
		}

		public override InvariantAnalysisResult CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			var checker = new QualitativeChecker<SafetySharpRuntimeModel>(createModel);
			checker.Configuration = _analysisConfiguration;
			var formulaIndex = Array.FindIndex(createModel.StateFormulasToCheckInBaseModel, stateFormula =>
				{
					var isEqual=IsFormulasStructurallyEquivalentVisitor.Compare(stateFormula, formula);
					return isEqual;
				}
			);
			if (formulaIndex==-1)
				throw new Exception($"Input formula is not checked directly. Use {nameof(AnalysisTestsWithQualitative)} instead");

			return checker.CheckInvariant(formulaIndex);
		}

		public override InvariantAnalysisResult[] CheckInvariants(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants)
		{
			var checker = new QualitativeChecker<SafetySharpRuntimeModel>(createModel);
			checker.Configuration = _analysisConfiguration;
			return checker.CheckInvariants(invariants);
		}
	}
}
