namespace SafetySharp.Analysis.Heuristics
{
    using System.Collections.Generic;
    using System.Linq;
    using Modeling;
    using Runtime;

    public class SubsumptionHeuristic : IFaultSetHeuristic
    {
        private Fault[] allFaults;

        public SubsumptionHeuristic(ModelBase model)
        {
	        allFaults = model.Faults;
        }

        public ISet<FaultSet> MakeSuggestions(ISet<FaultSet> setsToCheck)
        {
            return new HashSet<FaultSet>(
                from set in setsToCheck
                let subsumed = Fault.SubsumedFaults(set, allFaults)
                where !set.Equals(subsumed)
                select subsumed
            );
        }
    }
}
