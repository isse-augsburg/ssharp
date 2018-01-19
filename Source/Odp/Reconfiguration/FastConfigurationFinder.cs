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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SafetySharp.Odp.Reconfiguration
{
	using System.Linq;
	using JetBrains.Annotations;
	using Modeling;

	/// <summary>
	///   A <see cref="IConfigurationFinder"/> that follows the resource flow and returns the first solution it can find.
	/// </summary>
	public class FastConfigurationFinder : IConfigurationFinder
	{
		[NonSerializable]
		protected int[,] _costMatrix;
		[NonSerializable]
		protected int[,] _pathMatrix;
		[NonSerializable]
		protected int[] _precedingProducer;
		[NonSerializable]
		protected int[] _succedingConsumer;

		protected readonly bool PreferCapabilityAccumulation;

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="preferCapabilityAccumulation">Whether or not it is preferred that one agent applies multiple capabilities.</param>
		public FastConfigurationFinder(bool preferCapabilityAccumulation)
		{
			PreferCapabilityAccumulation = preferCapabilityAccumulation;
		}

		public Task<Configuration?> Find(TaskFragment taskFragment, ISet<BaseAgent> availableAgents, Predicate<BaseAgent> isProducer, Predicate<BaseAgent> isConsumer)
		{
			var agents = availableAgents.ToArray();
			CalculateShortestPaths(agents);
			IdentifyStartEndAgents(agents, isProducer, isConsumer);

			var distribution = FindDistribution(taskFragment, agents);
			if (distribution == null)
				return Task.FromResult<Configuration?>(null);

			var resourceFlow = FindResourceFlow(distribution, agents);
			if (resourceFlow == null)
				return Task.FromResult<Configuration?>(null);

			// translate from index to agent
			var agentDistribution = distribution.Select(i => agents[i]).ToArray();
			var agentResourceFlow = resourceFlow.Select(i => agents[i]).ToArray();

			return Task.FromResult<Configuration?>(new Configuration(agentDistribution, agentResourceFlow));
		}

		/// <summary>
		///   Employs Floyd-Warshall to calculate the shortest paths and their lengths between all pairs of <paramref name="availableAgents"/>.
		/// </summary>
		private void CalculateShortestPaths(BaseAgent[] availableAgents)
		{
			_pathMatrix = new int[availableAgents.Length, availableAgents.Length];
			_costMatrix = new int[availableAgents.Length, availableAgents.Length];

			for (var i = 0; i < availableAgents.Length; ++i)
			{
				for (var j = 0; j < availableAgents.Length; ++j)
				{
					// neighbours
					if (availableAgents[i].Outputs.Contains(availableAgents[j]) && availableAgents[j].Inputs.Contains(availableAgents[i]))
					{
						_pathMatrix[i, j] = j;
						_costMatrix[i, j] = 1;
					}
					else // default for non-neighbours
					{
						_pathMatrix[i, j] = -1; // signifies no path
						_costMatrix[i, j] = -1; // signifies infinity
					}
				}

				// reflexive case
				_pathMatrix[i, i] = i;
				_costMatrix[i, i] = 0;
			}

			// Floyd-Warshall algorithm
			for (var link = 0; link < availableAgents.Length; ++link)
			{
				for (var start = 0; start < availableAgents.Length; ++start)
				{
					for (var end = 0; end < availableAgents.Length; ++end)
					{
						if (_costMatrix[start, link] > -1 && _costMatrix[link, end] > -1 // paths start->link and link->end exist
							&& (_costMatrix[start, end] == -1 || _costMatrix[start, end] > _costMatrix[start, link] + _costMatrix[link, end]))
						{
							_costMatrix[start, end] = _costMatrix[start, link] + _costMatrix[link, end];
							_pathMatrix[start, end] = _pathMatrix[start, link];
						}
					}
				}
			}
		}

		/// <summary>
		///   Associates agents with the closest producer (so that the agent can be reached from the producer) and the closest consumer
		///   (so that the consumer can be reached from the agent).
		/// </summary>
		private void IdentifyStartEndAgents(BaseAgent[] availableAgents, Predicate<BaseAgent> isProducer, Predicate<BaseAgent> isConsumer)
		{
			_precedingProducer = new int[availableAgents.Length];
			_succedingConsumer = new int[availableAgents.Length];

			for (var i = 0; i < availableAgents.Length; ++i)
			{
				_precedingProducer[i] = isProducer(availableAgents[i]) ? i : -1;
				_succedingConsumer[i] = isConsumer(availableAgents[i]) ? i : -1;
			}

			for (var i = 0; i < availableAgents.Length; ++i)
			{
				for (var j = 0; j < availableAgents.Length; ++j)
				{
					if (_precedingProducer[i] == i && _costMatrix[i, j] >= 0 && // if i is a producer and j can be reached from i...
						(_precedingProducer[j] == -1 || _costMatrix[i, j] < _costMatrix[_precedingProducer[j], j])) // ... and j has no preceding producer or it is farther away than i:
						_precedingProducer[j] = i; // choose i as producer for j

					if (_succedingConsumer[i] == i && _costMatrix[j, i] >= 0 &&
						(_succedingConsumer[j] == -1 || _costMatrix[j, i] < _costMatrix[j, _succedingConsumer[j]]))
						_succedingConsumer[j] = i;
				}
			}
		}

		/// <summary>
		///   Finds a sequence of agents for each capability in the given <paramref name="taskFragment"/>, so that
		///   * each agent has the respective capability
		///   * a (possibly indirect) resource flow connection between each agent and its successor exists
		///   * the first agent can be reached from a producer, and a consumer can be reached from the last agent
		/// </summary>
		[CanBeNull]
		protected virtual int[] FindDistribution(TaskFragment taskFragment, BaseAgent[] availableAgents)
		{
			var path = new int[taskFragment.Length];
			if (taskFragment.Length == 0)
				return path;

			for (var firstAgent = 0; firstAgent < availableAgents.Length; ++firstAgent)
			{
				if (_precedingProducer[firstAgent] != -1 && CanSatisfyNext(taskFragment, availableAgents, path, 0, firstAgent))
				{
					path[0] = firstAgent;
					if (FindDistribution(taskFragment, path, 1, availableAgents))
						return path;
				}
			}

			return null;
		}

		/// <summary>
		///   Recursive helper method for the previous method.
		/// </summary>
		private bool FindDistribution(TaskFragment taskFragment, int[] path, int prefixLength, BaseAgent[] availableAgents)
		{
			var last = path[prefixLength - 1];

			// termination case: the path is already complete
			if (prefixLength == taskFragment.Length)
				return _succedingConsumer[last] != -1; // successful only if a consumer can be reached from the end of path

			// special handling: see if the last agent can do the next capability as well
			if (PreferCapabilityAccumulation && CanSatisfyNext(taskFragment, availableAgents, path, prefixLength, last))
			{
				path[prefixLength] = last;
				if (FindDistribution(taskFragment, path, prefixLength + 1, availableAgents))
					return true;
			}

			// otherwise check connected agents
			for (var next = 0; next < availableAgents.Length; ++next) // go through all agents
			{
				// if connected to last agent and can fulfill next capability
				if (_pathMatrix[last, next] != -1 && CanSatisfyNext(taskFragment, availableAgents, path, prefixLength, next))
				{
					path[prefixLength] = next; // try a path over next
					if (FindDistribution(taskFragment, path, prefixLength + 1, availableAgents)) // if there is such a path, return true
						return true;
				}
			}

			return false; // there is no valid path with the given prefix
		}

		/// <summary>
		///   Used to decide if an agent can fulfill a certain capability. Can be overridden by subclasses.
		/// </summary>
		protected virtual bool CanSatisfyNext(TaskFragment taskFragment, BaseAgent[] availableAgents, int[] path, int prefixLength, int agent)
		{
			return availableAgents[agent].AvailableCapabilities.Contains(taskFragment[prefixLength]);
		}

		/// <summary>
		///   Finds a sequence of immediately connected agents so that
		///   * the given <paramref name="distribution"/> is a subsequence of the returned sequence
		///   * the first agent in the sequence is a producer, and the last agent is a consumer
		/// </summary>
		[CanBeNull]
		protected virtual IEnumerable<int> FindResourceFlow(int[] distribution, BaseAgent[] availableAgents)
		{
			if (distribution.Length == 0)
				return FindArbitraryResourceFlow(availableAgents);

			var prefixPath = GetShortestPath(_precedingProducer[distribution[0]], distribution[0], availableAgents);
			var resourceFlow = new List<int>(prefixPath);

			for (var i = 1; i < distribution.Length; ++i)
			{
				var path = GetShortestPath(distribution[i - 1], distribution[i], availableAgents);
				resourceFlow.AddRange(path.Skip(1));
			}

			var suffixPath = GetShortestPath(distribution[distribution.Length - 1], _succedingConsumer[distribution[distribution.Length - 1]], availableAgents);
			resourceFlow.AddRange(suffixPath.Skip(1));

			return resourceFlow;
		}

		/// <summary>
		///   Finds a sequence of immediately connected agents so that the first agent in the sequence is a producer, and the last agent is a consumer.
		/// </summary>
		[CanBeNull]
		private IEnumerable<int> FindArbitraryResourceFlow(BaseAgent[] availableAgents)
		{
			return (from i in Enumerable.Range(0, availableAgents.Length)
					where _succedingConsumer[i] == i
						  && _precedingProducer[i] != -1
					orderby _costMatrix[_precedingProducer[i], i]
					select GetShortestPath(_precedingProducer[i], i, availableAgents)).FirstOrDefault();
		}

		/// <summary>
		///   Finds the shortest path between two given agents. The path includes both agents. The two agents may be the same (in which case it is listed only once).
		/// </summary>
		protected virtual IEnumerable<int> GetShortestPath(int from, int to, BaseAgent[] availableAgents)
		{
			for (var current = from; current != to; current = _pathMatrix[current, to])
				yield return current;
			yield return to;
		}
	}
}
