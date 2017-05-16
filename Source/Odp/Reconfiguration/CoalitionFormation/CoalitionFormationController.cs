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
	public class CoalitionFormationController : AbstractController
	{
		private readonly Dictionary<InvariantPredicate, IRecruitingStrategy> _strategies = new Dictionary<InvariantPredicate, IRecruitingStrategy>();

		public CoalitionFormationController(BaseAgent[] agents) : base(agents)
		{
			Register(Invariant.CapabilityConsistency, MissingCapabilitiesStrategy.Instance);
			Register(Invariant.IOConsistency, BrokenIoStrategy.Instance);
			Register(Invariant.NeighborsAliveGuarantee, DeadNeighbourStrategy.Instance);
		}

		public void Register(InvariantPredicate predicate, IRecruitingStrategy strategy)
		{
			_strategies[predicate] = strategy;
		}

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
				var minTfr = TaskFragment.Merge(await Task.WhenAll(from predicate in coalition.ViolatedPredicates
																   select RecruitNecessaryAgents(coalition, predicate)));

				await coalition.InviteCtfAgents();

				do
				{
					foreach (var distribution in CalculateCapabilityDistributions(coalition))
					{
						var reconfSuggestion = new ConfigurationSuggestion(coalition, distribution);

						// compute tfr, edge, core agents
						reconfSuggestion.ComputeTfr(minTfr);
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
		private Task<TaskFragment> RecruitNecessaryAgents(Coalition coalition, InvariantPredicate invariant)
		{
			if (!_strategies.ContainsKey(invariant))
				throw new InvalidOperationException("no recruiting strategy specified for invariant predicate");
			return _strategies[invariant].RecruitNecessaryAgents(coalition);
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
			var agents = configurationSuggestion.AgentsToConnect;

			for (var i = 1; i < agents.Count; ++i)
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
																	   || role.PostCondition.StateLength <= suggestion.TFR.End);
				config.RemoveRoles(agent, obsoleteRoles.ToArray());
			}

			var previousRole = default(Role);
			int firstCoreAgent = 0;
			if (!suggestion.CoreAgents.Contains(resourceFlow[0]))
			{
				// handle entry edge agent: set output port
				previousRole = resourceFlow[0].AllocatedRoles
												  .Single(role => role.PostCondition.StateLength == suggestion.TFR.Start);
				config.RemoveRoles(resourceFlow[0], previousRole); // remove old role
				previousRole.PostCondition.Port = resourceFlow[1];
				config.AddRoles(resourceFlow[0], previousRole); // re-add updated role

				firstCoreAgent++;
			}

			int lastCoreAgent = resourceFlow.Length - 1;
			if (!suggestion.CoreAgents.Contains(resourceFlow[resourceFlow.Length - 1]))
			{
				// handle exit edge agent: set input port
				var exitEdgeAgent = resourceFlow[resourceFlow.Length - 1];
				var lastRole = exitEdgeAgent.AllocatedRoles
											.Single(role => role.PreCondition.StateLength == suggestion.TFR.End);
				config.RemoveRoles(exitEdgeAgent, lastRole); // remove old role
				lastRole.PreCondition.Port = resourceFlow[resourceFlow.Length - 2];
				config.AddRoles(exitEdgeAgent, lastRole); // re-add updated role

				lastCoreAgent--;
			}

			// handle core agents: set capabilities & connections
			var currentState = suggestion.TFR.Start;
			var offset = suggestion.CTF.Start;
			var end = suggestion.TFR.End;

			for (var i = firstCoreAgent; i <= lastCoreAgent; ++i)
			{
				var agent = resourceFlow[i];
				var role = GetRole(task, i > 0 ? resourceFlow[i - 1] : null, i > 0 ? (Condition?)previousRole.PostCondition : null);
				role.PostCondition.Port = i < resourceFlow.Length - 1 ? resourceFlow[i + 1] : null;

				while (currentState <= end && suggestion.CtfDistribution[currentState - offset] == agent)
					role.AddCapability(task.RequiredCapabilities[offset + currentState++]);

				config.AddRoles(agent, role);
				previousRole = role;
			}

			return config;
		}

		protected class ConfigurationSuggestion
		{
			public Coalition Coalition { get; }
			public BaseAgent[] CtfDistribution { get; }

			public TaskFragment CTF { get; }
			public TaskFragment TFR { get; private set; }

			public ISet<BaseAgent> CoreAgents { get; } = new HashSet<BaseAgent>();
			public ISet<BaseAgent> EdgeAgents { get; } = new HashSet<BaseAgent>();
			private readonly HashSet<BaseAgent> _resourceFlowAgents = new HashSet<BaseAgent>();

			public List<BaseAgent> AgentsToConnect { get; } = new List<BaseAgent>();

			public ConfigurationSuggestion(Coalition coalition, BaseAgent[] ctfDistribution)
			{
				Coalition = coalition;
				CTF = coalition.CTF.Copy();
				CtfDistribution = ctfDistribution;
			}

			/// <summary>
			/// Identifies the task fragment to reconfigure, i.e. the minimal subfragment of CTF
			/// where old and suggested new capability distribution differ.
			/// </summary>
			public void ComputeTfr(TaskFragment minimalTfr)
			{
				// TODO: handle branches in resource flow (non-unique distribution, TFR start & end, edge agents)

				var offset = CTF.Start;
				var oldDistribution = Coalition.RecoveredDistribution;

				// extend TFR with modified capability allocations
				TFR = minimalTfr.Copy();
				for (var i = CTF.Start; i <= CTF.End; ++i)
					if (CtfDistribution[i - CTF.Start] != oldDistribution[i])
						TFR.Add(i);

				// round TFR to role boundaries (include capabilities applied by same agent, either before or after reconfiguration)
				var start = TFR.Start;
				while (start > CTF.Start && (CtfDistribution[start - 1 - offset] == CtfDistribution[start - offset] || oldDistribution[start - 1] == oldDistribution[start]))
					start--;
				TFR.Prepend(start);

				var end = TFR.End;
				while (end < CTF.End && (CtfDistribution[end + 1 - offset] == CtfDistribution[end - offset] || oldDistribution[end + 1] == oldDistribution[end]))
					end++;
				TFR.Append(end);

				// populate agent sets
				BaseAgent startEdgeAgent, endEdgeAgent;
				FindCoreAgents();
				FindEdgeAgents(out startEdgeAgent, out endEdgeAgent);

				// compute sequence of agents to be connected by resource flow
				if (startEdgeAgent != null)
					AgentsToConnect.Add(startEdgeAgent);

				var coreAgents = CtfDistribution.Slice(TFR.Start - offset, TFR.End - offset).ToArray();
				AgentsToConnect.AddRange(coreAgents.Where((t, i) => i == 0 || coreAgents[i - 1] != t));

				if (endEdgeAgent != null)
					AgentsToConnect.Add(endEdgeAgent);
			}

			private void FindCoreAgents()
			{
				// core agents are:
				//  (1) agents that will receive new roles, as indicated in CtfDistribution (restricted to TFR)
				//  (2) agents that will lose roles, because they either previously applied a capability in TFR
				//      or transported resources between such agents

				CoreAgents.UnionWith(
					CtfDistribution.Slice(TFR.Start - CTF.Start, TFR.End - CTF.Start) // (1)
				);

				// (2): go along the resource flow path, from TFR.Start to TFR.End
				var current = Coalition.RecoveredDistribution[TFR.Start];
				var currentPos = TFR.Start;
				while (currentPos <= TFR.End)
				{
					CoreAgents.Add(current);
					var currentRole = current.AllocatedRoles
											 .First(role => role.Task == Coalition.Task && role.PreCondition.StateLength == currentPos);
					currentPos = currentRole.PostCondition.StateLength;
					current = currentRole.PostCondition.Port;
				}
			}

			private void FindEdgeAgents(out BaseAgent startEdgeAgent, out BaseAgent endEdgeAgent)
			{
				startEdgeAgent = endEdgeAgent = null;

				if (TFR.Start > 0)
				{
					// find first role in TFR (or after, if TFR is empty) in old distribution
					var firstRole = FindRoleForCapability(TFR.Start);
					startEdgeAgent = firstRole.PreCondition.Port;
					EdgeAgents.Add(startEdgeAgent);
				}

				if (TFR.End < Coalition.Task.RequiredCapabilities.Length - 1)
				{
					// find last role in TFR (or before, if TFR is empty) in old distribution
					var lastRole = FindRoleForCapability(TFR.End);
					endEdgeAgent = lastRole.PostCondition.Port;
					EdgeAgents.Add(endEdgeAgent);
				}
			}

			private Role FindRoleForCapability(int capabilityIndex)
			{
				return Coalition.RecoveredDistribution[capabilityIndex]
								.AllocatedRoles.Single(role => role.Task == Coalition.Task &&
															   role.PreCondition.StateLength <= capabilityIndex &&
															   capabilityIndex <= role.PostCondition.StateLength);
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