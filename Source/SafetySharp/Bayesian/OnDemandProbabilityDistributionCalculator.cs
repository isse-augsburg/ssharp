using System.Collections.Generic;

namespace SafetySharp.Bayesian
{
    using System;
    using System.IO;
    using System.Linq;
    using Analysis;
    using ISSE.SafetyChecking;
    using ISSE.SafetyChecking.Formula;
    using ISSE.SafetyChecking.Modeling;
    using Modeling;
    using Newtonsoft.Json;

    /// <summary>
    /// Class for calculating arbitrary probabilities and probability distributions on a given set of random variables via model checking.
    /// </summary>
    public class OnDemandProbabilityDistributionCalculator : IProbabilityDistributionCalculator
    {
        // _probs should only be accessed by the GetProbability method!
        // _probs contains the 'positive' distributions of subsets of the random variables
        // subsets of e.g. random variables a,b,c will be represented and stored at the respective indices like this:
        // 000: {}      001: {a} 
        // 010: {b}     011: {a,b}
        // 100 {c}      101: {a,c}
        // 110: {b,c}   111: {a,b,c}
        private readonly Dictionary<int, Probability> _probs;
        private readonly List<RandomVariable> _randomVariables;
        private readonly int _stepBounds;
        private readonly ModelBase _model;
        private readonly double _tolerance;
        private readonly BayesianLearningConfiguration _config;

        public OnDemandProbabilityDistributionCalculator(ModelBase model, ICollection<RandomVariable> variables, int stepBounds, double tolerance, BayesianLearningConfiguration? config = null)
        {
            _config = config ?? BayesianLearningConfiguration.Default;
            _randomVariables = variables.ToList();
            _probs = new Dictionary<int, Probability>();
            _stepBounds = stepBounds;
            _model = model;
            _tolerance = tolerance;

            if (!string.IsNullOrWhiteSpace(_config.ProbabilityDeserializationFilePath))
            {
                DeserializeProbabilities();
            }
        }

        /// <summary>
        /// Returns the probability for the given random variables. If it is not present yet, it is calculated first.
        /// All access to _probs should be through this method!
        /// </summary>
        /// <param name="variables">random variables for the distribution</param>
        /// <returns></returns>
        private Probability GetProbability(ICollection<RandomVariable> variables)
        {
            var index = SubsetUtils.GetIndex(variables, _randomVariables);
            if (!_probs.ContainsKey(index))
            {
                _probs[index] = CalculateProbability(variables.ToList());
                if (!string.IsNullOrWhiteSpace(_config.ProbabilitySerializationFilePath))
                {
                    SerializeCurrentProbabilities();
                }   
            }
            return _probs[index];
        }

        /// <summary>
        /// Calculates all probability distributions of all subsets of the random variables.
        /// Use with caution!
        /// </summary>
        public void CalculateTrueProbabilities()
        {
            // Iterate through all 2^n random variables
            for (var i = 1; i < (1 << _randomVariables.Count); i++)
            {
                var currentVars = new HashSet<RandomVariable>();
                for (var j = 0; j < _randomVariables.Count; j++)
                {
                    // is the jth random variable present in the current subset?
                    // it is, if there is a 1 at the jth bit from the right side
                    // so 'and' i and 2^j and test if the result is greater than zero
                    if ((i & (1 << j)) > 0)
                    {
                        currentVars.Add(_randomVariables[j]);
                    }
                }
                _probs[SubsetUtils.GetIndex(currentVars, _randomVariables)] = CalculateProbability(currentVars.ToList());
            }
            WriteProbsToConsole();
        }

        /// <summary>
        /// Calculate the probability distribution of the true values of all variables.
        /// If just one variable was given, it also sets the probability distribution in the variable
        /// </summary>
        private Probability CalculateProbability(IList<RandomVariable> variables)
        {
            Console.Out.WriteLine($"Modelchecking joint probability of: {string.Join(",", variables)}.");
            var tc = SafetySharpModelChecker.TraversalConfiguration;
            tc.MomentOfIndependentFaultActivation = MomentOfIndependentFaultActivation.OnFirstMethodWithoutUndo;
            tc.DefaultTraceOutput = TextWriter.Null;
            SafetySharpModelChecker.TraversalConfiguration = tc;

            var last = variables.Last().ToOnceFormula();
            for (var i = variables.Count - 2; i >= 0; i--)
            {
                last = new BinaryFormula(
                        variables[i].ToOnceFormula(),
                        BinaryOperator.And,
                        last
                    );
            }

            var probability = SafetySharpModelChecker.CalculateProbabilityToReachStateBounded(_model, last, _stepBounds);
            GC.Collect();
            if (variables.Count == 1)
            {
                variables[0].Probability = new[] { probability, probability.Complement() };
            }
            Console.Out.WriteLine($"Finished modelchecking. Result is: {probability}");
            return probability;
        }

