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

namespace SafetySharp.Analysis.Heuristics
{
	using System.Collections.Generic;
	using System.Linq;
	using Modeling;
	using Runtime;
	using Utilities;

	/// <summary>
	///   A heuristic that tries to determine the minimal redundancy necessary in the system so the hazard does not occur.
	/// </summary>
	public sealed class MinimalRedundancyHeuristic<TExecutableModel> : IFaultSetHeuristic where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private const double DefaultMinFaultSetSizeRelative = 0.5;
		private readonly Fault[] _allFaults;
		private readonly int _minSetSize;
		private ISet<FaultSet> _currentSuggestions;
		private ISet<FaultSet> _nextSuggestions;
		private int _subsetStepSize = 1; // how many faults are removed from critical sets in each step
		private int _successCounter;

		/// <summary>
		///   Creates a new instance of the heuristic.
		/// </summary>
		/// <param name="model">The model for which the heuristic is created.</param>
		/// <param name="faultGroups">
		///   Different groups of faults. Suggested fault sets never contain all faults of any group.
		/// </param>
		public MinimalRedundancyHeuristic(TExecutableModel model, params IEnumerable<Fault>[] faultGroups)
			: this(model, DefaultMinFaultSetSizeRelative, faultGroups)
		{
		}

		/// <summary>
		///   Creates a new instance of the heuristic.
		/// </summary>
		/// <param name="model">The model for which the heuristic is created.</param>
		/// <param name="minSetSizeRelative">The relative minimum size of fault sets to check.</param>
		/// <param name="faultGroups">
		///   Different groups of faults. Suggested fault sets never contain all faults of any group.
		/// </param>
		public MinimalRedundancyHeuristic(TExecutableModel model, double minSetSizeRelative, params IEnumerable<Fault>[] faultGroups)
			: this(model, (int)(model.Faults.Length * minSetSizeRelative), faultGroups)
		{
		}

		/// <summary>
		///   Creates a new instance of the heuristic.
		/// </summary>
		/// <param name="model">The model for which the heuristic is created.</param>
		/// <param name="minSetSize">The minimum size of fault sets to check.</param>
		/// <param name="faultGroups">
		///   Different groups of faults. Suggested fault sets never contain all faults of any group.
		/// </param>
		public MinimalRedundancyHeuristic(TExecutableModel model, int minSetSize, params IEnumerable<Fault>[] faultGroups)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(faultGroups, nameof(faultGroups));

			_allFaults = model.Faults.Where(fault => fault.Activation != Activation.Suppressed).ToArray();
			_minSetSize = minSetSize;

			CollectSuggestions(faultGroups);
			_nextSuggestions = GetSubsets(_currentSuggestions);
		}

		void IFaultSetHeuristic.Augment(uint cardinalityLevel, LinkedList<FaultSet> setsToCheck)
		{
			_successCounter = 0;
			foreach (var suggestion in _currentSuggestions)
				setsToCheck.AddFirst(suggestion);
		}

		void IFaultSetHeuristic.Update(LinkedList<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
		{
			var isSuggestion = _currentSuggestions.Remove(checkedSet);
			if (!isSuggestion)
				return;

			// subsets of a safe set are safe - do not check them again
			if (isSafe)
			{
				_successCounter++;
				_nextSuggestions.ExceptWith(GetSubsets(new[] { checkedSet }));
			}
			else
				_successCounter--;

			var tolerance = _allFaults.Length / 4;
			if (_currentSuggestions.Count == 0 && checkedSet.Cardinality > _minSetSize)
			{
				if (_successCounter < -tolerance && isSafe)
				{
					_subsetStepSize++;
					_successCounter = 0;
				}

				_currentSuggestions = _nextSuggestions;
				_nextSuggestions = GetSubsets(_currentSuggestions);
			}
		}

		private void CollectSuggestions(IEnumerable<IEnumerable<Fault>> faultGroups)
		{
			var faults = new FaultSet(_allFaults);

			_currentSuggestions = new HashSet<FaultSet>(
				// one fault of each group is not activated (try all combinations)
				from excludedFaults in CartesianProduct(faultGroups)
				// also exclude subsuming faults
				let subsuming = FaultSet.SubsumingFaults(excludedFaults, _allFaults)
				orderby subsuming.Cardinality ascending
				select faults.GetDifference(subsuming)
				);
		}

		private ISet<FaultSet> GetSubsets(IEnumerable<FaultSet> sets)
		{
			return new HashSet<FaultSet>(
				from set in sets
				// get a subset of size subsetStepSize
				from subset in GetSubsets(set, _subsetStepSize)
				// remove all faults in the subset and their subsuming faults
				let suggestion = set.GetDifference(FaultSet.SubsumingFaults(subset, _allFaults))
				select suggestion
				);
		}

		/// <summary>
		///   Finds all subsets of <paramref name="set" /> with the given <paramref name="size" />.
		/// </summary>
		private List<FaultSet> GetSubsets(FaultSet set, int size)
		{
			var subset = new Fault[size];
			var results = new List<FaultSet>();
			GetSubsets(set, subset, 0, 0, results);
			return results;
		}

		/// <summary>
		///   Helper function for recursively finding subsets.
		/// </summary>
		private void GetSubsets(FaultSet set, Fault[] subset, int index, int minFaultIndex, List<FaultSet> results)
		{
			if (index == subset.Length)
			{
				results.Add(new FaultSet(subset));
				return;
			}

			for (var i = minFaultIndex; i < _allFaults.Length - subset.Length + index; ++i)
			{
				if (set.Contains(_allFaults[i]))
				{
					subset[index] = _allFaults[i];
					GetSubsets(set, subset, index + 1, i + 1, results);
				}
			}
		}

		private static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
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