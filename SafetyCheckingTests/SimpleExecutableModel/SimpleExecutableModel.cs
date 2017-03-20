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
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text;
    using ISSE.SafetyChecking.Modeling;

    public unsafe class SimpleExecutableModel :  ExecutableModel<SimpleExecutableModel>
    {
        internal SimpleModelBase Model { get; private set; }
        
        public override int StateVectorSize { get; } = sizeof(int);

        private AtomarPropositionFormula[] _atomarPropositionFormulas;

        public override AtomarPropositionFormula[] AtomarPropositionFormulas => _atomarPropositionFormulas;

        public override CounterExampleSerialization<SimpleExecutableModel> CounterExampleSerialization => new SimpleExecutableModelCounterExampleSerialization();
        
        public SimpleExecutableModel(byte[] serializedModel, Formula[] formulas)
        {
            SerializedModel = serializedModel;
            Formulas = formulas;
            InitializeFromSerializedModel();
        }

        private void InitializeFromSerializedModel()
        {
            Model = SimpleModelSerializer.Deserialize(SerializedModel);
            
            var atomarPropositionVisitor = new CollectAtomarPropositionFormulasVisitor();
            _atomarPropositionFormulas = atomarPropositionVisitor.AtomarPropositionFormulas.ToArray();
            foreach (var stateFormula in Formulas)
            {
                atomarPropositionVisitor.Visit(stateFormula);
            }

            StateConstraints = new Func<bool>[0];
            
            Faults = new Fault[0];
            UpdateFaultSets();

            _deserialize = state =>
            {
                Model.State = *((int*)state);
            };
            _serialize = state =>
            {
                *((int*)state) = Model.State;
            };
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

        internal override void SetChoiceResolver(ChoiceResolver choiceResolver)
        {
            Model.Choice.Resolver = choiceResolver;
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
                var serializedModel = SimpleModelSerializer.Serialize(inputModel);
                var simpleExecutableModel=new SimpleExecutableModel(serializedModel,formulasToCheckInBaseModel);
                return simpleExecutableModel;
            };
            
            var faults = new Fault[0];
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
            var executableStateFormula = formula as SimpleExecutableFormula;
            if (executableStateFormula != null)
            {
                Func<bool> formulaEvaluatesToTrue = () => executableStateFormula.Evaluate(Model.State);
                return Expression.Invoke(Expression.Constant(formulaEvaluatesToTrue));
            }

            throw new InvalidOperationException("AtomarPropositionFormula cannot be evaluated. Use SimpleExecutableFormula instead.");
        }
    }
}
