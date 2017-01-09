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
	///   A heuristic that suggests maximally safe fault sets based on the currently known minimal critical ones.
	/// </summary>
	public sealed class MaximalSafeSetHeuristic<TExecutableModel> : IFaultSetHeuristic where TExecutableModel : ExecutableModel<TExecutableModel>
	{
		private readonly uint _cardinalityLevel;
		private readonly List<Fault[]> _minimalCriticalSets = new List<Fault[]>();
		private readonly TExecutableModel _model;
		private readonly List<FaultSet> _suggestedSets = new List<FaultSet>();
		private FaultSet _allFaults;
		private bool _hasNewMinimalCriticalSets;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the heuristic is created for.</param>
		/// <param name="cardinalityLevel">The cardinality level where the first suggestions should be made.</param>
		public MaximalSafeSetHeuristic(TExecutableModel model, uint cardinalityLevel = 3)
		{
			Requires.NotNull(model, nameof(model));

			_model = model;
			_cardinalityLevel = cardinalityLevel;
			_allFaults = new FaultSet(model.Faults.Where(fault => fault.Activation != Activation.Suppressed).ToArray());
		}

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the heuristic is created for.</param>
		/// <param name="minimalCriticalFaultSets">The minimal critical fault sets known from a previous analysis.</param>
		public MaximalSafeSetHeuristic(TExecutableModel model, ISet<ISet<Fault>> minimalCriticalFaultSets)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(minimalCriticalFaultSets, nameof(minimalCriticalFaultSets));

			_model = model;
			_hasNewMinimalCriticalSets = true;
			_allFaults = new FaultSet(model.Faults.Where(fault => fault.Activation != Activation.Suppressed).ToArray());

			foreach (var set in minimalCriticalFaultSets)
				_minimalCriticalSets.Add(set.Select(MapFault).ToArray());
		}

		/// <summary>
		///   Changes the sets that will be checked by DCCA, by reordering and adding sets.
		/// </summary>
		/// <param name="cardinalityLevel">The level of cardinality that is currently checked.</param>
		/// <param name="setsToCheck">The next sets to be checked, in reverse order (the last set is checked first).</param>
		public void Augment(uint cardinalityLevel, LinkedList<FaultSet> setsToCheck)
		{
			if (setsToCheck.Count == 0 || _minimalCriticalSets.Count == 0 || _cardinalityLevel > cardinalityLevel || !_hasNewMinimalCriticalSets)
				return;

			_suggestedSets.Clear();
			_hasNewMinimalCriticalSets = false;

			foreach (var set in RemoveAllFaults(new FaultSet(), 0))
			{
				setsToCheck.AddFirst(_allFaults.GetDifference(set));
				_suggestedSets.Add(_allFaults.GetDifference(set));
			}
		}

		/// <summary>
		///   Informs the heuristic of the result of analyzing <paramref name="checkedSet" />
		///   and allows it to adapt the sets to check next.
		/// </summary>
		public void Update(LinkedList<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
		{
			// Ignore critical sets we've suggested ourself as these are likely not minimal and
			// would degrade the quality of our suggestions
			if (isSafe || _suggestedSets.Contains(checkedSet))
				return;

			_hasNewMinimalCriticalSets = true;
			_minimalCriticalSets.Add(checkedSet.ToFaultSequence(_model.Faults).ToArray());
		}

		/// <summary>
		///   Maps the <paramref name="fault" /> to the corresponding instance in the <see cref="_model" /> based on name matching.
		/// </summary>
		private Fault MapFault(Fault fault)
		{
			var candidates = _model.Faults.Where(f => f.Name == fault.Name).ToArray();

			Requires.That(candidates.Length == 1, $"Failed to map fault {fault.Name} to its corresponding instance in the new model.");
			return candidates[0];
		}

		/// <summary>
		///   Gets all combinations of fault sets that have one fault of each minimal critical fault set removed.
		/// </summary>
		private IEnumerable<FaultSet> RemoveAllFaults(FaultSet removed, int setIndex)
		{
			if (setIndex >= _minimalCriticalSets.Count)
				yield return removed;
			else
			{
				foreach (var fault in _minimalCriticalSets[setIndex].Where(f => f.Activation != Activation.Forced))
				{
					var next = removed.Contains(fault) ? removed : removed.Add(fault);

					foreach (var set in RemoveAllFaults(next, setIndex + 1))
						yield return set;
				}
			}
		}
	}
}