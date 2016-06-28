using System;
using System.Collections;
using System.Collections.Generic;
using SafetySharp.Runtime;

namespace SafetySharp.Analysis
{
	class FaultSetCollection : IEnumerable<FaultSet>
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
				if (elementsByCardinality[i] == null)
					continue;

				foreach (var element in elementsByCardinality[i])
				{
					if (element.IsSubsetOf(set))
						return true;
				}
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
				if (elementsByCardinality[i] == null)
					continue;

				foreach (var element in elementsByCardinality[i])
				{
					if (set.IsSubsetOf(element))
						return true;
				}
			}

			return false;
		}

		public HashSet<FaultSet> GetMinimalSets()
		{
			var result = new HashSet<FaultSet>();
			for (int i = 0; i <= numFaults; ++i)
			{
				if (elementsByCardinality[i] == null)
					continue;

				foreach (var element in elementsByCardinality[i])
				{
					if (!ContainsSubsetOf(element))
						result.Add(element);
				}
			}
			return result;
		}

		public IEnumerator<FaultSet> GetEnumerator()
		{
			for (int i = 0; i <= numFaults; ++i)
			{
				if (elementsByCardinality[i] == null)
					continue;

				foreach (var element in elementsByCardinality[i])
					yield return element;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
