using System;
using System.Collections.Generic;

namespace SafetySharp.Bayesian
{
    using System.Linq;
    using ISSE.SafetyChecking.Modeling;

    /// <summary>
    /// Class for calculating arbitrary probabilities by unoptimized inference in a given BayesianNetwork.
    /// </summary>
    public class BayesianNetworkProbabilityDistributionCalculator : IProbabilityDistributionCalculator
    {

        private readonly BayesianNetwork _network;
        private readonly IList<RandomVariable> _randomVariables;
        private readonly double _tolerance;

        public BayesianNetworkProbabilityDistributionCalculator(BayesianNetwork network, double tolerance)
        {
            _network = network;
            _randomVariables = _network.RandomVariables;
            _tolerance = tolerance;
        }

        /// <summary>
        /// Calculates the probability of P(p1, ..., pi, n1, ..., nj)
        /// for p1,...,pi are all 'positive' values of binary random variables and n1,..,nj 'negative' ones.
        /// RandomVariables will be identified by name.
        /// </summary>
        public Probability CalculateProbability(ICollection<string> positiveNames, ICollection<string> negativeNames)
        {
            var positive = GetRandomVariablesByName(positiveNames);
            var negative = GetRandomVariablesByName(negativeNames);
            return CalculateProbability(positive, negative);
        }

        /// <summary>
        /// Calculates the probability of P(p1, ..., pi, n1, ..., nj)
        /// for p1,...,pi are all 'positive' values of binary random variables and n1,..,nj 'negative' ones.
        /// </summary>
        public Probability CalculateProbability(ICollection<RandomVariable> positive, ICollection<RandomVariable> negative)
        {
            var distribution = CalculateProbabilityDistribution(positive.Union(negative).ToList());
            var bitRepresentation = new string('0', positive.Count) + new string('1', negative.Count);
            var index = Convert.ToInt32(bitRepresentation, 2);
            return distribution[index];
        }

        /// <summary>
        /// Calculate the complete probability distribution of the given random variables
        /// e.g. for variables A,B: P(a,b), P(a,!b), P(!a,b) and P(!a,!b).
        /// RandomVariables will be identified by name.
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last to first</returns>
        public IList<Probability> CalculateProbabilityDistribution(IList<string> variableNames)
        {
            var variables = GetRandomVariablesByName(variableNames);
            return CalculateProbabilityDistribution(variables);
        }

        /// <summary>
        /// Calculate the complete probability distribution of the given random variables
        /// e.g. for variables A,B: P(a,b), P(a,!b), P(!a,b) and P(!a,!b).
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last to first</returns>
        public IList<Probability> CalculateProbabilityDistribution(IList<RandomVariable> variables)
        {
            var naNCounts = 0;
            var missingVariables = _randomVariables.Except(variables).ToList();
            var allVariablesInGivenOrder = new List<RandomVariable>();
            allVariablesInGivenOrder.AddRange(variables);
            allVariablesInGivenOrder.AddRange(missingVariables);
            var missingCount = 1 << missingVariables.Count;
            var instantiationCount = 1 << variables.Count;

            var probs = new Probability[instantiationCount];

            // calculate probability of current instantiations
            for (var i = 0; i < instantiationCount; i++)
            {
                var currentProb = Probability.Zero;
                var instantiationBits = Convert.ToString(i, 2).PadLeft(variables.Count, '0');

                for (var j = 0; j < missingCount; j++)
                {
                    var realBits = new char[_randomVariables.Count];
                    var missingBits = Convert.ToString(j, 2).PadLeft(missingVariables.Count, '0');
                    var allBits = instantiationBits + missingBits;
                    for (var currentVarIndex = 0; currentVarIndex < _randomVariables.Count; currentVarIndex++)
                    {
                        var realIndex = _randomVariables.IndexOf(allVariablesInGivenOrder[currentVarIndex]);
                        realBits[realIndex] = allBits[currentVarIndex];
                    }
                    var instanceProb = CalculateJointProbabilityInstance(new string(realBits));
                    if (!double.IsNaN(instanceProb.Value))
                    {
                        currentProb += instanceProb;
                    }
                    else
                    {
                        naNCounts++;
                    }
                    
                }
                probs[i] = currentProb;
            }
            if (naNCounts > 0)
            {
                Console.Out.WriteLine($"Warning: Dicarded {naNCounts} NaN results.");
            }
            return probs;
        }

