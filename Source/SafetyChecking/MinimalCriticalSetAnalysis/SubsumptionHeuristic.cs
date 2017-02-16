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
	using System.Collections.Generic;
	using Modeling;
	using AnalysisModel;
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
		/// <param name="modelFaults">All faults in the model the heuristic is created for.</param>
		public SubsumptionHeuristic(Fault[] modelFaults)
		{
			Requires.NotNull(modelFaults, nameof(modelFaults));

			_allFaults = modelFaults;
		}

		void IFaultSetHeuristic.Augment(uint cardinalityLevel, LinkedList<FaultSet> setsToCheck)
		{
			// for each set, check the set of subsumed faults first
			for (var node = setsToCheck.First; node != null; node = node.Next)
			{
				var subsumed = FaultSet.SubsumedFaults(node.Value, _allFaults);
				if (!node.Value.Equals(subsumed) && !_subsumedSets.Contains(subsumed))
				{
					setsToCheck.AddBefore(node, subsumed);
					_subsumedSets.Add(subsumed);
				}
			}
		}

		void IFaultSetHeuristic.Update(LinkedList<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
		{
			if (!_subsumedSets.Contains(checkedSet))
				return;

			_successCounter += isSafe ? 1 : -1;
			if (_successCounter != 0)
				return;

			// if the subsumed sets are critical more often than they are not,
			// check the "normal" sets first.
			for (var node = setsToCheck.First; node != null; node = node.Next)
			{
				var set = node.Value;
				if (_subsumedSets.Contains(set))
				{
					if (isSafe && node.Previous != null) // move subsumed set before set
						SwitchValues(node, node.Previous);
					else if (!isSafe && node.Next != null) // move subsumed set after set
					{
						SwitchValues(node, node.Next);
						node = node.Next; // skip iteration for node.Next
					}
				}
			}
		}

		private void SwitchValues(LinkedListNode<FaultSet> node1, LinkedListNode<FaultSet> node2)
		{
			var tmp = node1.Value;
			node1.Value = node2.Value;
			node2.Value = tmp;
		}
	}
}