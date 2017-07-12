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
using ISSE.SafetyChecking.Utilities;

namespace Tests.SimpleExecutableModel
{
	using System.Linq;

	public unsafe class SimpleExecutableModel :  ExecutableModel<SimpleExecutableModel>
	{
		internal SimpleModelBase Model { get; private set; }
		
		public override int StateVectorSize { get; } = sizeof(int) + sizeof(long);

		private AtomarPropositionFormula[] _atomarPropositionFormulas;

		public override AtomarPropositionFormula[] AtomarPropositionFormulas => _atomarPropositionFormulas;

		public override CounterExampleSerialization<SimpleExecutableModel> CounterExampleSerialization => new SimpleExecutableModelCounterExampleSerialization();
		
		public SimpleExecutableModel(byte[] serializedModel)
		{
			SerializedModel = serializedModel;
			InitializeFromSerializedModel();
		}

		private void InitializeFromSerializedModel()
		{
			var modelWithFormula = SimpleModelSerializer.DeserializeFromByteArray(SerializedModel);
			Model = modelWithFormula.Item1;
			Formulas = modelWithFormula.Item2;

			var atomarPropositionVisitor = new CollectAtomarPropositionFormulasVisitor();
			foreach (var stateFormula in Formulas)
			{
				atomarPropositionVisitor.Visit(stateFormula);
			}
			_atomarPropositionFormulas = atomarPropositionVisitor.AtomarPropositionFormulas.ToArray();

			StateConstraints = new Func<bool>[0];
			
			Faults = Model.Faults;
			UpdateFaultSets();

			_deserialize = SimpleModelSerializer.CreateFastInPlaceDeserializer(Model);
			_serialize = SimpleModelSerializer.CreateFastInPlaceSerializer(Model);
			_restrictRanges = () => { };
			
			InitializeConstructionState();
			CheckConsistencyAfterInitialization();
		}

		public override void ExecuteInitialStep()
		{
			Model.SetInitialState();
		}

		public override void ExecuteStep()
		{
			Model.Update();
		}

		public override void SetChoiceResolver(ChoiceResolver choiceResolver)
		{
			Model.Choice.Resolver = choiceResolver;

			foreach (var fault in Model.Faults)
			{
				fault.Choice.Resolver = choiceResolver;
			}
		}
		
		public static CoupledExecutableModelCreator<SimpleExecutableModel> CreateExecutedModelCreator(SimpleModelBase inputModel, params Formula[] formulasToCheckInBaseModel)
		{
			Requires.NotNull(inputModel, nameof(inputModel));
			Requires.NotNull(formulasToCheckInBaseModel, nameof(formulasToCheckInBaseModel));
			

			Func<int, SimpleExecutableModel> creatorFunc = (reservedBytes) =>
			{
				// Each model checking thread gets its own SimpleExecutableModel.
				// Thus, we serialize the C# model and load this file again.
				// The serialization can also be used for saving counter examples
				var serializedModelWithFormulas = SimpleModelSerializer.SerializeToByteArray(inputModel, formulasToCheckInBaseModel);
				var simpleExecutableModel=new SimpleExecutableModel(serializedModelWithFormulas);
				return simpleExecutableModel;
			};
			
			var faults = inputModel.Faults;
			return new CoupledExecutableModelCreator<SimpleExecutableModel>(creatorFunc, inputModel, formulasToCheckInBaseModel, faults);
		}

		public static ExecutableModelCreator<SimpleExecutableModel> CreateExecutedModelFromFormulasCreator(SimpleModelBase model)
		{
			Requires.NotNull(model, nameof(model));

			Func<Formula[], CoupledExecutableModelCreator<SimpleExecutableModel>> creator = formulasToCheckInBaseModel =>
			{
				Requires.NotNull(formulasToCheckInBaseModel, nameof(formulasToCheckInBaseModel));
				return CreateExecutedModelCreator(model, formulasToCheckInBaseModel);
			};
			return new ExecutableModelCreator<SimpleExecutableModel>(creator, model);
		}

		public override Expression CreateExecutableExpressionFromAtomarPropositionFormula(AtomarPropositionFormula formula)
		{
			var atomarProposition = formula as SimpleAtomarProposition;
			if (atomarProposition != null)
			{
				Func<bool> formulaEvaluatesToTrue = () => atomarProposition.Evaluate(Model);
				return Expression.Invoke(Expression.Constant(formulaEvaluatesToTrue));
			}

			throw new InvalidOperationException("AtomarPropositionFormula cannot be evaluated. Use SimpleAtomarProposition instead.");
		}
	}
}
