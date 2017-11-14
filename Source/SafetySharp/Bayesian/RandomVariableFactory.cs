namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Analysis;
    using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
    using ISSE.SafetyChecking.Modeling;
    using ModelChecking;
    using Modeling;

    /// <summary>
    /// Class for creating various types of RandomVariables
    /// </summary>
    public class RandomVariableFactory
    {
        private readonly ModelBase _model;

        public RandomVariableFactory(ModelBase model)
        {
            _model = model;
        }

        /// <summary>
        /// Returns a new BooleanRandomVariable that represents the given state expression
        /// </summary>
        public BooleanRandomVariable FromState(Func<bool> state, string name)
        {
            return new BooleanRandomVariable(state, name);
        }

        /// <summary>
        /// Returns a new FaultRandomVariable that represents the given fault
        /// </summary>
        public FaultRandomVariable FromFault(Fault fault)
        {
            return new FaultRandomVariable(fault, fault.Name);
        }

        /// <summary>
        /// Returns a list of FaultRandomVariables that represent the given faults 
        /// </summary>
        public IList<FaultRandomVariable> FromFaults(IList<Fault> faults)
        {
            return faults.Select(FromFault).ToList();
        }

        /// <summary>
        /// Executes a DCCA and returns a list of McsRandomVariables that represent the DCCA results limited by the given faults.
        /// A minimal critical set which contains faults that are not in the given list of faults will be ignored.
        /// An empty minimal critical set will not be created as a random variable because it would not make sense regarding the probability theory.
        /// </summary>
        public IList<McsRandomVariable> FromDccaLimitedByFaults(Func<bool> hazard, ICollection<FaultRandomVariable> faults)
        {
            var analysis = new SafetySharpSafetyAnalysis
            {
                Backend = SafetyAnalysisBackend.FaultOptimizedOnTheFly,
                Heuristics = { new MaximalSafeSetHeuristic(_model.Faults) }
            };
            var result = analysis.ComputeMinimalCriticalSets(_model, new ExecutableStateFormula(hazard));
            var mcsVariables = new List<McsRandomVariable>();
            // Create a random variable for every nonempty critical set
            foreach (var criticalSet in result.MinimalCriticalSets.Where(set => set.Count > 0))
            {
                var mcs = new MinimalCriticalSet(criticalSet);
                var mcsName = $"mcs_{string.Join("_", mcs.Faults.Select(fv => fv.Name))}";
                var faultVariables = faults.Where(fv => mcs.Faults.Contains(fv.Reference)).ToList();
                mcsVariables.Add(new McsRandomVariable(mcs, faultVariables, mcsName));
            }
            GC.Collect();

            return mcsVariables.Where(
                    criticalSet => criticalSet.FaultVariables.Count == criticalSet.Reference.Faults.Count
                ).ToList();
        }
    }
}