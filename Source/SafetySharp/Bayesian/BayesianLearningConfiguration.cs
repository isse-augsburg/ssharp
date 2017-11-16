namespace SafetySharp.Bayesian
{
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

        public static BayesianLearningConfiguration Default => new BayesianLearningConfiguration
        {
            UseDccaResultsForLearning = true,
            MaxConditionSize = int.MaxValue,
            UseRealProbabilitiesForSimulation = true,
            ProbabilitySerializationFilePath = "",
            ProbabilityDeserializationFilePath = "",
            BayesianNetworkSerializationPath = ""
        };

    }
}