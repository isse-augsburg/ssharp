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

using ISSE.SafetyChecking.AnalysisModel;
using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
using ISSE.SafetyChecking.FaultMinimalKripkeStructure;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
using ISSE.SafetyChecking.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetyLustre
{

    public sealed class LustreSafetyAnalysis : SafetyAnalysis<LustreExecutableModel>
    {
        public SafetyAnalysisResults<LustreExecutableModel> ComputeMinimalCriticalSets(string ocFileName, string mainNode, IEnumerable<Fault> faults, Formula hazard, int maxCardinality = Int32.MaxValue)
        {
            var modelCreator = LustreExecutableModel.CreateExecutedModelCreator(ocFileName, mainNode, faults.ToArray(), hazard);
            return ComputeMinimalCriticalSets(modelCreator, hazard, maxCardinality);
        }

        public static SafetyAnalysisResults<LustreExecutableModel> AnalyzeHazard(string ocFileName, string mainNode, IEnumerable<Fault> faults, Formula hazard, int maxCardinality = Int32.MaxValue,
                                                          SafetyAnalysisBackend backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly)
        {
            var modelCreator = LustreExecutableModel.CreateExecutedModelCreator(ocFileName, mainNode, faults.ToArray(), hazard);
            return AnalyzeHazard(modelCreator, hazard, maxCardinality, backend);
        }

        public sealed class LustreQualitativeChecker : QualitativeChecker<LustreExecutableModel>
        {
            public static int maxValue = 1000;

            public LustreQualitativeChecker(string ocFileName, string mainNode, IEnumerable<Fault> faults, Formula invariant)
            : base(LustreExecutableModel.CreateExecutedModelCreator(ocFileName, mainNode, faults.ToArray(), invariant))
            {
            }

            public InvariantAnalysisResult CheckInvariant(Formula invariant, int max)
            {
                maxValue = max;
                return CheckInvariant(invariant);
            }
        }

        public sealed class LustreMarkovChainFromExecutableModelGenerator : MarkovChainFromExecutableModelGenerator<LustreExecutableModel>
        {
            public LustreMarkovChainFromExecutableModelGenerator(string ocFileName, string mainNode, IEnumerable<Fault> faults) : base(LustreExecutableModel.CreateExecutedModelFromFormulasCreator(ocFileName, mainNode, faults.ToArray())) { }
        }
    }
}
