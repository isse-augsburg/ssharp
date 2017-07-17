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
	using System.Linq;
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
		public abstract void CreateModelChecker(bool suppressCounterExampleGeneration,Action<string> logAction);

		public abstract InvariantAnalysisResult<SafetySharpRuntimeModel>[] CheckInvariants(ExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants);

		public abstract InvariantAnalysisResult<SafetySharpRuntimeModel> CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula);

		public abstract InvariantAnalysisResult<SafetySharpRuntimeModel> Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula);
	}

	public class AnalysisTestsWithLtsMin: AnalysisTestsVariant
	{
		private LtsMin modelChecker;

		public override void CreateModelChecker(bool suppressCounterExampleGeneration, Action<string> logAction)
		{
			// QualitativeChecker<SafetySharpRuntimeModel>
			// LtsMin
			modelChecker = new LtsMin();
			modelChecker.OutputWritten += logAction;
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel> Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			return modelChecker.Check(createModel, formula);
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel> CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			return modelChecker.CheckInvariant(createModel, formula);
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel>[] CheckInvariants(ExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants)
		{
			throw new NotImplementedException();
		}
	}

	public class AnalysisTestsWithQualitative : AnalysisTestsVariant
	{
		private QualitativeChecker<SafetySharpRuntimeModel> modelChecker;
		
		public override void CreateModelChecker(bool suppressCounterExampleGeneration, Action<string> logAction)
		{
			// QualitativeChecker<SafetySharpRuntimeModel>
			// LtsMin
			modelChecker = new SafetySharpQualitativeChecker();
			modelChecker.OutputWritten += logAction;
			
			modelChecker.Configuration.ModelCapacity = ModelCapacityByMemorySize.Small;
			modelChecker.Configuration.GenerateCounterExample = !suppressCounterExampleGeneration;
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel> Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			throw new NotImplementedException();
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel> CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel,Formula formula)
		{
			return modelChecker.CheckInvariant(createModel, formula);
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel>[] CheckInvariants(ExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants)
		{
			return modelChecker.CheckInvariants(createModel, invariants);
		}
	}

	public class AnalysisTestsWithQualitativeWithIndex : AnalysisTestsVariant
	{
		private QualitativeChecker<SafetySharpRuntimeModel> modelChecker;

		public override void CreateModelChecker(bool suppressCounterExampleGeneration, Action<string> logAction)
		{
			// QualitativeChecker<SafetySharpRuntimeModel>
			// LtsMin
			modelChecker = new SafetySharpQualitativeChecker();
			modelChecker.OutputWritten += logAction;

			modelChecker.Configuration.ModelCapacity=ModelCapacityByMemorySize.Small;
			modelChecker.Configuration.GenerateCounterExample = !suppressCounterExampleGeneration;
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel> Check(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			throw new NotImplementedException();
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel> CheckInvariant(CoupledExecutableModelCreator<SafetySharpRuntimeModel> createModel, Formula formula)
		{
			var formulaIndex = Array.FindIndex(createModel.StateFormulasToCheckInBaseModel, stateFormula =>
				{
					var isEqual=IsFormulasStructurallyEquivalentVisitor.Compare(stateFormula, formula);
					return isEqual;
				}
			);
			if (formulaIndex==-1)
				throw new Exception($"Input formula is not checked directly. Use {nameof(AnalysisTestsWithQualitative)} instead");

			return modelChecker.CheckInvariant(createModel, formulaIndex);
		}

		public override InvariantAnalysisResult<SafetySharpRuntimeModel>[] CheckInvariants(ExecutableModelCreator<SafetySharpRuntimeModel> createModel, params Formula[] invariants)
		{
			return modelChecker.CheckInvariants(createModel, invariants);
		}
	}
}
