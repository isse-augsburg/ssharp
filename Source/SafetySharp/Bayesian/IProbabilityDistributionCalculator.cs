using System.Collections.Generic;
using ISSE.SafetyChecking.Modeling;

namespace SafetySharp.Bayesian
{
    /// <summary>
    /// Interface for calculating arbitrary probability distributions on a given set of random variables
    /// </summary>
    public interface IProbabilityDistributionCalculator
    {
        Probability CalculateProbability(ICollection<RandomVariable> positive, ICollection<RandomVariable> negative);
        IList<Probability> CalculateProbabilityDistribution(IList<RandomVariable> variables);
        IList<Probability> CalculateConditionalProbabilityDistribution(RandomVariable randomVariable, IList<RandomVariable> conditions);
        IList<Probability> CalculateConditionalProbabilityDistribution(IList<RandomVariable> randomVariables, IList<RandomVariable> conditions);
    }
}