namespace SafetySharp.Analysis.Heuristics
{
    using System.Collections.Generic;
    using System.Linq;
    using Modeling;
    using Runtime;

    /// <summary>
    /// A heuristic that tries to determine the minimal redundancy necessary in the system
    /// so the hazard does not occur.
    /// </summary>
    public class MinimalRedundancyHeuristic : IFaultSetHeuristic
    {
        private readonly Fault[] allFaults;
        private readonly int minSetSize;

        private ISet<FaultSet> currentSuggestions;
        private ISet<FaultSet> nextSuggestions;

        private int successCounter;

        // how many faults are removed from critical sets in each step
        private int subsetStepSize = 1;

        const double defaultMinFaultSetSizeRelative = 0.5;

        public MinimalRedundancyHeuristic(ModelBase model, params IEnumerable<Fault>[] faultGroups)
            : this(model, defaultMinFaultSetSizeRelative, faultGroups) { }

        public MinimalRedundancyHeuristic(ModelBase model, double minSetSizeRelative, params IEnumerable<Fault>[] faultGroups)
            : this(model, (int)(model.Faults.Length * minSetSizeRelative), faultGroups) { }

        /// <summary>
        /// Creates a new instance of the heuristic.
        /// </summary>
        /// <param name="model">The model for which the heuristic is created.</param>
        /// <param name="minSetSize">The minimum size of fault sets to check.</param>
        /// <param name="faultGroups">Different groups of faults. Suggested fault sets never contain
        /// all faults of any group.</param>
        public MinimalRedundancyHeuristic(ModelBase model, int minSetSize, params IEnumerable<Fault>[] faultGroups)
        {
            allFaults = model.Faults;
            this.minSetSize = minSetSize;

            CollectSuggestions(faultGroups);
            nextSuggestions = GetSubsets(currentSuggestions);
        }

        void IFaultSetHeuristic.Augment(List<FaultSet> setsToCheck)
        {
            successCounter = 0;
            setsToCheck.AddRange(currentSuggestions);
        }

        void IFaultSetHeuristic.Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
        {
            bool isSuggestion = currentSuggestions.Remove(checkedSet);
            if (!isSuggestion)
                return;

            // subsets of a safe set are safe - do not check them again
            if (isSafe)
            {
                successCounter++;
                nextSuggestions.ExceptWith(GetSubsets(new[] { checkedSet }));
            }
            else
                successCounter--;

            int tolerance = allFaults.Length / 4;
            if (currentSuggestions.Count == 0 && checkedSet.Cardinality > minSetSize)
            {
                if (successCounter < -tolerance && isSafe)
                {
	                subsetStepSize++;
	                successCounter = 0;
                }

                currentSuggestions = nextSuggestions;
                nextSuggestions = GetSubsets(currentSuggestions);
            }
        }

        private void CollectSuggestions(IEnumerable<IEnumerable<Fault>> faultGroups)
        {
            var faults = new FaultSet(allFaults);

            currentSuggestions = new HashSet<FaultSet>(
                // one fault of each group is not activated (try all combinations)
                from excludedFaults in CartesianProduct(faultGroups)
                // also exclude subsuming faults
                let subsuming = Fault.SubsumingFaults(excludedFaults, allFaults)
                orderby subsuming.Cardinality descending
                select faults.GetDifference(subsuming)
            );
        }

        private ISet<FaultSet> GetSubsets(IEnumerable<FaultSet> sets)
        {
            return new HashSet<FaultSet>(
                from set in sets
                // get a subset of size subsetStepSize
                from subset in GetSubsets(set, subsetStepSize)
                // remove all faults in the subset and their subsuming faults
                let suggestion = set.GetDifference(Fault.SubsumingFaults(subset, allFaults))
                select suggestion
            );
        }

        /// <summary>
        /// Finds all subsets of <paramref name="set"/> with the given <paramref name="size"/>.
        /// </summary>
        private List<FaultSet> GetSubsets(FaultSet set, int size)
        {
            var subset = new Fault[size];
            var results = new List<FaultSet>();
            GetSubsets(set, subset, 0, 0, results);
            return results;
        }

        /// <summary>
        /// Helper function for recursively finding subsets.
        /// </summary>
        private void GetSubsets(FaultSet set, Fault[] subset, int index, int minFaultIndex, List<FaultSet> results)
        {
            if (index == subset.Length)
            {
                results.Add(new FaultSet(subset));
                return;
            }

            for (int i = minFaultIndex; i < allFaults.Length - subset.Length + index; ++i)
            {
                if (set.Contains(allFaults[i]))
                {
                    subset[index] = allFaults[i];
                    GetSubsets(set, subset, index + 1, i + 1, results);
                }
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