        /*
         * Calculates the union with the inclusion/exclusion principle, but with one more indirection:
         * It calculates the probablity of (A u B u C), but A, B, C are just representation sets, 
         * because each consists of intersections of various random variables.
         * So e.g. for A = P(R1, R2), B = P(R2,R3,R4) and C = P(R1,R3,R5) it actually calculates:
         * A u B u C = P(R1,R2) u P(R2,R3,R4) u P(R1,R3,R5)
         */
        private Probability CalculateUnionOfIntersectionSets(IList<ISet<RandomVariable>> variables)
        {
            var sum = Probability.Zero;
            for (var k = 0; k < variables.Count; k++)
            {
                var innerSum = Probability.Zero;
                var currentIndices = SubsetUtils.AllSubsets(Enumerable.Range(0, variables.Count).ToList(), k + 1);
                foreach (var indexSubset in currentIndices)
                {
                    // get the current representation sets, e.g. A, B
                    var currentVariableSets = indexSubset.Select(index => variables[index]).ToList();
                    // get the set of the actual random variables, e.g. R1,R2,R3,R4
                    var currentVariables = currentVariableSets.SelectMany(x => x).Distinct().ToList();
                    innerSum += GetProbability(currentVariables);
                }
                if (k % 2 == 0)
                {
                    sum += innerSum;
                }
                else
                {
                    sum -= innerSum;
                }
            }
            return sum;
        }



        /// <summary>
        /// Calculates the probability of P(p1, ..., pi, n1, ..., nj)
        /// for p1,...,pi are all 'positive' values of binary random variables and n1,..,nj 'negative' ones.
        /// </summary>
        public Probability CalculateProbability(ICollection<RandomVariable> positive, ICollection<RandomVariable> negative)
        {
            // get P(p1,...,pi) or 1 if there are no positive variables
            var trueProb = positive.Count > 0 ? GetProbability(positive) : Probability.One;

            // calculate the union of all intersection sets consisting of all positive variables and one negative variable,
            // so P(p1,...,pi,n1) u P(p1,...,pi,n2) u ... u P(p1,...,pi,nj)
            var negVars = new List<ISet<RandomVariable>>();
            foreach (var negVar in negative)
            {
                var negSet = new HashSet<RandomVariable> { negVar };
                negSet.UnionWith(positive);
                negVars.Add(negSet);
            }
            var negProb = CalculateUnionOfIntersectionSets(negVars);

            // if the difference of trueProb and negProb is below the tolerance level, it is actually Zero
            var diff = trueProb - negProb;
            return diff.Is(0.0, _tolerance) ? Probability.Zero : diff;
        }

        /// <summary>
        /// Calculate the complete probability distribution of the given random variables
        /// e.g. for variables A,B: P(a,b), P(a,!b), P(!a,b) and P(!a,!b).
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last to first</returns>
        public IList<Probability> CalculateProbabilityDistribution(IList<RandomVariable> variables)
        {
            var matrixSize = 1 << variables.Count;
            var probResults = new Probability[matrixSize];
            // iterate again over the binary indices,
            // from left to right, 0 at position i means the ith random variable is positive, 1 means negative
            for (var i = 0; i < matrixSize; i++)
            {
                var pos = new List<RandomVariable>();
                var neg = new List<RandomVariable>();

                var indexAsBits = Convert.ToString(i, 2).PadLeft(variables.Count, '0');
                for (var j = 0; j < indexAsBits.Length; j++)
                {
                    if (indexAsBits[j] == '0')
                    {
                        pos.Add(variables[j]);
                    }
                    else if (indexAsBits[j] == '1')
                    {
                        neg.Add(variables[j]);
                    }
                }
                probResults[i] = CalculateProbability(pos, neg);
            }
            return probResults;
        }

        /// <summary>
        /// Calculate the conditional probability distribution of a random variable A given condition variables B1,...,Bn
        /// e.g. P(A|B,C) as [ P(a|b,c), P(a|b,!c), P(a|!b,c) P(a|!b!c), ...]
        /// </summary>
        /// <returns>A probability array of the distribution, the iteration order is last condition to first and to the variable</returns>
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

        public void WriteProbsToConsole()
        {
            foreach (var keyValuePair in _probs)
            {
                Console.Out.WriteLine($"{{{string.Join(",", SubsetUtils.FromIndex(_randomVariables, keyValuePair.Key).Select(v => v.Name))}}}: {keyValuePair.Value}");
            }
        }

        public int NumberOfCalculatedDistributions()
        {
            return _probs.Count;
        }

        public int NumberOfMaxDistributions()
        {
            return 1 << _randomVariables.Count;
        }

        private void SerializeCurrentProbabilities()
        {
            using (var file = File.CreateText(_config.ProbabilitySerializationFilePath))
            {
                var toSerialize = new ProbabilitySerialization
                {
                    RandomVariables = _randomVariables.Select(rvar => rvar.Name).ToArray(),
                    Probs = _probs
                };
                var serializer = new JsonSerializer();
                serializer.Serialize(file, toSerialize);
            }
        }

        private void DeserializeProbabilities()
        {
            using (var file = File.OpenText(_config.ProbabilityDeserializationFilePath))
            {
                var serializer = new JsonSerializer();
                var probs = (ProbabilitySerialization) serializer.Deserialize(file, typeof(ProbabilitySerialization));
                foreach (var keyValuePair in probs.Probs)
                {
                    var storedIndex = keyValuePair.Key;
                    var storedRandomVariables = SubsetUtils.FromIndex(probs.RandomVariables, storedIndex);
                    var realRandomVariables = _randomVariables.Where(rvar => storedRandomVariables.Contains(rvar.Name)).ToList();
                    _probs[SubsetUtils.GetIndex(realRandomVariables, _randomVariables)] = keyValuePair.Value;
                    if (realRandomVariables.Count == 1)
                    {
                        realRandomVariables[0].Probability = new[] { keyValuePair.Value, keyValuePair.Value.Complement() };
                    }
                }
            }
        }
    }

    public class ProbabilitySerialization
    {
        public string[] RandomVariables { get; set; }
        public Dictionary<int, Probability> Probs { get; set; }
    }
}