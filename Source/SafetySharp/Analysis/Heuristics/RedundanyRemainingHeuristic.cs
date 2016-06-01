namespace SafetySharp.Analysis.Heuristics
{
    using System.Collections.Generic;
    using System.Linq;
    using Modeling;
    using Runtime;

    public class RedundanyRemainingHeuristic : IFaultSetHeuristic
    {
        private readonly IEnumerable<Fault>[] faultGroups;
        private readonly Fault[] allFaults;
        private readonly ISet<FaultSet> noSuggestions = new HashSet<FaultSet>();

        public RedundanyRemainingHeuristic(ModelBase model, params IEnumerable<Fault>[] faultGroups)
        {
            this.faultGroups = faultGroups;
            allFaults = model.Faults;
        }

        public ISet<FaultSet> MakeSuggestions(ISet<FaultSet> setsToCheck)
        {
            // only make suggestions at the beginning
            if (setsToCheck.Count != 1 || !setsToCheck.First().IsEmpty)
                return noSuggestions;

            var suggestions = new HashSet<FaultSet>();
            var allFaults = new FaultSet(this.allFaults);

            foreach (var excludedFaults in CartesianProduct(faultGroups))
            {
                var subsuming = new FaultSet(excludedFaults.ToArray()).SubsumingFaults();
                suggestions.Add(allFaults.GetDifference(subsuming));
            }

            return suggestions;
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
