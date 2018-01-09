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
using ISSE.SafetyChecking.ExecutableModel;
using System.Linq.Expressions;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using ISSE.SafetyChecking.Utilities;

namespace Tests.SimpleExecutableModel
{
	using System.Linq;

	public unsafe class LustreExecutableModel :  ExecutableModel<LustreExecutableModel>
	{
		internal LustreModelBase Model { get; private set; }
		
		public override int StateVectorSize => Model.StateVectorSize;

		private AtomarPropositionFormula[] _atomarPropositionFormulas;

		public override AtomarPropositionFormula[] AtomarPropositionFormulas => _atomarPropositionFormulas;

		public override CounterExampleSerialization<LustreExecutableModel> CounterExampleSerialization => new LustreExecutableModelCounterExampleSerialization();
		
		public LustreExecutableModel(byte[] serializedModel)
		{
			SerializedModel = serializedModel;
			InitializeFromSerializedModel();
		}

		private void InitializeFromSerializedModel()
		{
			var modelWithFormula = LustreModelSerializer.DeserializeFromByteArray(SerializedModel);
			Model = modelWithFormula.Item1;
			Formulas = modelWithFormula.Item2;

			var atomarPropositionVisitor = new CollectAtomarPropositionFormulasVisitor();
			_atomarPropositionFormulas = atomarPropositionVisitor.AtomarPropositionFormulas.ToArray();
			foreach (var stateFormula in Formulas)
			{
				atomarPropositionVisitor.Visit(stateFormula);
			}

			StateConstraints = new Func<bool>[0];
			
			Faults = new Fault[0];
			UpdateFaultSets();

			_deserialize = LustreModelSerializer.CreateFastInPlaceDeserializer(Model);
			_serialize = LustreModelSerializer.CreateFastInPlaceSerializer(Model);
			_restrictRanges = () => { };
			
			InitializeConstructionState();
			CheckConsistencyAfterInitialization();
		}

		public override void ExecuteInitialStep()
		{
			foreach (var fault in NondeterministicFaults)
				fault.Reset();

			Model.SetInitialState();
		}

		public override void ExecuteStep()
		{
			foreach (var fault in NondeterministicFaults)
				fault.Reset();

			Model.Update();
		}

		public override void SetChoiceResolver(ChoiceResolver choiceResolver)
		{
			Model.Choice.Resolver = choiceResolver;
		}
		
		public static CoupledExecutableModelCreator<LustreExecutableModel> CreateExecutedModelCreator(string ocFileName, params Formula[] formulasToCheckInBaseModel)
		{
			Requires.NotNull(ocFileName, nameof(ocFileName));
			Requires.NotNull(formulasToCheckInBaseModel, nameof(formulasToCheckInBaseModel));
			

			Func<int, LustreExecutableModel> creatorFunc = (reservedBytes) =>
			{
				// Each model checking thread gets its own SimpleExecutableModel.
				// Thus, we serialize the C# model and load this file again.
				// The serialization can also be used for saving counter examples
				var serializedModelWithFormulas = LustreModelSerializer.CreateByteArray(ocFileName, formulasToCheckInBaseModel);
				var simpleExecutableModel=new LustreExecutableModel(serializedModelWithFormulas);
				return simpleExecutableModel;
			};
			
			var faults = new Fault[0];
			return new CoupledExecutableModelCreator<LustreExecutableModel>(creatorFunc, ocFileName, formulasToCheckInBaseModel, faults);
		}

		public static ExecutableModelCreator<LustreExecutableModel> CreateExecutedModelFromFormulasCreator(string ocFileName)
		{
			Requires.NotNull(ocFileName, nameof(ocFileName));

			Func<Formula[], CoupledExecutableModelCreator<LustreExecutableModel>> creator = formulasToCheckInBaseModel =>
			{
				Requires.NotNull(formulasToCheckInBaseModel, nameof(formulasToCheckInBaseModel));
				return CreateExecutedModelCreator(ocFileName, formulasToCheckInBaseModel);
			};
			return new ExecutableModelCreator<LustreExecutableModel>(creator, ocFileName);
		}

		public override Expression CreateExecutableExpressionFromAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			var atomarProposition = formula as LustreAtomarProposition;
			if (atomarProposition != null)
			{
				Func<bool> formulaEvaluatesToTrue = () => atomarProposition.Evaluate(Model);
				return Expression.Invoke(Expression.Constant(formulaEvaluatesToTrue));
			}

			throw new InvalidOperationException("AtomarPropositionFormula cannot be evaluated. Use SimpleAtomarProposition instead.");
		}
	}
}
