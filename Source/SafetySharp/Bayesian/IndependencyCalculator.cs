namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using ISSE.SafetyChecking.Modeling;

    /// <summary>
    /// Class for identifying as many conditional independencies in a model as possible.
    /// </summary>
    class IndependencyCalculator
    {
        private readonly double _tolerance;

        private IList<FaultRandomVariable> _faults;
        private ISet<McsRandomVariable> _minimalCriticalSets;
        private IList<BooleanRandomVariable> _states;
        private BooleanRandomVariable _hazard;
        private DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>> _conditionalIndependencies;
        private readonly IProbabilityDistributionCalculator _probCalculator;

        /// <summary>
        /// Creates a new IndependencyCalculator
        /// </summary>
        /// <param name="probCalculator">Provider for probability distributions</param>
        /// <param name="tolerance">Tolerance level for double calculations</param>
        public IndependencyCalculator(IProbabilityDistributionCalculator probCalculator, double tolerance)
        {
            _probCalculator = probCalculator;
            _tolerance = tolerance;
        }

        /// <summary>
        /// Identifies as many conditional independencies in the model as possible.
        /// </summary>
        public DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>> FindIndependencies(IList<FaultRandomVariable> faults, ICollection<McsRandomVariable> minimalCriticalSets, IList<BooleanRandomVariable> states, BooleanRandomVariable hazard)
        {
            _hazard = hazard;
            _faults = faults;
            _minimalCriticalSets = new HashSet<McsRandomVariable>(minimalCriticalSets);
            _states = states;
            _conditionalIndependencies = new DualKeyDictionary<RandomVariable, ICollection<ISet<RandomVariable>>>();

            var variablesForCalculation = GetVariablesForCalculation();
            CalculatePairIndependencies(variablesForCalculation);

            if (BayesianNetworkCreator.Config.UseDccaResultsForLearning && _minimalCriticalSets.Count > 0)
            {
                GenerateDccaIndependencies();
                CalculateFurtherDccaIndependencies();
            }

            return _conditionalIndependencies;
        }

        /// <summary>
        /// Gets the variables whose independencies are identified by exhaustive use of the mathematical definition of independency and probability distribution calculation
        /// </summary>
        private IList<RandomVariable> GetVariablesForCalculation()
        {
            var variables = new List<RandomVariable>();
            variables.AddRange(_faults);
            variables.AddRange(_states);
            if (!BayesianNetworkCreator.Config.UseDccaResultsForLearning)
            {
                variables.AddRange(_minimalCriticalSets);
                variables.Add(_hazard);
            }
            if (BayesianNetworkCreator.Config.UseDccaResultsForLearning && _minimalCriticalSets.Count == 0)
            {
                variables.Add(_hazard);
            }
            return variables;
        }

        /// <summary>
        /// Identifies the conditional independencies between the given variables by exhaustive use of the mathematical definition of independency and probability distribution calculation
        /// </summary>
        private void CalculatePairIndependencies(IList<RandomVariable> variablesToUse)
        {
            for (var i = 0; i < variablesToUse.Count; i++)
            {
                for (var j = i + 1; j < variablesToUse.Count; j++)
                {
                    var first = variablesToUse[i];
                    var second = variablesToUse[j];
                    if (IsIndependent(first, second))
                    {
                        Console.Out.WriteLine($"Variables {first.Name} and {second.Name} are independent");
                        AddIndependency(first, second);
                    }
                    for (var k = 1; k < variablesToUse.Count - 1 && k <= BayesianNetworkCreator.Config.MaxConditionSize; k++)
                    {
                        var subsets =
                            SubsetUtils.AllSubsets(
                                Enumerable.Range(0, variablesToUse.Count).Except(new[] { i, j })
                                          .ToList(), k);
                        foreach (var indexSubset in subsets)
                        {
                            var curVars = indexSubset.Select(index => variablesToUse[index]).ToList();
                            if (IsConditionalIndependent(first, second, new List<RandomVariable>(curVars)))
                            {
                                Console.Out.WriteLine(
                                    $"Variables {first.Name} and {second.Name} are independent given {string.Join(", ", curVars)}");
                                AddIndependency(first, second, curVars);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates conditional independencies that can be derived from the DCCA result
        /// </summary>
        private void GenerateDccaIndependencies()
        {
            foreach (var mcs in _minimalCriticalSets)
            {
                var otherFaults = _faults.Where(fault => !mcs.Reference.Faults.Contains(fault.Reference)).ToList();
                var mcsFaults = mcs.FaultVariables;

                // the hazard is independent to all faults that are part of a critical set given all minimal critical sets
                foreach (var mcsFault in mcsFaults)
                {
                    AddIndependency(_hazard, mcsFault, _minimalCriticalSets);
                }
                // the minimal critical set is independent of all other faults and states given its own faults
                foreach (var otherFault in otherFaults)
                {
                    AddIndependency(mcs, otherFault, mcsFaults);
                }
                foreach (var state in _states)
                {
                    AddIndependency(mcs, state, mcsFaults);
                }

                // the minimal critical set is independent of all other minimal critical sets given the faults of both critical sets
                foreach (var otherMcs in _minimalCriticalSets.Where(m => m != mcs))
                {
                    AddIndependency(mcs, otherMcs, mcsFaults.Union(otherMcs.FaultVariables).ToList());
                }
            }
        }

        private void CalculateFurtherDccaIndependencies()
        {
            // let A be a fault that is not part of a minimal critical set or a state
            // check for every A:
            //      is A independent of the hazard given the minimal critical sets
            //      is A independent of the hazard given all subsets of other A's like this one
            var allFaultsInCriticalSets = _minimalCriticalSets.SelectMany(mcs => mcs.FaultVariables).ToList();
            var faultsInNoMcsAndStates = new List<RandomVariable>(_faults.Except(allFaultsInCriticalSets)).ToList();
            faultsInNoMcsAndStates.AddRange(_states);
            var i = 0;
            foreach (var faultOrState in faultsInNoMcsAndStates)
            {
                if (IsIndependent(_hazard, faultOrState))
                {
                    Console.Out.WriteLine(
                         $"Variables {_hazard.Name} and {faultOrState.Name} are independent");
                    AddIndependency(_hazard, faultOrState);
                }
                if (IsConditionalIndependent(_hazard, faultOrState, new List<RandomVariable>(_minimalCriticalSets)))
                {
                    Console.Out.WriteLine(
                         $"Variables {_hazard.Name} and {faultOrState.Name} are independent given {string.Join(", ", _minimalCriticalSets)}");
                    AddIndependency(_hazard, faultOrState, _minimalCriticalSets);
                }

                for (var k = 1; k < faultsInNoMcsAndStates.Count - 1 && k <= BayesianNetworkCreator.Config.MaxConditionSize; k++)
                {
                    var subsets =
                        SubsetUtils.AllSubsets(
                            Enumerable.Range(0, faultsInNoMcsAndStates.Count).Except(new[] { i })
                                      .ToList(), k);
                    foreach (var indexSubset in subsets)
                    {
                        var curVars = indexSubset.Select(index => faultsInNoMcsAndStates[index]).ToList();
                        if (IsConditionalIndependent(_hazard, faultOrState, new List<RandomVariable>(curVars)))
                        {
                            Console.Out.WriteLine(
                                $"Variables {_hazard.Name} and {faultOrState.Name} are independent given {string.Join(", ", curVars)}");
                            AddIndependency(_hazard, faultOrState, curVars);
                        }
                    }
                }
                i++;
            }
        }

        private bool IsIndependent(RandomVariable first, RandomVariable second)
        {
            var probDistribution = _probCalculator.CalculateProbabilityDistribution(new[] { first, second });
            Console.Out.WriteLine($"Variables {first.Name} and {second.Name} distribution: {string.Join(",", probDistribution)}");
            GC.Collect();

            return AreValuesIndependent(first.TrueProbability, second.TrueProbability, probDistribution[0])
                   && AreValuesIndependent(first.TrueProbability, second.TrueProbability.Complement(), probDistribution[1])
                   && AreValuesIndependent(first.TrueProbability.Complement(), second.TrueProbability, probDistribution[2])
                   && AreValuesIndependent(first.TrueProbability.Complement(), second.TrueProbability.Complement(), probDistribution[3]);
        }

        private bool AreValuesIndependent(Probability p1, Probability p2, Probability both)
        {
            return Math.Abs(p1.Value * p2.Value - both.Value) < _tolerance;
        }

        private bool IsConditionalIndependent(RandomVariable first, RandomVariable second, IList<RandomVariable> conditions)
        {
            var firstGivenConditions = _probCalculator.CalculateConditionalProbabilityDistribution(first, conditions);
            var secondGivenConditions = _probCalculator.CalculateConditionalProbabilityDistribution(second, conditions);
            var firstGivenSecondAndConditions = _probCalculator.CalculateConditionalProbabilityDistribution(first,
                new List<RandomVariable> { second }.Union(conditions).ToList());
            for (var i = 0; i < 1 << (conditions.Count + 2); i++)
            {
                var indexAsBits = Convert.ToString(i, 2).PadLeft(conditions.Count + 2, '0');
                // for the binary representation of the index abcd..., remove b and use acd... as index
                var indexWithoutSecond = Convert.ToInt32(indexAsBits.Remove(1, 1), 2);
                // for the binary representation of the index abcd..., remove a and use bcd... as index
                var indexWithoutFirst = Convert.ToInt32(indexAsBits.Remove(0, 1), 2);

                if (!AreValuesConditionalIndependent(firstGivenConditions[indexWithoutSecond], secondGivenConditions[indexWithoutFirst],
                        firstGivenSecondAndConditions[i]))
                {
                    return false;
                }
            }
            return true;
        }

        private bool AreValuesConditionalIndependent(Probability firstGivenConditions, Probability secondGivenConditions,
                                                   Probability firstGivenSecondAndConditions)
        {
            return firstGivenConditions.Is(0.0, _tolerance) || secondGivenConditions.Is(0.0, _tolerance) ||
                   firstGivenSecondAndConditions.Is(firstGivenConditions.Value, _tolerance);
        }

        private void AddIndependency(RandomVariable first, RandomVariable second, IEnumerable<RandomVariable> conditions = null)
        {
            if (!_conditionalIndependencies.ContainsKey(first, second))
            {
                _conditionalIndependencies[first, second] = new List<ISet<RandomVariable>>();
            }
            if (conditions != null)
            {
                _conditionalIndependencies[first, second].Add(new HashSet<RandomVariable>(conditions));
            }
        }

        public void PrettyPrintIndependencies()
        {
            Console.Out.WriteLine("Independencies: {");
            foreach (var tuple in _conditionalIndependencies)
            {
                Console.Out.WriteLine(PrettyPrintIndependency(tuple));
            }
            Console.Out.WriteLine("}");
        }

        private string PrettyPrintIndependency(Tuple<RandomVariable, RandomVariable> tuple)
        {
            var builder = new StringBuilder();
            builder.Append("[");
            builder.Append(tuple.Item1.Name);
            builder.Append(" independent of ");
            builder.Append(tuple.Item2.Name);
            if (_conditionalIndependencies[tuple].Count > 0)
            {
                builder.Append(" given ");
            }
            foreach (var rvSet in _conditionalIndependencies[tuple])
            {
                builder.Append($"{{{string.Join(",", rvSet.Select(rv => rv.Name))}}}");
            }
            return builder.ToString();
        }

    }
}
