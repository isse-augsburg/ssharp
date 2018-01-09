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

        public void PrintBayesianNetwork()
        {
            Console.Out.WriteLine("Bayesian Network:");
            Dag.ExportToGraphviz();
            foreach (var distribution in Distributions.Values)
            {
                Console.Out.WriteLine(distribution.ToMoreReadableString());
            }
        }

    }
}