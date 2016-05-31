namespace SafetySharp.Analysis.Heuristics
{
    using System.Collections.Generic;
    using System.Linq;
    using Modeling;
    using Runtime;

    public class SubsumptionHeuristic : IFaultSetHeuristic
    {
        public ISet<FaultSet> MakeSuggestions(ISet<FaultSet> setsToCheck)
        {
            return new HashSet<FaultSet>(
                from set in setsToCheck
                let subsumed = set.SubsumedFaults()
                where !set.Equals(subsumed)
                select subsumed
            );
        }
    }
}
