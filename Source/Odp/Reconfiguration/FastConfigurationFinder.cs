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

		protected readonly bool PreferCapabilityAccumulation;

		/// <summary>
		///   Creates a new instance.
		/// </summary>
		/// <param name="preferCapabilityAccumulation">Whether or not it is preferred that one agent applies multiple capabilities.</param>
		public FastConfigurationFinder(bool preferCapabilityAccumulation)
		{
			PreferCapabilityAccumulation = preferCapabilityAccumulation;
		}

		public Task<Configuration?> Find(TaskFragment taskFragment, ISet<BaseAgent> availableAgents)
		{
			var agents = availableAgents.ToArray();
			CalculateShortestPaths(agents);

			var distribution = FindDistribution(taskFragment, agents);
			if (distribution == null)
				return Task.FromResult<Configuration?>(null);

			var resourceFlow = FindResourceFlow(distribution, agents);

			// translate from index to agent
			var agentDistribution = distribution.Select(i => agents[i]).ToArray();
			var agentResourceFlow = resourceFlow.Select(i => agents[i]).ToArray();

			return Task.FromResult<Configuration?>(new Configuration(agentDistribution, agentResourceFlow));
		}

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

		protected virtual int[] FindDistribution(TaskFragment taskFragment, BaseAgent[] availableAgents)
		{
			var path = new int[taskFragment.Length];

			for (var firstAgent = 0; firstAgent < availableAgents.Length; ++firstAgent)
			{
				if (CanSatisfyNext(taskFragment, availableAgents, path, 0, firstAgent))
				{
					path[0] = firstAgent;
					if (FindDistribution(taskFragment, path, 1, availableAgents))
						return path;
				}
			}

			return null;
		}

		private bool FindDistribution(TaskFragment taskFragment, int[] path, int prefixLength, BaseAgent[] availableAgents)
		{
			// termination case: the path is already complete
			if (prefixLength == taskFragment.Length)
				return true;

			var last = path[prefixLength - 1];

			// special handling: see if the last agent can't do the next capability as well
			if (PreferCapabilityAccumulation && CanSatisfyNext(taskFragment, availableAgents, path, prefixLength, last))
			{
				path[prefixLength] = last;
				if (FindDistribution(taskFragment, path, prefixLength + 1, availableAgents))
					return true;
			}
			else // otherwise check connected agents
			{
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
			}

			return false; // there is no valid path with the given prefix
		}

		protected virtual bool CanSatisfyNext(TaskFragment taskFragment, BaseAgent[] availableAgents, int[] path, int prefixLength, int agent)
		{
			return availableAgents[agent].AvailableCapabilities.Contains(taskFragment[prefixLength]);
		}

		protected virtual IEnumerable<int> FindResourceFlow(int[] distribution, BaseAgent[] availableAgents)
		{
			if (distribution.Length == 0)
				return new int[0];

			var resourceFlow = new List<int>() { distribution[0] };
			for (var i = 1; i < distribution.Length; ++i)
				resourceFlow.AddRange(GetShortestPath(distribution[i-1], distribution[i], availableAgents).Skip(1));

			return resourceFlow;
		}

		protected virtual IEnumerable<int> GetShortestPath(int from, int to, BaseAgent[] availableAgents)
		{
			for (var current = from; current != to; current = _pathMatrix[current, to])
				yield return current;
			yield return to;
		}
	}
}
