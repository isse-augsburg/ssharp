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

namespace SafetySharp.Odp.Reconfiguration.CoalitionFormation
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	/// <summary>
	/// A decentralized, local reconfiguration algorithm based on coalition formation.
	/// This class is only meant to be used by the <see cref="CoalitionReconfigurationAgent"/> class
	/// (and possibly subclasses). It is implemented as an <see cref="IController"/> nevertheless in
	/// order to allow compositional usage within other controllers.
	/// 
	/// (cf. Konstruktion selbst-organisierender Softwaresysteme, chapter 8)
	/// </summary>
	public partial class CoalitionFormationController : AbstractController
	{
		public CoalitionFormationController(BaseAgent[] agents) : base(agents) { }

		public override Task<ConfigurationUpdate> CalculateConfigurations(object context, params ITask[] tasks)
		{
			Debug.Assert(tasks.Length == 1);
			var task = tasks[0];

			var leader = (CoalitionReconfigurationAgent)context;
			var coalition = new Coalition(leader, task, leader.BaseAgentState.ViolatedPredicates);

			return CalculateConfigurations(coalition);
		}

		protected async Task<ConfigurationUpdate> CalculateConfigurations(Coalition coalition)
		{
			try
			{
				foreach (var predicate in coalition.ViolatedPredicates)
					await RecruitNecessaryAgents(coalition, predicate);

				await coalition.InviteCtfAgents();

				do
				{
					foreach (var distribution in CalculateCapabilityDistributions(coalition))
					{
						var reconfSuggestion = new ConfigurationSuggestion(coalition, distribution);

						// compute tfr, edge, core agents
						reconfSuggestion.ComputeTFR();
						foreach (var edgeAgent in reconfSuggestion.EdgeAgents)
							await coalition.Invite(edgeAgent);

						// use dijkstra to find resource flow
						var resourceFlow = await ComputeResourceFlow(reconfSuggestion);
						if (resourceFlow != null)
							return ComputeRoleAllocations(reconfSuggestion, resourceFlow.ToArray());
					}

					// TODO: if still no path found: recruit additional agents (if further exist)
				} while (true /* TODO: while further agents exist */);
			}
			catch (OperationCanceledException)
			{
				// operation was canceled (e.g. because coalition was merged into another coalition), so produce no updates
				return null;
			}
			catch (RestartReconfigurationException)
			{
				// restart reconfiguration, e.g. because another coalition was just merged into the current one
				return await CalculateConfigurations(coalition);
			}

			// failed to find a solution
			return null;
		}

		/// <summary>
		/// Selects the strategy used to solve the occuring invariant violations based on the violated predicate.
		/// </summary>
		protected virtual Task RecruitNecessaryAgents(Coalition coalition, InvariantPredicate invariant)
		{
			if (invariant == Invariant.CapabilityConsistency)
				return RecruitMissingCapabilities(coalition);
			else if (invariant == Invariant.IOConsistency)
				return RecruitIOReplacements(coalition);
			else if (invariant == Invariant.NeighborsAliveGuarantee)
				return ReplaceDeadNeighbours(coalition);

			throw new NotImplementedException(); // TODO: strategies for other invariant predicates
		}

		protected async Task RecruitIOReplacements(Coalition coalition)
		{
			var affectedRoles = await FindDisconnectedRoles(coalition);

			// if affected role empty: find predecessor / successor roles with at least one capability, invite along the way
			foreach (var entry in affectedRoles)
			{
				var agent = entry.Item1;
				var role = entry.Item2;

				// predecessor
				var currentRole = role;
				var currentAgent = agent;
				var previousAgent = currentRole.PreCondition.Port;

				while (previousAgent != null && currentRole.CapabilitiesToApply.Count() == 0)
				{
					await coalition.Invite(previousAgent);
					currentRole = previousAgent.AllocatedRoles.Single(otherRole =>
						otherRole.PostCondition.StateMatches(currentRole.PreCondition) && otherRole.PostCondition.Port == currentAgent
					);
					currentAgent = previousAgent;
					previousAgent = currentRole.PreCondition.Port;
				}

				// successor
				currentRole = role;
				currentAgent = agent;
				var nextAgent = currentRole.PostCondition.Port;

				while (nextAgent != null && currentRole.CapabilitiesToApply.Count() == 0)
				{
					await coalition.Invite(nextAgent);
					currentRole = nextAgent.AllocatedRoles.Single(otherRole =>
						otherRole.PreCondition.StateMatches(currentRole.PostCondition) && otherRole.PreCondition.Port == currentAgent
					);
					currentAgent = nextAgent;
					nextAgent = currentRole.PostCondition.Port;
				}
			}
		}

		protected async Task ReplaceDeadNeighbours(Coalition coalition)
		{
			var affectedRoles = from member in coalition.Members
								let agent = member.BaseAgent
								from role in agent.AllocatedRoles
								where role.Task == coalition.Task && (role.PreCondition.Port?.IsAlive == false || role.PostCondition.Port?.IsAlive == false)
								select role;

			var taggedRoles = coalition.Members.SelectMany(member =>
				member.BaseAgent.AllocatedRoles.Select(r => Tuple.Create(member.BaseAgent, r))
			);
			// for each role, must find predecessor / successor role
			foreach (var role in affectedRoles)
			{
				if (role.PreCondition.Port?.IsAlive == false) // find predecessor
					await RecruitConnectedAgent(role, coalition, FindLastPredecessor);

				if (role.PostCondition.Port?.IsAlive == false) // find successor
					await RecruitConnectedAgent(role, coalition, FindFirstSuccessor);
			}

			// once predecessor / successor are in the coalition,
			// Coalition.InviteCtf() will fill the gaps in the CTF
			// (called in CalculateConfigurations()).

			// TODO surround empty predecessors/successors ?

			// coalition might have lost capabilities due to dead agents
			await RecruitMissingCapabilities(coalition);
		}

		/// <summary>
		/// Reconfiguration strategy for violations of the <see cref="Invariant.CapabilityConsistency"/> invariant predicate.
		/// </summary>
		protected async Task RecruitMissingCapabilities(Coalition coalition)
		{
			var availableCapabilities = new HashSet<ICapability>(
				coalition.Members.SelectMany(member => member.BaseAgentState.AvailableCapabilities)
			);

			foreach (var agent in new AgentQueue(coalition))
			{
				if (coalition.CTF.Capabilities.IsSubsetOf(availableCapabilities))
					break;

				var newMember = await coalition.Invite(agent);
				availableCapabilities.UnionWith(newMember.BaseAgentState.AvailableCapabilities);
			}
		}

		private async Task<Tuple<BaseAgent, Role>[]> FindDisconnectedRoles(Coalition coalition)
		{
			var affectedRoles = new List<Tuple<BaseAgent, Role>>();

			var members = coalition.Members.ToList(); // use list because coalition.Members is modified during iteration
			for (int i = 0; i < members.Count; ++i)
			{
				var agent = members[i].BaseAgent;
				foreach (var role in agent.AllocatedRoles)
				{
					// 1. invite disconnected agents (so their roles can be removed / updated)
					var affected = false;
					if (role.PreCondition.Port != null && !agent.Inputs.Contains(role.PreCondition.Port))
					{
						affected = true;
						if (!coalition.Contains(role.PreCondition.Port))
						{
							var newMember = await coalition.Invite(role.PreCondition.Port);
							members.Add(newMember);
						}
					}
					if (role.PostCondition.Port != null && !agent.Outputs.Contains(role.PostCondition.Port))
					{
						affected = true;
						if (!coalition.Contains(role.PostCondition.Port))
						{
							var newMember = await coalition.Invite(role.PostCondition.Port);
							members.Add(newMember);
						}
					}

					// 2. collect affected roles
					if (affected)
						affectedRoles.Add(Tuple.Create(agent, role));
				}
			}

			return affectedRoles.ToArray();
		}

		// used to recruit predecessor / successor of a role connected to a dead agent
		private async Task RecruitConnectedAgent(Role role, Coalition coalition,
			Func<Role, IEnumerable<Tuple<BaseAgent, Role>>, Tuple<BaseAgent, Role>> getConnectedRole)
		{
			var taggedRoles = coalition.Members.SelectMany(member =>
				member.BaseAgent.AllocatedRoles.Select(r => Tuple.Create(member.BaseAgent, r))
			);

			// 1) in coalition
			var agent = getConnectedRole(role, taggedRoles);
			if (agent == null)
			{
				// 2) by recruitment
				foreach (var newAgent in new AgentQueue(coalition).Reverse())
				{
					await coalition.Invite(newAgent);
					agent = getConnectedRole(role,
						newAgent.AllocatedRoles.Select(r => Tuple.Create(newAgent, r))
					);
					if (agent != null)
						break;
				}

				if (agent != null)
					await coalition.Invite(agent.Item1);
				// TODO: if agent still null (real successor/predecessor is unreachable) ?
			}
		}

		private Tuple<BaseAgent, Role> FindLastPredecessor(Role role, IEnumerable<Tuple<BaseAgent, Role>> possiblePredecessors)
		{
			return (from predecessor in possiblePredecessors
					let predRole = predecessor.Item2
					where predRole.Task == role.Task
						&& (predRole.PreCondition.StateLength < role.PreCondition.StateLength
								|| (role.CapabilitiesToApply.Count() > 0 && predRole.PreCondition.StateLength == role.PreCondition.StateLength))
					orderby predRole.PreCondition.StateLength descending
					select predecessor).FirstOrDefault();
		}

		private Tuple<BaseAgent, Role> FindFirstSuccessor(Role role, IEnumerable<Tuple<BaseAgent, Role>> possibleSuccessors)
		{
			return (from successor in possibleSuccessors
					let succRole = successor.Item2
					where succRole.Task == role.Task
						&& (succRole.PostCondition.StateLength > role.PostCondition.StateLength
							|| (role.CapabilitiesToApply.Count() > 0 && succRole.PostCondition.StateLength == role.PostCondition.StateLength))
					orderby succRole.PostCondition.StateLength ascending
					select successor).FirstOrDefault();
		}

		/// <summary>
		/// Lazily calculates all possible distributions of the capabilities in CTF between the members of the coalition.
		/// </summary>
		protected IEnumerable<BaseAgent[]> CalculateCapabilityDistributions(Coalition coalition)
		{
			// TODO: prefer distributions with minimal changes to previous distribution
			var distribution = new BaseAgent[coalition.CTF.Length];
			return CalculateCapabilityDistributions(coalition, distribution, 0);
		}

		// enumerate all paths, but lazily! (depth-first search)
		private IEnumerable<BaseAgent[]> CalculateCapabilityDistributions(Coalition coalition, BaseAgent[] distribution, int prefixLength)
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
			var eligibleAgents = coalition.Members
										  .Select(member => member.BaseAgent)
										  .Where(agent => CanSatisfyNext(agent, coalition, distribution, prefixLength));
			foreach (var agent in eligibleAgents)
			{
				distribution[prefixLength] = agent;
				foreach (var result in CalculateCapabilityDistributions(coalition, distribution, prefixLength + 1))
					yield return result;
			}
		}

		// TODO: override for pill production
		protected virtual bool CanSatisfyNext(BaseAgent agent, Coalition coalition, BaseAgent[] distribution, int prefixLength)
		{
			var capability = coalition.Task.RequiredCapabilities[coalition.CTF.Start + prefixLength];
			return agent.AvailableCapabilities.Contains(capability);
		}

		protected async Task<IEnumerable<BaseAgent>> ComputeResourceFlow(ConfigurationSuggestion configurationSuggestion)
		{
			var resourceFlow = new List<BaseAgent>();
			var agents = configurationSuggestion.AgentsToReconfigure;

			for (var i = 1; i < agents.Length; ++i)
			{
				var shortestPaths = ComputeShortestPaths(configurationSuggestion, agents[i - 1]);
				IEnumerable<BaseAgent> connection = shortestPaths.GetPathFromSource(destination: agents[i]);

				if (connection == null)
				{
					var canFindConnection = await RecruitResourceFlowAgentsForPath(configurationSuggestion.Coalition, shortestPaths, agents[i]);
					if (!canFindConnection)
						return null;

					connection = ComputeShortestPaths(configurationSuggestion, source: agents[i - 1]).GetPathFromSource(destination: agents[i]);
					Debug.Assert(connection != null);
				}

				if (resourceFlow.Count > 0)
					connection = connection.Skip(1); // connection.first == resourceFlow.last => don't duplicate it
				resourceFlow.AddRange(connection);
				configurationSuggestion.AddResourceFlowAgents(connection);
			}

			return resourceFlow;
		}

		private static ShortestPaths<BaseAgent> ComputeShortestPaths(ConfigurationSuggestion configurationSuggestion, BaseAgent source)
		{
			return ShortestPaths<BaseAgent>.Compute(
				source,
				agent => agent.Outputs.Where(neighbour => neighbour.Inputs.Contains(agent) && configurationSuggestion.Coalition.Contains(neighbour)),
				configurationSuggestion.EdgeWeight
			);
		}

		private async Task<bool> RecruitResourceFlowAgentsForPath(Coalition coalition, ShortestPaths<BaseAgent> shortestPathsFromSource , BaseAgent destination)
		{
			var visited = new HashSet<BaseAgent>();
			var stack = new Stack<BaseAgent>();
			stack.Push(destination);

			while (stack.Count > 0)
			{
				var agent = stack.Pop();
				if (visited.Contains(agent)) // avoid cycles
					continue;

				// recruit agent
				await coalition.Invite(agent);

				// Destination can be reached from agent (it was found by a reverse depth-first search starting from destination).
				// Agents reachable from source were not affected, unless recruited agents were themselves reachable from source.
				// Hence, once a recruited agent is reachable from source, a path from source to destination exists.
				if (shortestPathsFromSource.IsReachable(agent))
					return true;

				// push neighbours on stack
				foreach (var neighbour in agent.Inputs)
				{
					// TODO: prioritize agents participating in current task
					if (neighbour.Outputs.Contains(agent))
						stack.Push(neighbour);
				}

				visited.Add(agent);
			}

			return false;
		}

		protected ConfigurationUpdate ComputeRoleAllocations(ConfigurationSuggestion suggestion, BaseAgent[] resourceFlow)
		{
			Debug.Assert(resourceFlow.Length >= 2);
			var config = new ConfigurationUpdate();
			var task = suggestion.Coalition.Task;

			// clear old roles from core agents
			foreach (var agent in suggestion.CoreAgents)
			{
				var obsoleteRoles = agent.AllocatedRoles.Where(role => suggestion.TFR.Start <= role.PreCondition.StateLength
																	   && role.PostCondition.StateLength <= suggestion.TFR.End);
				config.RemoveRoles(agent, obsoleteRoles.ToArray());
			}

			var currentState = suggestion.TFR.Start;
			var offset = suggestion.Coalition.CTF.Start;
			var end = suggestion.TFR.End;

			// handle entry edge agent: set output port
			var previousRole = resourceFlow[0].AllocatedRoles
				.Single(role => role.PostCondition.StateLength == suggestion.TFR.Start);
			config.RemoveRoles(resourceFlow[0], previousRole); // remove old role
			previousRole.PostCondition.Port = resourceFlow[1];
			config.AddRoles(resourceFlow[0], previousRole); // re-add updated role

			// handle core agents: set capabilities & connections
			for (var i = 1; i < resourceFlow.Length - 1; ++i)
			{
				var agent = resourceFlow[i];
				var role = GetRole(task, resourceFlow[i - 1], previousRole.PostCondition);
				role.PostCondition.Port = resourceFlow[i + 1];

				while (currentState <= end && suggestion.CtfDistribution[currentState] == agent)
					role.AddCapability(task.RequiredCapabilities[offset + currentState++]);

				config.AddRoles(agent, role);
				previousRole = role;
			}

			// handle exit edge agent: set input port
			var exitEdgeAgent = resourceFlow[resourceFlow.Length - 1];
			var lastRole = exitEdgeAgent.AllocatedRoles
				.Single(role => role.PreCondition.StateLength == suggestion.TFR.End);
			config.RemoveRoles(exitEdgeAgent); // remove old role
			lastRole.PreCondition.Port = resourceFlow[resourceFlow.Length - 2];
			config.AddRoles(exitEdgeAgent, lastRole); // re-add updated role

			return config;
		}

		protected class ConfigurationSuggestion
		{
			public Coalition Coalition { get; }
			public BaseAgent[] CtfDistribution { get; }

			public TaskFragment TFR { get; private set; }
			private Role _firstRole;
			private Role _lastRole;

			public ISet<BaseAgent> EdgeAgents { get; private set; }
			private BaseAgent _startEdgeAgent;
			private BaseAgent _endEdgeAgent;
			public ISet<BaseAgent> CoreAgents { get; private set; }
			private readonly HashSet<BaseAgent> _resourceFlowAgents = new HashSet<BaseAgent>();

			public BaseAgent[] AgentsToReconfigure { get; private set; }

			public ConfigurationSuggestion(Coalition coalition, BaseAgent[] ctfDistribution)
			{
				Coalition = coalition;
				CtfDistribution = ctfDistribution;
			}

			/// <summary>
			/// Identifies the task fragment to reconfigure, i.e. the minimal subfragment of CTF
			/// where old and suggested new capability distribution differ.
			/// </summary>
			public void ComputeTFR()
			{
				// TODO: handle branches in resource flow (non-unique distribution, TFR start & end, edge agents)
				// TODO: handle TFR = []

				var ctf = Coalition.CTF;
				var oldDistribution = Coalition.RecoveredDistribution;

				// find first modified capability allocation (inside CTF)
				var tfrStart = ctf.Start;
				while (tfrStart < ctf.End && CtfDistribution[tfrStart - ctf.Start] == oldDistribution[tfrStart])
					tfrStart++;

				// find last modified capability allocation (inside CTF)
				var tfrEnd = ctf.End;
				while (tfrEnd > ctf.Start && CtfDistribution[tfrEnd - ctf.Start] == oldDistribution[tfrEnd])
					tfrEnd--;

				// find role containing the first and modified capability, respectively
				_firstRole = FindRoleForCapability(tfrStart);
				_lastRole = FindRoleForCapability(tfrEnd);

				TFR = new TaskFragment(Coalition.Task, tfrStart, tfrEnd);

				FindCoreAgents();
				FindEdgeAgents();

				AgentsToReconfigure = new[] { _startEdgeAgent } // edge agent preceeding TFR
					.Concat(CtfDistribution.Skip(TFR.Start - Coalition.CTF.Start).Take(TFR.Length)) // core agents
					.Concat(new[] { _endEdgeAgent }) // edge agent following TFR
					.Distinct()
					.Where(agent => agent != null)
					.ToArray();
			}

			private Role FindRoleForCapability(int capabilityIndex)
			{
				var agent = Coalition.RecoveredDistribution[capabilityIndex];
				return Enumerable.Range(0, capabilityIndex + 1) // possible start indices of the role
					.SelectMany(roleStart =>
								agent.AllocatedRoles.Where(role => role.PreCondition.StateLength == roleStart) // roles starting at the given index
					).Single(); // since branches in the resource flow are not permitted, there should be exactly one
			}

			private void FindCoreAgents()
			{
				// core agents are:
				//  (1) agents that will receive new roles, as indicated in CtfDistribution (restricted to TFR)
				//  (2) agents that will lose roles, because they either previously applied a capability in TFR
				//     or transported resources between such agents
				CoreAgents = new HashSet<BaseAgent>(
					CtfDistribution.Skip(TFR.Start - Coalition.CTF.Start).Take(TFR.Length) // (1)
				);

				// (2): go along the resource flow path, fron TFR.Start to TFR.End
				var current = Coalition.RecoveredDistribution[TFR.Start];
				var currentPos = TFR.Start;
				while (currentPos < TFR.End)
				{
					CoreAgents.Add(current);
					var currentRole = current.AllocatedRoles
											 .First(role => role.Task == Coalition.Task && role.PreCondition.StateLength == currentPos);
					currentPos = currentRole.PostCondition.StateLength;
					current = currentRole.PostCondition.Port;
				}
			}

			private void FindEdgeAgents()
			{
				// edge agents are agents that send/receive resources to/from the first/last role
				EdgeAgents = new HashSet<BaseAgent>();

				if (TFR.Start != 0)
				{
					_startEdgeAgent = _firstRole.PreCondition.Port;
					EdgeAgents.Add(_startEdgeAgent);
				}

				if (TFR.End != Coalition.Task.RequiredCapabilities.Length - 1)
				{
					_endEdgeAgent = _lastRole.PostCondition.Port;
					EdgeAgents.Add(_endEdgeAgent);
				}
			}

			/// <summary>
			/// Adds agents in <paramref name="connection"/> which are neither core nor edge agents to the set of resource flow agents.
			/// </summary>
			public void AddResourceFlowAgents(IEnumerable<BaseAgent> connection)
			{
				foreach (var agent in connection)
				{
					if (!CoreAgents.Contains(agent) && !EdgeAgents.Contains(agent))
						_resourceFlowAgents.Add(agent);
				}
			}

			internal int EdgeWeight(BaseAgent from, BaseAgent to)
			{
				if (CoreAgents.Contains(to) || EdgeAgents.Contains(to) || _resourceFlowAgents.Contains(to))
					return 1;
				return 2 * Coalition.Members.Count;
			}
		}
	}
}