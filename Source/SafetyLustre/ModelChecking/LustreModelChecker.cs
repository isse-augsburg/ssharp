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
using System.IO;
using System.Linq;
using ISSE.SafetyChecking;
using ISSE.SafetyChecking.MarkovDecisionProcess.Unoptimized;
using ISSE.SafetyChecking.AnalysisModel;
using ISSE.SafetyChecking.AnalysisModelTraverser;
using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.FaultMinimalKripkeStructure;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.MarkovDecisionProcess;
using ISSE.SafetyChecking.Modeling;
using ISSE.SafetyChecking.Utilities;
using LtmcModelChecker = ISSE.SafetyChecking.LtmcModelChecker;

namespace SafetyLustre
{
    /*
    /// <summary>
    ///   Provides convienent methods for model checking S# models.
    /// </summary>
    public static class LustreModelChecker
    {
        public static AnalysisConfiguration TraversalConfiguration;

        static LustreModelChecker()
        {
            TraversalConfiguration = AnalysisConfiguration.Default;
            TraversalConfiguration.EnableEarlyTermination = true;
        }



        /// <summary>
        ///   Calculates the probability to reach a state where <paramref name="stateFormula" /> holds.
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="stateFormula">The state formula to be checked.</param>
        public static Probability CalculateProbabilityToReachState(string ocFileName, IEnumerable<Fault> faults, Formula stateFormula)
        {
            var finallyStateFormula = new UnaryFormula(stateFormula, UnaryOperator.Finally);

            return CalculateProbabilityOfFormula(ocFileName, faults, finallyStateFormula);
        }

        /// <summary>
        ///   Calculates the probability of formula.
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="formula">The state formula to be checked.</param>
        public static Probability CalculateProbabilityOfFormula(string ocFileName, IEnumerable<Fault> faults, Formula formula)
        {
            Program.modelChecking = true;

            Probability probability;

            var createModel = LustreExecutableModel.CreateExecutedModelFromFormulasCreator(ocFileName, faults.ToArray());

            var markovChainGenerator = new MarkovChainFromExecutableModelGenerator<LustreExecutableModel>(createModel) { Configuration = TraversalConfiguration };
            markovChainGenerator.Configuration.SuccessorCapacity *= 2;
            markovChainGenerator.AddFormulaToCheck(formula);
            markovChainGenerator.Configuration.UseCompactStateStorage = true;
            var markovChain = markovChainGenerator.GenerateLabeledMarkovChain();

            using (var modelChecker = new ConfigurationDependentLtmcModelChecker(markovChainGenerator.Configuration, markovChain, TraversalConfiguration.DefaultTraceOutput))
            {
                probability = modelChecker.CalculateProbability(formula);
            }

            System.GC.Collect();
            return probability;
        }

        /// <summary>
        ///   Calculates the probability to reach a state where <paramref name="stateFormula" /> holds.
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="stateFormula">The state formula to be checked.</param>
        /// <param name="bound">The maximal number of steps. If stateFormula is satisfied the first time any step later than bound, this probability does not count into the end result.</param>
        public static Probability CalculateProbabilityToReachStateBounded(string ocFileName, IEnumerable<Fault> faults, Formula stateFormula, int bound)
        {
            var formula = new BoundedUnaryFormula(stateFormula, UnaryOperator.Finally, bound);

            return CalculateProbabilityOfFormula(ocFileName, faults, formula);
        }

        /// <summary>
        ///   Calculates the probability to reach a state where <paramref name="stateFormula" /> holds and on its way
        ///   invariantFormula holds in every state, or more formally Pr[invariantFormula U stateFormula].
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="stateFormula">The state formula which _must_ finally be true.</param>
        /// <param name="invariantFormula">The state formulas which must hold until stateFormula is satisfied.</param>
        /// <param name="bound">The maximal number of steps. If stateFormula is satisfied the first time any step later than bound, this probability does not count into the end result.</param>
        public static Probability CalculateProbabilityToReachStateBounded(string ocFileName, IEnumerable<Fault> faults, Formula stateFormula, Formula invariantFormula, int bound)
        {
            var formula = new BoundedBinaryFormula(invariantFormula, BinaryOperator.Until, stateFormula, bound);

            return CalculateProbabilityOfFormula(ocFileName, faults, formula);
        }

        /// <summary>
        ///   Calculates the probability to reach a state where <paramref name="stateFormula" /> holds.
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="stateFormula">The state formula which _must_ finally be true.</param>
        public static ProbabilityRange CalculateProbabilityRangeToReachState(string ocFileName, IEnumerable<Fault> faults, Formula stateFormula)
        {
            var finallyStateFormula = new UnaryFormula(stateFormula, UnaryOperator.Finally);

            return CalculateProbabilityRangeOfFormula(ocFileName, faults, finallyStateFormula);
        }



        /// <summary>
        ///   Calculates the probability of formula.
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="formula">The state formula to be checked.</param>
        public static ProbabilityRange CalculateProbabilityRangeOfFormula(string ocFileName, IEnumerable<Fault> faults, Formula formula)
        {
            ProbabilityRange probabilityRangeToReachState;

            var createModel = LustreExecutableModel.CreateExecutedModelFromFormulasCreator(ocFileName, faults.ToArray());

            var ltmdpGenerator = new MarkovDecisionProcessFromExecutableModelGenerator<LustreExecutableModel>(createModel) { Configuration = TraversalConfiguration };
            ltmdpGenerator.AddFormulaToCheck(formula);
            ltmdpGenerator.Configuration.SuccessorCapacity *= 8;
            ltmdpGenerator.Configuration.UseCompactStateStorage = true;
            var ltmdp = ltmdpGenerator.GenerateLabeledTransitionMarkovDecisionProcess();

            using (var modelChecker = new ConfigurationDependentLtmdpModelChecker(ltmdpGenerator.Configuration, ltmdp, TraversalConfiguration.DefaultTraceOutput))
            {
                probabilityRangeToReachState = modelChecker.CalculateProbabilityRange(formula);
            }

            System.GC.Collect();
            return probabilityRangeToReachState;
        }

        /// <summary>
        ///   Calculates the probability to reach a state whether <paramref name="stateFormula" /> holds.
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="stateFormula">The state formula which _must_ finally be true.</param>
        /// <param name="bound">The maximal number of steps. If stateFormula is satisfied the first time any step later than bound, this probability does not count into the end result.</param>
        public static ProbabilityRange CalculateProbabilityRangeToReachStateBounded(string ocFileName, IEnumerable<Fault> faults, Formula stateFormula, int bound)
        {
            var formula = new BoundedUnaryFormula(stateFormula, UnaryOperator.Finally, bound);
            return CalculateProbabilityRangeOfFormula(ocFileName, faults, formula);
        }



        /// <summary>
        ///   Calculates the probability to reach a state where <paramref name="stateFormula" /> holds and on its way
        ///   invariantFormula holds in every state, or more formally Pr[invariantFormula U stateFormula].
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="stateFormula">The state formula which _must_ finally be true.</param>
        /// <param name="invariantFormula">The state formulas which must hold until stateFormula is satisfied.</param>
        /// <param name="bound">The maximal number of steps. If stateFormula is satisfied the first time any step later than bound, this probability does not count into the end result.</param>
        public static ProbabilityRange CalculateProbabilityRangeToReachStateBounded(string ocFileName, IEnumerable<Fault> faults, Formula stateFormula, Formula invariantFormula, int bound)
        {
            var formula = new BoundedBinaryFormula(invariantFormula, BinaryOperator.Until, stateFormula, bound);

            return CalculateProbabilityRangeOfFormula(ocFileName, faults, formula);
        }


        /// <summary>
        ///   Conduct a quantitative analysis where parameters may vary
        /// </summary>
        /// <param name="model">The model that should be checked.</param>
        /// <param name="parameter">The parameters of the parametric analysis.</param>
        public static QuantitativeParametricAnalysisResults ConductQuantitativeParametricAnalysis(string ocFileName, IEnumerable<Fault> faults, QuantitativeParametricAnalysisParameter parameter)
        {
            var stepSize = (parameter.To - parameter.From) / (parameter.Steps - 1);

            var sourceValues = new double[parameter.Steps];
            var resultValues = new double[parameter.Steps];

            var faultsFlat = faults.ToArray();

            for (var i = 0; i < parameter.Steps; i++)
            {
                var currentValue = i * stepSize;
                sourceValues[i] = currentValue;
                parameter.UpdateParameterInModel(currentValue);

                double currentResult;
                if (parameter.Bound.HasValue)
                    currentResult = CalculateProbabilityToReachStateBounded(ocFileName, faultsFlat, parameter.StateFormula, parameter.Bound.Value).Value;
                else
                    currentResult = CalculateProbabilityToReachState(ocFileName, faultsFlat, parameter.StateFormula).Value;
                System.GC.Collect();
                resultValues[i] = currentResult;

            }

            var result = new QuantitativeParametricAnalysisResults
            {
                From = parameter.From,
                To = parameter.To,
                Steps = parameter.Steps,
                SourceValues = sourceValues,
                ResultValues = resultValues
            };
            return result;
        }
    }
    */
}