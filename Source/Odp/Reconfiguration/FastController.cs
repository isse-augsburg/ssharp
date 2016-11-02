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

namespace SafetySharp.Odp.Reconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Modeling;

	public class FastController : AbstractController
	{
		[Hidden(HideElements = true)]
		protected BaseAgent[] _availableAgents;

		[Hidden(HideElements = true)]
		protected int[,] _costMatrix;

		[Hidden(HideElements = true)]
		protected int[,] _pathMatrix;

		public FastController(BaseAgent[] agents) : base(agents) { }

		// synchronous implementation
		public override Task<ConfigurationUpdate> CalculateConfigurations(params ITask[] tasks)
		{
			_availableAgents = GetAvailableAgents();
			var configs = new ConfigurationUpdate();

			CalculateShortestPaths();

			foreach (var task in tasks)
			{
				configs.RemoveAllRoles(task, Agents);
				var path = FindAgentPath(task);
				if (path == null)
					ReconfigurationFailure = true;
				else
					ExtractConfigurations(configs, task, path);
			}

			OnConfigurationsCalculated(configs);
			return Task.FromResult(configs);
		}

		protected virtual bool PreferCapabilityAccumulation => true;

		protected virtual BaseAgent[] GetAvailableAgents()
		{
			return Array.FindAll(Agents, agent => agent.IsAlive);
		}

		private void CalculateShortestPaths()
		{
			_pathMatrix = new int[_availableAgents.Length, _availableAgents.Length];
			_costMatrix = new int[_availableAgents.Length, _availableAgents.Length];

			for (var i = 0; i < _availableAgents.Length; ++i)
			{
				for (var j = 0; j < _availableAgents.Length; ++j)
				{
					// neighbours
					if (_availableAgents[i].Outputs.Contains(_availableAgents[j]))
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
			for (var link = 0; link < _availableAgents.Length; ++link)
			{
				for (var start = 0; start < _availableAgents.Length; ++start)
				{
					for (var end = 0; end < _availableAgents.Length; ++end)
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

		protected virtual int[] FindAgentPath(ITask task)
		{
			var path = new int[task.RequiredCapabilities.Length];

			for (var firstAgent = 0; firstAgent < _availableAgents.Length; ++firstAgent)
			{
				if (CanSatisfyNext(task, path, 0, firstAgent))
				{
					path[0] = firstAgent;
					if (FindAgentPath(task, path, 1))
						return path;
				}
			}

			return null;
		}

		private bool FindAgentPath(ITask task, int[] path, int prefixLength)
		{
			// termination case: the path is already complete
			if (prefixLength == task.RequiredCapabilities.Length)
				return true;

			var last = path[prefixLength - 1];

			// special handling: see if the last agent can't do the next capability as well
			if (PreferCapabilityAccumulation && CanSatisfyNext(task, path, prefixLength, last))
			{
				path[prefixLength] = last;
				if (FindAgentPath(task, path, prefixLength + 1))
					return true;
			}
			else // otherwise check connected agents
			{
				for (int next = 0; next < _availableAgents.Length; ++next) // go through all agents
				{
					// if connected to last agent and can fulfill next capability
					if (_pathMatrix[last, next] != -1 && CanSatisfyNext(task, path, prefixLength, next))
					{
						path[prefixLength] = next; // try a path over next
						if (FindAgentPath(task, path, prefixLength + 1)) // if there is such a path, return true
							return true;
					}
				}
			}

			return false; // there is no valid path with the given prefix
		}

		protected virtual bool CanSatisfyNext(ITask task, int[] path, int prefixLength, int agent)
		{
			return _availableAgents[agent].AvailableCapabilities.Contains(task.RequiredCapabilities[prefixLength]);
		}

		private void ExtractConfigurations(ConfigurationUpdate configs, ITask task, int[] agentPath)
		{
			var role = GetRole(task, null, null);
			for (var i = 0; i < agentPath.Length; ++i)
			{
				// add the capability to the current role
				var capability = task.RequiredCapabilities[i];
				role.AddCapability(capability);
				role.PostCondition.AppendToState(capability);

				// if this concludes the current role, prepare the next one
				if (i + 1 < agentPath.Length && agentPath[i] != agentPath[i+1])
				{
					// configure the connecting agents
					BaseAgent first, last;
					ConfigureConnection(agentPath[i], agentPath[i + 1], task, role.PostCondition, configs, out first, out last);

					// complete the current role
					role.PostCondition.Port = first;
					configs.AddRoles(_availableAgents[agentPath[i]], role);

					// create a new role, referencing the last connection agent
					// (role.PostCondition has the same state as the last transport role's PostCondition)
					role = GetRole(task, last, role.PostCondition);
				}
			}
			// the last role was not yet saved, do it now
			configs.AddRoles(_availableAgents[agentPath[agentPath.Length - 1]], role);
		}

		private void ConfigureConnection(
			int from, int to,
			ITask task, Condition initialCondition, ConfigurationUpdate configs,
			out BaseAgent first, out BaseAgent last
		)
		{
			var connection = GetShortestPath(from, to).ToArray();

			// assign the agents following "from" and preceeding "to"
			// (since first != to, connection.Length >= 2)
			first = _availableAgents[connection[1]];
			last = _availableAgents[connection[connection.Length - 2]];

			BaseAgent previous = _availableAgents[from];

			// for all agents between "from" and "to":
			for (var i = 1; i < connection.Length - 1; ++i)
			{
				var link = _availableAgents[connection[i]];

				// (initialCondition has the same state as the previous transport role's PostCondition)
				var linkRole = GetRole(task, previous, initialCondition);
				linkRole.PostCondition.Port = _availableAgents[connection[i + 1]];
				configs.AddRoles(link, linkRole);

				previous = link;
			}
		}

		protected virtual IEnumerable<int> GetShortestPath(int from, int to)
		{
			for (int current = from; current != to; current = _pathMatrix[current, to])
				yield return current;
			yield return to;
		}
	}
}