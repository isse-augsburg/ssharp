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

using ISSE.SafetyChecking;
using ISSE.SafetyChecking.DiscreteTimeMarkovChain;
using ISSE.SafetyChecking.ExecutableModel;
using ISSE.SafetyChecking.Formula;
using ISSE.SafetyChecking.Modeling;
using ISSE.SafetyChecking.Simulator;
using System.Collections.Generic;
using System.Linq;

namespace SafetyLustre
{
    public sealed class LustreSimulator : Simulator<LustreExecutableModel>
    {
        /// <summary>
        ///   Gets the model that is simulated, i.e., a copy of the original model passed to the simulator.
        /// </summary>
        public LustreModelBase Model => RuntimeModel.Model;

        /// <summary>
        ///   Initializes a new instance.
        /// </summary>
        public LustreSimulator(string ocFileName, string mainNode, IEnumerable<Fault> faults, params Formula[] formulas)
        : base(LustreExecutableModel.CreateExecutedModelCreator(ocFileName, mainNode, faults.ToArray()).Create(0)) { }

        public LustreSimulator(ExecutableCounterExample<LustreExecutableModel> counterExample)
        : base(counterExample) { }
    }

    public sealed class LustreProbabilisticSimulator : ProbabilisticSimulator<LustreExecutableModel>
    {
        /// <summary>
        ///   Gets the model that is simulated, i.e., a copy of the original model passed to the simulator.
        /// </summary>
        public LustreModelBase Model => RuntimeModel.Model;

        public static AnalysisConfiguration Configuration = AnalysisConfiguration.Default;

        /// <summary>
        ///   Initializes a new instance.
        /// </summary>
        public LustreProbabilisticSimulator(string ocFileName, string mainNode, IEnumerable<Fault> faults, params Formula[] formulas)
        : base(LustreExecutableModel.CreateExecutedModelFromFormulasCreator(ocFileName, mainNode, faults.ToArray()), formulas, Configuration) { }
    }
}
