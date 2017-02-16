// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace ISSE.SafetyChecking.MinimalCriticalSetAnalysis
{
	using System.Collections;
	using System.Collections.Generic;
	using AnalysisModel;

	/// <summary>
	///   Represents a collection of <see cref="FaultSet" /> ordered by cardinality.
	/// </summary>
	internal class FaultSetCollection : IEnumerable<FaultSet>
	{
		private readonly HashSet<FaultSet>[] _elementsByCardinality;
		private readonly int _maxCardinality;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="maxCardinality">The maximum number of faults contained in the collection.</param>
		public FaultSetCollection(int maxCardinality)
		{
			_maxCardinality = maxCardinality;
			_elementsByCardinality = new HashSet<FaultSet>[maxCardinality + 1];
		}

		/// <summary>
		///   Returns an enumerator that iterates through the entire collection.
		/// </summary>
		public IEnumerator<FaultSet> GetEnumerator()
		{
			for (var i = 0; i <= _maxCardinality; ++i)
			{
				if (_elementsByCardinality[i] == null)
					continue;

				foreach (var element in _elementsByCardinality[i])
					yield return element;
			}
		}

		/// <summary>
		///   Returns an enumerator that iterates through the entire collection.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		///   Adds the <paramref name="faultSet" /> to the collection.
		/// </summary>
		/// <param name="faultSet">The fault set that should be added.</param>
		public void Add(FaultSet faultSet)
		{
			var cardinality = faultSet.Cardinality;

			if (_elementsByCardinality[cardinality] == null)
				_elementsByCardinality[cardinality] = new HashSet<FaultSet>();

			_elementsByCardinality[cardinality].Add(faultSet);
		}

		/// <summary>
		/// Checks if the collection contains the given <paramref name="faultSet"/>.
		/// </summary>
		public bool Contains(FaultSet faultSet)
		{
			return _elementsByCardinality[faultSet.Cardinality]?.Contains(faultSet) ?? false;
		}

		/// <summary>
		///   Checks whether the collection contains a subset of <paramref name="faultSet." />
		/// </summary>
		/// <param name="faultSet">The fault set that should be checked.</param>
		public bool ContainsSubsetOf(FaultSet faultSet)
		{
			return Contains(faultSet) || ContainsProperSubsetOf(faultSet);
		}

		/// <summary>
		///   Checks whether the collection contains a proper subset of <paramref name="faultSet." />
		/// </summary>
		/// <param name="faultSet">The fault set that should be checked.</param>
		public bool ContainsProperSubsetOf(FaultSet faultSet)
		{
			var cardinality = faultSet.Cardinality;

			for (var i = 0; i < cardinality; ++i)
			{
				if (_elementsByCardinality[i] == null)
					continue;

				foreach (var element in _elementsByCardinality[i])
				{
					if (element.IsSubsetOf(faultSet))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		///   Checks whether the collection contains a superset of <paramref name="faultSet." />
		/// </summary>
		/// <param name="faultSet">The fault set that should be checked.</param>
		public bool ContainsSupersetOf(FaultSet faultSet)
		{
			var cardinality = faultSet.Cardinality;

			if (_elementsByCardinality[cardinality]?.Contains(faultSet) ?? false)
				return true;

			for (var i = _maxCardinality; i > cardinality; --i)
			{
				if (_elementsByCardinality[i] == null)
					continue;

				foreach (var element in _elementsByCardinality[i])
				{
					if (faultSet.IsSubsetOf(element))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		///   Gets the minimal sets contained in the collection.
		/// </summary>
		public HashSet<FaultSet> GetMinimalSets()
		{
			var result = new HashSet<FaultSet>();

			for (var i = 0; i <= _maxCardinality; ++i)
			{
				if (_elementsByCardinality[i] == null)
					continue;

				foreach (var element in _elementsByCardinality[i])
				{
					if (!ContainsProperSubsetOf(element))
						result.Add(element);
				}
			}

			return result;
		}
	}
}