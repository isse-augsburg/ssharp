namespace SafetySharp.Modeling
{
    using System.Collections.Generic;
    using Runtime;

    public static class SubsumptionHelper
    {
        private static readonly IDictionary<Fault[], Fault[]> unprocessedSubsumptions = new Dictionary<Fault[], Fault[]>();

        private static readonly IDictionary<FaultSet, FaultSet> subsumptions = new Dictionary<FaultSet, FaultSet>();

        public static void Subsumes(this Fault fault, params Fault[] subsumed)
        {
            new[] { fault }.Subsumes(subsumed);
        }

        public static void Subsumes(this Fault[] faults, params Fault[] subsumed)
        {
            unprocessedSubsumptions.Add(faults, subsumed);
        }

        internal static void ProcessSubsumptions()
        {
            foreach (var entry in unprocessedSubsumptions)
            {
                var key = new FaultSet(entry.Key);
                var value = new FaultSet(entry.Value);

                if (subsumptions.ContainsKey(key))
                    subsumptions[key] = subsumptions[key].GetUnion(value);
                else
                    subsumptions.Add(key, value);
            }

            unprocessedSubsumptions.Clear();
        }

        public static FaultSet SubsumedFaults(this FaultSet faults)
        {
            var subsumed = faults;

            uint oldCount;
            do // fixed-point iteration
            {
                oldCount = subsumed.Cardinality;

                foreach (var subsumption in subsumptions)
                {
                    if (subsumption.Key.IsSubsetOf(subsumed))
                        subsumed = subsumed.GetUnion(subsumption.Value);
                }
            } while (oldCount < subsumed.Cardinality);

            return subsumed;
        }
    }
}
