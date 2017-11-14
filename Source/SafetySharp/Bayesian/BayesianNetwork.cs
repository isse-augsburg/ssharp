using System;
using System.Collections.Generic;
using System.Linq;

namespace SafetySharp.Bayesian
{
    /// <summary>
    /// Class for managing Bayesian Networks consisting of a DAG over random variables and their conditional probability distributions.
    /// </summary>
    public class BayesianNetwork
    {
        public DagPattern<RandomVariable> Dag { get; }
        public Dictionary<RandomVariable, ProbabilityDistribution> Distributions { get; }
        public IList<RandomVariable> RandomVariables => Dag.Nodes;

        /// <summary>
        /// Creates a Bayesian Network from a given DagPattern which has to be a DAG and calculates the conditional distributions according to the DAG structure.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the given DagPattern is no DAG</exception>
        public static BayesianNetwork FromDagPattern(DagPattern<RandomVariable> dag, IProbabilityDistributionCalculator calc)
        {
            if (!dag.IsDag())
            {
                throw new ArgumentException("BayesianNetworks need a DAG, but the given DAG was actually a DAG pattern!");
            }
            var network = new BayesianNetwork(dag);
            foreach (var rvar in dag.Nodes)
            {
                var parents = dag.GetParents(rvar).ToList();
                var distribution = calc.CalculateConditionalProbabilityDistribution(rvar, parents);
                network.Distributions[rvar] = new ProbabilityDistribution(rvar, parents, distribution);
            }
            return network;
        }

        /// <summary>
        /// Creates a Bayesian Network from a given DagPattern which has to be a DAG and uses the given distributions if they fit the DAG structure.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the given DagPattern is no DAG or the distributions don't fit the DAG structure</exception>
        public static BayesianNetwork FromDagPattern(DagPattern<RandomVariable> dag, IEnumerable<ProbabilityDistribution> distributions)
        {
            if (!dag.IsDag())
            {
                throw new ArgumentException("BayesianNetworks need a DAG, but the given DAG was actually a DAG pattern!");
            }
            var network = new BayesianNetwork(dag);
            var processed = new bool[network.RandomVariables.Count];
            foreach (var distribution in distributions)
            {
                var rvar = distribution.RandomVariable;
                var parents = dag.GetParents(rvar);
                // do the conditions fit with the dag structure?
                if (parents.Except(distribution.Conditions).Any() || parents.Count != distribution.Conditions.Count)
                {
                    throw new ArgumentException($"The random variable {rvar.Name} has parents {string.Join(",", parents)} but conditions {string.Join(",", distribution.Conditions)}");
                }
                network.Distributions[rvar] = distribution;
                processed[network.RandomVariables.IndexOf(rvar)] = true;

                // fill distributions in random variables without parents
                if (parents.Count == 0)
                {
                    rvar.Probability = distribution.Distribution;
                }
            }
            if (!processed.All(p => p))
            {
                throw new ArgumentException("The given probability distribution did not include all random variables!");
            }

            return network;
        }

        private BayesianNetwork(DagPattern<RandomVariable> dag)
        {
            Dag = dag;
            Distributions = new Dictionary<RandomVariable, ProbabilityDistribution>();
        }
    }
}