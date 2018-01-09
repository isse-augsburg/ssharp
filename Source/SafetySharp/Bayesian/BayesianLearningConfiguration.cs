// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Stefan Fritsch
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

namespace SafetySharp.Bayesian
{
    using System.Collections.Generic;

    /// <summary>
    /// Configurations for learning bayesian networks
    /// </summary>
    public struct BayesianLearningConfiguration
    {
        /// <summary>
        /// Use the results of a DCCA for the structure learning algorithms, the DCCA structure will be guaranteed in the learned model
        /// </summary>
        public bool UseDccaResultsForLearning { get; set; }

        /// <summary>
        /// The maximal size for condition sets while learning conditional independencies
        /// </summary>
        public int MaxConditionSize { get; set; }

        /// <summary>
        /// Flag whether to use the actual probabilities while generating the simulation data. If false, a uniform distribution will be used
        /// </summary>
        public bool UseRealProbabilitiesForSimulation { get; set; }

        /// <summary>
        /// Optional file path for serializing the calculated probabilites while modelchecking
        /// </summary>
        public string ProbabilitySerializationFilePath { get; set; }

        /// <summary>
        /// Optional file path for deserializing already known probabilities for modelchecking
        /// </summary>
        public string ProbabilityDeserializationFilePath { get; set; }

        /// <summary>
        /// Optional file path for serializing the bayesian network result
        /// </summary>
        public string BayesianNetworkSerializationPath { get; set; }

        public IList<string> FurtherSimulationDataFiles { get; set; } 

        public static BayesianLearningConfiguration Default => new BayesianLearningConfiguration
        {
            UseDccaResultsForLearning = true,
            MaxConditionSize = int.MaxValue,
            UseRealProbabilitiesForSimulation = true,
            ProbabilitySerializationFilePath = "",
            ProbabilityDeserializationFilePath = "",
            BayesianNetworkSerializationPath = "",
            FurtherSimulationDataFiles = new List<string>()
        };

    }
}