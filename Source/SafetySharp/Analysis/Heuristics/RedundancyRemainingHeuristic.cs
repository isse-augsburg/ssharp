namespace SafetySharp.Analysis.Heuristics
{
    using System.Collections.Generic;
    using System.Linq;
    using Modeling;
    using Runtime;

    public class RedundancyRemainingHeuristic : IFaultSetHeuristic
    {
        private readonly Fault[] allFaults;
        private readonly IEnumerable<IEnumerable<Fault>> faultCombinations;
        private readonly int minSetSize;

        private readonly List<FaultSet> suggestions = new List<FaultSet>();

        const double minFaultSetSizeRelative = 1.0 / 2;

        public RedundancyRemainingHeuristic(ModelBase model, params IEnumerable<Fault>[] faultGroups)
            : this(model, minFaultSetSizeRelative, faultGroups) { }

        public RedundancyRemainingHeuristic(ModelBase model, double minSetSizeRelative, params IEnumerable<Fault>[] faultGroups)
            : this(model, (int)(model.Faults.Length * minSetSizeRelative), faultGroups) { }

        public RedundancyRemainingHeuristic(ModelBase model, int minSetSize, params IEnumerable<Fault>[] faultGroups)
        {
            allFaults = model.Faults;
            faultCombinations = CartesianProduct(faultGroups);
            this.minSetSize = minSetSize;
            CollectSuggestions();
        }

        public void Augment(List<FaultSet> setsToCheck)
        {
            setsToCheck.InsertRange(0, suggestions);
        }

        public void Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
        {
            bool isSuggestion = suggestions.Remove(checkedSet);

            if (isSuggestion && !isSafe)
            {
                // if set was critical, try some more redundancy
                var sets = from excludedFaults in faultCombinations
                           let excludedSet = Fault.SubsumingFaults(excludedFaults, allFaults)
                           // disable more faults:
                           let moreRedundancy = checkedSet.GetDifference(excludedSet)
                           // don't check checkedSet again, stop when minSetSize reached:
                           where moreRedundancy != checkedSet && moreRedundancy.Cardinality >= minSetSize
                           // check larger sets first:
                           //orderby moreRedundancy.Cardinality descending
                           //orderby moreRedundancy.Cardinality
                           select moreRedundancy;
                setsToCheck.InsertRange(0, sets);
                suggestions.AddRange(sets);
            }
        }

        public void CollectSuggestions()
        {
            var faults = new FaultSet(allFaults);

            foreach (var excludedFaults in faultCombinations)
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
