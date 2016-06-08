namespace SafetySharp.Analysis.Heuristics
{
    using System.Collections.Generic;
    using Modeling;
    using Runtime;

    public class SubsumptionHeuristic : IFaultSetHeuristic
    {
        private Fault[] allFaults;

        private readonly HashSet<FaultSet> subsumedSets = new HashSet<FaultSet>();

        private int successCounter;

        public SubsumptionHeuristic(ModelBase model)
        {
	        allFaults = model.Faults;
        }

        public void Augment(List<FaultSet> setsToCheck)
        {
            // for each set, check the set of subsumed faults first
            for (int i = 0; i < setsToCheck.Count; ++i)
            {
                var subsumed = Fault.SubsumedFaults(setsToCheck[i], allFaults);
                if (!setsToCheck[i].Equals(subsumed) && !subsumedSets.Contains(subsumed))
                {
	                setsToCheck.Insert(i, subsumed);
	                subsumedSets.Add(subsumed);
	                i++;
                }
            }
        }

        public void Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
        {
            if (!subsumedSets.Contains(checkedSet))
                return;

            int delta = isSafe ? 1 : -1;
            successCounter += delta;

            if (successCounter != 0)
                return;

            // if the subsumed sets are critical more often than they are not,
            // check the "normal" sets first.
            for (int i = 0; i < setsToCheck.Count; ++i)
            {
                var set = setsToCheck[i];
                if (subsumedSets.Contains(set))
                {
                    setsToCheck.RemoveAt(i);
                    setsToCheck.Insert(i - delta, set);
                }
            }
        }
    }
}
