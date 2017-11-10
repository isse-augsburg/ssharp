// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using JetBrains.Annotations;

	/// <summary>
	///   Computes distributions of capabilities within a given task fragment among a given set of agents.
	/// </summary>
	public class DistributionCalculator
	{
		public TaskFragment Fragment { get; }

		private readonly BaseAgent[] _recoveredDistribution;

		private readonly ISet<BaseAgent> _agents;

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="fragment">The fragment for which distributions shall be calculated.</param>
		/// <param name="recoveredDistribution">The previous distribution, as far as recovered. Later updates to this array will be picked up.</param>
		/// <param name="agents">The set of available agents. Later updates to this collection will be picked up.</param>
		public DistributionCalculator(TaskFragment fragment, [NotNull, ItemCanBeNull] BaseAgent[] recoveredDistribution, [NotNull, ItemNotNull] ISet<BaseAgent> agents)
		{
			if (recoveredDistribution == null)
				throw new ArgumentNullException(nameof(recoveredDistribution));
			if (agents == null)
				throw new ArgumentNullException(nameof(agents));

			Fragment = fragment;
			_recoveredDistribution = recoveredDistribution;
			_agents = agents;
		}

		/// <summary>
		///   Calculates all possible distributions of the capabilities in CTF between the members of the coalition,
		///   ordered by the difference from the recovered distribution.
		/// </summary>
		public IEnumerable<BaseAgent[]> CalculateDistributions()
		{
			var distribution = new BaseAgent[Fragment.Length];
			return CalculateCapabilityDistributions(distribution, 0)
				.OrderBy(newDistribution =>
				{
					var changedPositions = Enumerable.Range(0, newDistribution.Length)
													 .Where(i => newDistribution[i] != _recoveredDistribution[i])
													 .ToArray();
					return changedPositions.Any() ? changedPositions.Max() - changedPositions.Min() : 0;
				});
		}

		// enumerate all paths, but lazily! (depth-first search)
		private IEnumerable<BaseAgent[]> CalculateCapabilityDistributions(BaseAgent[] distribution, int prefixLength)
		{
			// termination case: copy distribution and return it
			if (prefixLength == distribution.Length)
			{
				var result = new BaseAgent[distribution.Length];
				Array.Copy(distribution, result, distribution.Length);
				yield return result;
				yield break;
			}

			// recursive case: iterate through all possible next agents, recurse, forward results
			var eligibleAgents = _agents.Where(agent => CanSatisfyNext(agent, distribution, prefixLength));
			foreach (var agent in eligibleAgents)
			{
				distribution[prefixLength] = agent;
				foreach (var result in CalculateCapabilityDistributions(distribution, prefixLength + 1))
					yield return result;
			}
		}

		// TODO: override for pill production
		protected virtual bool CanSatisfyNext(BaseAgent agent, BaseAgent[] distribution, int prefixLength)
		{
			return agent.AvailableCapabilities.Contains(Fragment.Capabilities.ElementAt(prefixLength));
		}
	}
}
