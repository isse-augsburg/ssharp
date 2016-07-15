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
	using Modeling;
	using Utilities;

	/// <summary>
	///   A heuristic taking the subsumption relation between different <see cref="Fault" /> instances into account.
	/// </summary>
	public sealed class SubsumptionHeuristic : IFaultSetHeuristic
	{
		private readonly Fault[] _allFaults;
		private readonly HashSet<FaultSet> _subsumedSets = new HashSet<FaultSet>();
		private int _successCounter;

		/// <summary>
		///   Creates a new instance of the heuristic.
		/// </summary>
		/// <param name="model">The model for which the heuristic is created.</param>
		public SubsumptionHeuristic(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));

			_allFaults = model.Faults;
		}

		void IFaultSetHeuristic.Augment(uint cardinalityLevel, List<FaultSet> setsToCheck)
		{
			// for each set, check the set of subsumed faults first
			for (var i = 0; i < setsToCheck.Count; ++i)
			{
				var subsumed = FaultSet.SubsumedFaults(setsToCheck[i], _allFaults);
				if (!setsToCheck[i].Equals(subsumed) && !_subsumedSets.Contains(subsumed))
				{
					setsToCheck.Insert(i + 1, subsumed);
					_subsumedSets.Add(subsumed);
					i++;
				}
			}
		}

		void IFaultSetHeuristic.Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
		{
			if (!_subsumedSets.Contains(checkedSet))
				return;

			var delta = isSafe ? 1 : -1;
			_successCounter += delta;

			if (_successCounter != 0)
				return;

			// if the subsumed sets are critical more often than they are not,
			// check the "normal" sets first.
			for (var i = 0; i < setsToCheck.Count; ++i)
			{
				var set = setsToCheck[i];
				if (_subsumedSets.Contains(set))
				{
					setsToCheck[i] = setsToCheck[i + delta];
					setsToCheck[i + delta] = set;
					if (isSafe) // we just moved 'set' to position i+1
						i++; // skip it, don't move it again
				}
			}
		}
	}
}