        /// <summary>
        /// Calculate the conditional probability distribution of a random variable A given condition variables B1,...,Bn
        /// e.g. P(A|B,C) as [ P(a|b,c), P(a|b,!c), P(a|!b,c) P(a|!b!c), ...]
        /// RandomVariables will be identified by name.
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last condition to first and to the variable</returns>
        public IList<Probability> CalculateConditionalProbabilityDistribution(string randomVariableName, IList<string> conditionNames)
        {
            var randomVariable = GetRandomVariableByName(randomVariableName);
            var conditions = GetRandomVariablesByName(conditionNames);
            return CalculateConditionalProbabilityDistribution(randomVariable, conditions);
        }

        /// <summary>
        /// Calculate the conditional probability distribution of random variables A1,...,Am given condition variables B1,...,Bn
        /// e.g. P(A|B,C) as [ P(a|b,c), P(a|b,!c), P(a|!b,c) P(a|!b!c), ...]
        /// RandomVariables will be identified by name.
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last condition to first and to the variable</returns>
        public IList<Probability> CalculateConditionalProbabilityDistribution(IList<string> randomVariableNames, IList<string> conditionNames)
        {
            var randomVariable = GetRandomVariablesByName(randomVariableNames);
            var conditions = GetRandomVariablesByName(conditionNames);
            return CalculateConditionalProbabilityDistribution(randomVariable, conditions);
        }

        /// <summary>
        /// Calculate the conditional probability distribution of a random variable A given condition variables B1,...,Bn
        /// e.g. P(A|B,C) as [ P(a|b,c), P(a|b,!c), P(a|!b,c) P(a|!b!c), ...]
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last condition to first and to the variable</returns>
        public IList<Probability> CalculateConditionalProbabilityDistribution(RandomVariable randomVariable, IList<RandomVariable> conditions)
        {
            return CalculateConditionalProbabilityDistribution(new List<RandomVariable> {randomVariable}, conditions );
        }

        /// <summary>
        /// Calculate the conditional probability distribution of random variables A1,...,Am given condition variables B1,...,Bn
        /// e.g. P(A|B,C) as [ P(a|b,c), P(a|b,!c), P(a|!b,c) P(a|!b!c), ...]
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last condition to first and to the variable</returns>
        public IList<Probability> CalculateConditionalProbabilityDistribution(IList<RandomVariable> randomVariables, IList<RandomVariable> conditions)
        {
            var jointResults = CalculateProbabilityDistribution(randomVariables.Union(conditions).ToList());
            // if the condition set is empty, return the distribution of the random variable
            if (conditions.Count == 0)
            {
                return jointResults;
            }

            var matrixSize = jointResults.Count;
            var probResults = new Probability[matrixSize];
            var conditionResults = CalculateProbabilityDistribution(conditions);
            // iterate again over the binary indices,
            // from left to right, 0 at position i means the ith random variable is positive, 1 means negative
            for (var i = 0; i < matrixSize; i++)
            {
                var indexAsBits = Convert.ToString(i, 2).PadLeft(randomVariables.Count + conditions.Count, '0');
                // remove the first bit for only iterating the conditions
                var condIndex = Convert.ToInt32(indexAsBits.Remove(0, randomVariables.Count), 2);

                var curJointProb = jointResults[i];
                var curCondProb = conditionResults[condIndex];
                // if the condition is impossible, the conditional probability does not exist, so handle it as NaN
                // set it manually to be sure it does not become Infinity or the like
                if (curCondProb.Is(0.0, _tolerance))
                {
                    probResults[i] = new Probability(double.NaN);
                }
                else
                {
                    probResults[i] = curJointProb / curCondProb;
                }
            }

            return probResults;
        }

        /// <summary>
        /// Calculate the joint probability distribution value of the random variables. The instance is given by binary representation.
        /// </summary>
        private Probability CalculateJointProbabilityInstance(string randomVariableBits)
        {
            var currentProb = Probability.One;
            foreach (var distribution in _network.Distributions.Values)
            {
                var currentDistributionBits = new char[distribution.Conditions.Count + 1];
                currentDistributionBits[0] = randomVariableBits[_randomVariables.IndexOf(distribution.RandomVariable)];
                for (var conditionIndex = 0; conditionIndex < distribution.Conditions.Count; conditionIndex++)
                {
                    currentDistributionBits[conditionIndex + 1] =
                        randomVariableBits[_randomVariables.IndexOf(distribution.Conditions[conditionIndex])];
                }

                var distributionInstantiationIndex = Convert.ToInt32(new string(currentDistributionBits), 2);
                currentProb *= distribution.Distribution[distributionInstantiationIndex];
            }
            return currentProb;
        }

        private RandomVariable GetRandomVariableByName(string name)
        {
            return _randomVariables.First(rvar => rvar.Name == name);
        }

        private IList<RandomVariable> GetRandomVariablesByName(IEnumerable<string> names)
        {
            return names.Select(GetRandomVariableByName).ToList();
        } 
    }
}