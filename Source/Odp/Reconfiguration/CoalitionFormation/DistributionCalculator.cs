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
	using System.Diagnostics;
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

		private readonly IConnectionOracle _oracle;

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="fragment">The fragment for which distributions shall be calculated.</param>
		/// <param name="recoveredDistribution">The previous distribution, as far as recovered.</param>
		/// <param name="agents">The set of available agents.</param>
		/// <param name="oracle">Information about possible connections between agents.</param>
		public DistributionCalculator(TaskFragment fragment, [NotNull, ItemCanBeNull] BaseAgent[] recoveredDistribution, [NotNull, ItemNotNull] ISet<BaseAgent> agents, [NotNull] IConnectionOracle oracle)
		{
			if (recoveredDistribution == null)
				throw new ArgumentNullException(nameof(recoveredDistribution));
			if (agents == null)
				throw new ArgumentNullException(nameof(agents));

			Fragment = fragment;
			_recoveredDistribution = recoveredDistribution;
			_agents = agents;
			_oracle = oracle;
		}

		/// <summary>
		///   Calculates all possible distributions of the capabilities in CTF between the members of the coalition,
		///   ordered by the difference from the recovered distribution.
		/// </summary>
		[NotNull, ItemNotNull]
		public IEnumerable<BaseAgent[]> CalculateDistributions()
		{
			var inevitableChanges = FindInevitableChanges(Fragment, _recoveredDistribution);
			var inevitableChangeDistributions = new HashSet<BaseAgent[]>();

			// find distributions that only modify inevitable positions
			foreach (var distribution in FindInevitableChangeDistributions(inevitableChanges))
			{
				ValidateDistribution(distribution);
				yield return distribution;
				inevitableChangeDistributions.Add(distribution);
			}

			// breadth-first search for other distributions
			foreach (var distribution in FindMinimalChangeDistribution(inevitableChangeDistributions, inevitableChanges))
			{
				ValidateDistribution(distribution);
				yield return distribution;
			}
		}

		// some sanity-checks for distributions
		[Conditional("DEBUG")]
		private void ValidateDistribution(BaseAgent[] distribution)
		{
			Debug.Assert(distribution != null, "Distribution may not be null.");
			Debug.Assert(distribution.Length == Fragment.Length, "Distribution length must match fragment length.");
			Debug.Assert(!distribution.Contains(null), "Distribution must be completely defined.");
			for (var i = 0; i < distribution.Length; ++i)
				Debug.Assert(distribution[i].AvailableCapabilities.Contains(Fragment.Task.RequiredCapabilities[Fragment.Start + i]), "Distribution assigns unavailable capability.");
		}

		/// <summary>
		///   Identifies capability indices in the given <paramref name="fragment"/> that cannot be applied by the same agent as before.
		/// </summary>
		[NotNull]
		private static bool[] FindInevitableChanges(TaskFragment fragment, [NotNull, ItemCanBeNull] BaseAgent[] recoveredDistribution)
		{
			return (from index in fragment.CapabilityIndices
					let isDead = recoveredDistribution[index] == null
					let capability = fragment.Task.RequiredCapabilities[index]
					let lostCapability = !recoveredDistribution[index]?.AvailableCapabilities.Contains(capability) ?? true
					select isDead || lostCapability
			).ToArray();
		}

		/// <summary>
		///   Computes all distributions that only modify positions that cannot remain unchanged.
		/// </summary>
		/// <param name="inevitableChanges">An array of the same length as <see cref="Fragment"/> that indicates for each capability position if a change is inevitable.</param>
		[NotNull, ItemNotNull]
		private IEnumerable<BaseAgent[]> FindInevitableChangeDistributions([NotNull] bool[] inevitableChanges)
		{
			var currentDistribution = new BaseAgent[Fragment.Length];
			Array.Copy(_recoveredDistribution, Fragment.Start, currentDistribution, 0, Fragment.Length);

			return FindInevitableChangeDistributions(currentDistribution, 0, inevitableChanges);
		}

		[NotNull, ItemNotNull]
		private IEnumerable<BaseAgent[]> FindInevitableChangeDistributions([NotNull, ItemCanBeNull] BaseAgent[] distribution, int prefixLength, [NotNull] bool[] inevitableChanges)
		{
			// only modify inevitable positions, skip others
			while (prefixLength < Fragment.Length && !inevitableChanges[prefixLength])
				prefixLength++;

			// termination case: entire distribution found, hence return a copy (unless resource flow impossible)
			if (prefixLength == Fragment.Length)
			{
				if (!_oracle.ConnectionImpossible(distribution))
					yield return Copy(distribution);
				yield break;
			}

			// recursion: for each possibility for prefixLength, compute all possibilities for the remainder.
			foreach (var modifiedDistribution in FindModifications(distribution, prefixLength))
				foreach (var result in FindInevitableChangeDistributions(modifiedDistribution, prefixLength + 1, inevitableChanges))
					yield return result;
		}

		/// <summary>
		///   Finds a distribution with a minimal number of changes from the <see cref="_recoveredDistribution"/>, through a breadth-first search.
		/// </summary>
		[NotNull, ItemNotNull]
		private IEnumerable<BaseAgent[]> FindMinimalChangeDistribution([NotNull, ItemNotNull] IEnumerable<BaseAgent[]> inevitableChangeDistributions, [NotNull] bool[] inevitableChanges) {
			if (inevitableChangeDistributions.Any(d => d.Contains(null)))
				throw new Exception("Invalid input");

			var queue = new Queue<Tuple<BaseAgent[], int>>(inevitableChangeDistributions.Select(d => Tuple.Create(d, -1)));

			while (queue.Count > 0)
			{
				var previous = queue.Dequeue();
				var previousDistribution = previous.Item1;
				var previousModifiedIndex = previous.Item2;

				// select the index to modify (don't modify inevitable change positions)
				var modifiedIndex = previousModifiedIndex + 1;
				while (modifiedIndex < Fragment.Length && inevitableChanges[modifiedIndex])
					++modifiedIndex;

				// if all indices have already been modified: no new distribution found
				if (modifiedIndex >= Fragment.Length)
					continue;

				// Find all possibilities to modify the chosen index, and return the respective resulting distributions.
				foreach (var distribution in FindModifications(previousDistribution, modifiedIndex))
				{
					var result = Copy(distribution);
					yield return result;
					queue.Enqueue(Tuple.Create(result, modifiedIndex));
				}
			}
		}

		/// <summary>
		///   Finds possible agents that can apply the capability at the given <paramref name="modifiedIndex"/>.
		///   Modifies the given <paramref name="distribution"/> in-place.
		/// </summary>
		private IEnumerable<BaseAgent[]> FindModifications(BaseAgent[] distribution, int modifiedIndex)
		{
			var eligibleAgents = _agents.Where(agent => CanSatisfyNext(agent, distribution, modifiedIndex));
			foreach (var agent in eligibleAgents)
			{
				distribution[modifiedIndex] = agent;
				if (!_oracle.ConnectionImpossible(distribution))
					yield return distribution;
			}
		}

		private static T[] Copy<T>(T[] original)
		{
			var copy = new T[original.Length];
			Array.Copy(original, copy, original.Length);
			return copy;
		}

		// TODO: override for pill production
		protected virtual bool CanSatisfyNext(BaseAgent agent, BaseAgent[] distribution, int prefixLength)
		{
			return agent.AvailableCapabilities.Contains(Fragment.Capabilities.ElementAt(prefixLength));
		}
	}
}
