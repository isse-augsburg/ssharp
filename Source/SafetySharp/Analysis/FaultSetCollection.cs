using System.Collections.Generic;
using System.Linq;
using SafetySharp.Runtime;

namespace SafetySharp.Analysis
{
	class FaultSetCollection
	{
		private readonly int numFaults;
		private readonly HashSet<FaultSet>[] elementsByCardinality;

		public FaultSetCollection(int numFaults)
		{
			this.numFaults = numFaults;
			elementsByCardinality = new HashSet<FaultSet>[numFaults + 1];
		}

		public void Add(FaultSet set)
		{
			var cardinality = set.Cardinality;
			if (elementsByCardinality[cardinality] == null)
				elementsByCardinality[cardinality] = new HashSet<FaultSet>();
			elementsByCardinality[cardinality].Add(set);
		}

		public bool ContainsSubsetOf(FaultSet set)
		{
			var cardinality = set.Cardinality;
			if (elementsByCardinality[cardinality]?.Contains(set) ?? false)
				return true;

			for (int i = 0; i < cardinality; ++i)
			{
				if (elementsByCardinality[cardinality]?.Any(other => other.IsSubsetOf(set)) ?? false)
					return true;
			}
			return false;
		}

		public bool ContainsSupersetOf(FaultSet set)
		{
			var cardinality = set.Cardinality;
			if (elementsByCardinality[cardinality]?.Contains(set) ?? false)
				return true;

			for (int i = numFaults; i > cardinality; --i)
			{
				if (elementsByCardinality[cardinality]?.Any(other => set.IsSubsetOf(other)) ?? false)
					return true;
			}
			return false;
		}
	}
}
