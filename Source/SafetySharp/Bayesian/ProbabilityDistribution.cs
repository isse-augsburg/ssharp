namespace SafetySharp.Bayesian
{
    using System;
    using System.Linq;
    using System.Text;
    using ISSE.SafetyChecking.Modeling;
    using System.Collections.Generic;

    /// <summary>
    /// Class for probability distributions. Supports distributions P(A) for a single variable A and 
    /// conditional probability distributions P(A|B,C,...) for a variable A and arbitrary variables B,C,...
    /// Distributions are stored as a List of Probabilities. The iteration order is from right to left, so
    /// at index 0 would be P(a|b,c), index 1 P(a|b,!c), index 2 P(a|!b,c) and so on.
    /// </summary>
    public class ProbabilityDistribution
    {
        public RandomVariable RandomVariable { get; }
        public IList<RandomVariable> Conditions { get; }

        public IList<Probability> Distribution { get; }

        public ProbabilityDistribution(RandomVariable randomVariable, IList<RandomVariable> conditions, IList<Probability> distribution)
        {
            RandomVariable = randomVariable;
            Conditions = conditions;
            Distribution = distribution;
        }

        /// <summary>
        /// Returns the distribution in a more intuitive way by iterating from left to right instead from right to left.
        /// E.g. for random variables A, B it will return an array storing the values in the order a|b, !a|b, a|!b, !a|!b
        /// </summary>
        private IList<Probability> MoreReadableDistribution()
        {
            var matrix = new Probability[Distribution.Count];
            for (var index = 0; index < Distribution.Count; index++)
            {
                var indexBits = Convert.ToString(index, 2).PadLeft(Conditions.Count + 1, '0');
                var newIndex = Convert.ToInt32(new string(indexBits.Reverse().ToArray()), 2);
                matrix[newIndex] = Distribution[index];
            }
            return matrix;
        }

        /// <summary>
        /// Formats the distribution in a readable way
        /// </summary>
        public override string ToString()
        {
            return FormatAsString(Distribution, false);
        }

        /// <summary>
        /// Same as ToString, but formats the Distribution in a more intuitive way
        /// </summary>
        public string ToMoreReadableString()
        {
            return FormatAsString(MoreReadableDistribution(), true);
        }

        /// <summary>
        /// Formats the given distribution in a readable way. 
        /// </summary>
        /// <param name="distribution">Distribution to be represented as string</param>
        /// <param name="reverseIndex">If true the distribution will be iterated from right to left, otherwise from left to right</param>
        private string FormatAsString(IList<Probability> distribution, bool reverseIndex)
        {
            var builder = new StringBuilder();
            builder.Append($"Probability distribution of {RandomVariable.Name}");
            if (Conditions.Count > 0)
            {
                builder.Append($" | {string.Join(",", Conditions.Select(rv => rv.Name))}");
            }
            builder.Append("\n");
            for (var index = 0; index < distribution.Count; index++)
            {
                var indexBits = Convert.ToString(index, 2).PadLeft(Conditions.Count + 1, '0');
                if (reverseIndex)
                    indexBits = new string(indexBits.Reverse().ToArray());
                if (indexBits[0] == '1')
                    builder.Append("!");
                else
                    builder.Append(" ");
                builder.Append($"{RandomVariable.Name}");
                if (Conditions.Count > 0)
                    builder.Append(" | ");
                for (var j = 1; j < indexBits.Length; j++)
                {
                    if (indexBits[j] == '1')
                        builder.Append("!");
                    else
                        builder.Append(" ");
                    builder.Append(Conditions[j - 1].Name);
                    if (j != indexBits.Length - 1)
                        builder.Append(",");
                }
                builder.Append($": {distribution[index]} \n");
            }
            return builder.ToString();
        }
    }
}