namespace SafetySharp.Analysis.Heuristics
{
    using System.Collections.Generic;
    using System.Linq;
    using Modeling;
    using Runtime;

    public class RedundancyRemainingHeuristic : IFaultSetHeuristic
    {
        private readonly IEnumerable<Fault>[] faultGroups;
        private readonly Fault[] allFaults;
        private readonly ISet<FaultSet> noSuggestions = new HashSet<FaultSet>();

        private readonly List<FaultSet> suggestions = new List<FaultSet>();

        public RedundancyRemainingHeuristic(ModelBase model, params IEnumerable<Fault>[] faultGroups)
        {
            this.faultGroups = faultGroups;
            allFaults = model.Faults;
            CollectSuggestions();
        }

        public void Augment(List<FaultSet> setsToCheck)
        {
            setsToCheck.InsertRange(0, suggestions);
        }

        public void Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
        {
            // TODO: if !isSafe, try more redundancy
            // if several critical, try more redundancy FIRST
            suggestions.Remove(checkedSet);
        }

        public void CollectSuggestions()
        {
            var faults = new FaultSet(allFaults);

            foreach (var excludedFaults in CartesianProduct(faultGroups))
            {
                var subsuming = Fault.SubsumingFaults(excludedFaults, allFaults);
                suggestions.Add(faults.GetDifference(subsuming));
            }
        }

        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(
              emptyProduct,
              (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item }));
        }
    }
}
