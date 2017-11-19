using System;
using System.Collections.Generic;

namespace SafetySharp.Bayesian
{
    using System.Linq;
    using ISSE.SafetyChecking.Modeling;

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

        public Probability CalculateProbability(ICollection<string> positiveNames, ICollection<string> negativeNames)
        {
            var positive = positiveNames.Select(name => GetRandomVariableByName(name)).ToList();
            var negative = negativeNames.Select(name => GetRandomVariableByName(name)).ToList();
            return CalculateProbability(positive, negative);
        }

        public Probability CalculateProbability(ICollection<RandomVariable> positive, ICollection<RandomVariable> negative)
        {
            var distribution = CalculateProbabilityDistribution(positive.Union(negative).ToList());
            var bitRepresentation = new string('0', positive.Count) + new string('1', negative.Count);
            var index = Convert.ToInt32(bitRepresentation, 2);
            return distribution[index];
        }

        public IList<Probability> CalculateProbabilityDistribution(IList<string> variableNames)
        {
            var variables = variableNames.Select(GetRandomVariableByName).ToList();
            return CalculateProbabilityDistribution(variables);
        }

        public IList<Probability> CalculateProbabilityDistribution(IList<RandomVariable> variables)
        {
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
                    currentProb += CalculateJointProbabilityInstance(new string(realBits));
                }
                probs[i] = currentProb;
            }

            return probs;
        }

        public IList<Probability> CalculateConditionalProbabilityDistribution(string randomVariableName, IList<string> conditionNames)
        {
            var randomVariable = GetRandomVariableByName(randomVariableName);
            var conditions = conditionNames.Select(GetRandomVariableByName).ToList();
            return CalculateConditionalProbabilityDistribution(randomVariable, conditions);
        }

        public IList<Probability> CalculateConditionalProbabilityDistribution(RandomVariable randomVariable, IList<RandomVariable> conditions)
        {
            var matrixSize = 1 << (conditions.Count + 1);
            var probResults = new Probability[matrixSize];
            var jointResults = CalculateProbabilityDistribution(new List<RandomVariable> { randomVariable }.Union(conditions).ToList());

            // if the condition set is empty, return the distribution of the random variable
            if (conditions.Count == 0)
            {
                return jointResults;
            }

            var conditionResults = CalculateProbabilityDistribution(conditions);
            // iterate again over the binary indices,
            // from left to right, 0 at position i means the ith random variable is positive, 1 means negative
            for (var i = 0; i < matrixSize; i++)
            {
                var indexAsBits = Convert.ToString(i, 2).PadLeft(conditions.Count + 1, '0');
                // remove the first bit for only iterating the conditions
                var condIndex = Convert.ToInt32(indexAsBits.Remove(0, 1), 2);

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
    }
}