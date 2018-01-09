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
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using JetBrains.Annotations;

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
	    protected virtual IRecruitingStrategy NewTaskStrategy => CoalitionFormation.NewTaskStrategy.Instance;

		public CoalitionFormationController(BaseAgent[] agents) : base(agents)
		{
			Register(Invariant.CapabilityConsistency, MissingCapabilitiesStrategy.Instance);
			Register(Invariant.IoConsistency, BrokenIoStrategy.Instance);
			Register(Invariant.NeighborsAliveGuarantee, DeadNeighbourStrategy.Instance);
		}

		public void Register(InvariantPredicate predicate, IRecruitingStrategy strategy)
		{
			_strategies[predicate] = strategy;
		}

		public override Task<ConfigurationUpdate> CalculateConfigurationsAsync(object context, ITask task)
		{
			var leader = (CoalitionReconfigurationAgent)context;
			var violatedPredicates = (leader.ReconfigurationReason as ReconfigurationReason.InvariantsViolated)?.ViolatedPredicates
				?? new InvariantPredicate[0];
			var isInitialReconfiguration = leader.ReconfigurationReason is ReconfigurationReason.InitialReconfiguration;

			var coalition = new Coalition(leader, task, violatedPredicates, isInitialReconfiguration);
			return CalculateConfigurations(coalition);
		}

		protected async Task<ConfigurationUpdate> CalculateConfigurations(Coalition coalition)
		{
			Debug.WriteLine("Begin coalition-based reconfiguration");
			try
			{
				ConfigurationUpdate config;

				Debug.WriteLine("Recruiting necessary agents");
				var fragmentComputations = new List<Task<TaskFragment>>();
                // recruit for initial reconfigurations
                if (coalition.IsInitialConfiguration)
                    fragmentComputations.Add(NewTaskStrategy.RecruitNecessaryAgents(coalition));
                // recruit for violated predicates
			    fragmentComputations.AddRange(from predicate in coalition.ViolatedPredicates
			                                  select RecruitNecessaryAgents(coalition, predicate));
                // complete all recruitments
			    var fragments = await Task.WhenAll(fragmentComputations);
                // merge minimal fragments from all strategies
                var minTfr = TaskFragment.Merge(coalition.Task, fragments);

				Debug.WriteLine("Inviting CTF agents");
				coalition.MergeCtf(minTfr);
				await coalition.InviteCtfAgents();

				var connectionManager = new AgentConnectionManager();

				do
				{
					Debug.WriteLine("Calculating distributions");
					foreach (var distribution in CalculateCapabilityDistributions(coalition, connectionManager))
					{
						Debug.WriteLine("Distribution found");
						var reconfSuggestion = new ConfigurationSuggestion(coalition, distribution);

						// compute tfr, edge, core agents
						reconfSuggestion.ComputeReconfiguredTaskFragment(minTfr);

						Debug.WriteLine("Inviting edge agents");
						await reconfSuggestion.FindEdgeAgents();
						foreach (var edgeAgent in reconfSuggestion.EdgeAgents)
							await coalition.Invite(edgeAgent);

						// use dijkstra to find resource flow
						Debug.WriteLine("Computing resource flow");
						var resourceFlow = await ComputeResourceFlow(reconfSuggestion, connectionManager);
						Debug.WriteLine("ResourceFlow {0} found", (object)(resourceFlow == null ? "not" : "indeed"));
						if (resourceFlow != null)
						{
							Debug.WriteLine("Computing role allocations");
							config = ComputeRoleAllocations(reconfSuggestion, resourceFlow.ToArray());
                            config.RecordInvolvement(coalition.BaseAgents);
							OnConfigurationsCalculated(coalition.Task, config);
							Debug.WriteLine("Reconfiguration complete");
							return config;
						}
						Debug.WriteLine("No luck with this distribution.");
					}

					Debug.WriteLine("Inviting arbitrary neighbour");
                    // still no solution found: recruit an arbitrary known agent, try again
					if (coalition.HasNeighbours)
					{
						await coalition.InviteNeighbour();
						await coalition.InviteCtfAgents(); // CTF might have grown - make sure to fill all gaps if it has
					}

				} while (coalition.HasNeighbours);

				// failed to find a solution; no more neighbours to recruit - give up
			    config = FailedReconfiguration(coalition);
                OnConfigurationsCalculated(coalition.Task, config);
				return config;
			}
			catch (OperationCanceledException)
			{
				// operation was canceled (e.g. because coalition was merged into another coalition), so produce no updates
				Debug.WriteLine("Controller for coalition with leader {0} cancelled", coalition.Leader.BaseAgent.Id);
				return new ConfigurationUpdate();
			}
			catch (RestartReconfigurationException)
			{
				// restart reconfiguration, e.g. because another coalition was just merged into the current one
				Debug.WriteLine("Controller for coalition with leader {0} restarted", coalition.Leader.BaseAgent.Id);
				return await CalculateConfigurations(coalition);
			}
		}

		private ConfigurationUpdate FailedReconfiguration(Coalition coalition)
		{
			// At this point, all reachable agents will be in the coalition.
			// Otherwise the algorithm would try to recruit them instead of failing.

			var config = new ConfigurationUpdate();
			config.Fail();
			config.RemoveAllRoles(coalition.Task, coalition.BaseAgents.ToArray());
			return config;
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
		///   Semi-lazily calculates the best capability distributions for the CTF.
		///   Agents recruited or changes to the CTF between two yielded distributions are tacken into account.
		/// </summary>
		/// <param name="coalition">The coalition used to find distributions.</param>
		/// <param name="connectionOracle">Information about possible connections between agents.</param>
		/// <returns>A lazily computed sequence of distributions, so that each distribution's length equals the CTF length at that moment.</returns>
		private static IEnumerable<BaseAgent[]> CalculateCapabilityDistributions(Coalition coalition, IConnectionOracle connectionOracle)
		{
			DistributionCalculator calculator;
			int agentCount;

			// keep track of previous distributions to prevent an endless loop
			var previousDistributions = new HashSet<BaseAgent[]>(ArrayEqualityComparer<BaseAgent>.Default);

			do
			{
				calculator = new DistributionCalculator(coalition.CTF, coalition.RecoveredDistribution, coalition.BaseAgents, connectionOracle);
				agentCount = coalition.BaseAgents.Count;

				using (var enumerator = calculator.CalculateDistributions().GetEnumerator())
					while (calculator.Fragment.Equals(coalition.CTF) && coalition.BaseAgents.Count == agentCount && enumerator.MoveNext())
					{
						var distribution = enumerator.Current;
						if (previousDistributions.Add(distribution)) // only yield new distributions
							yield return distribution;
					}

				// If the fragment changes, the distribution length changes - there's no chance that previous distributions reoccur
				if (!calculator.Fragment.Equals(coalition.CTF))
					previousDistributions.Clear();

				// continue with new calculator if the fragment / the agents changed,
				// stop if calculator has no more (new) distributions
			} while (!calculator.Fragment.Equals(coalition.CTF) || coalition.BaseAgents.Count != agentCount);
		}

		protected async Task<IEnumerable<BaseAgent>> ComputeResourceFlow(ConfigurationSuggestion configurationSuggestion, AgentConnectionManager connectionManager)
		{
			var agents = configurationSuggestion.ComputeAgentsToConnect();
			Debug.Assert(agents.Length > 0);

			// don't waste time if no connection is possible
			if (connectionManager.ConnectionImpossible(agents))
				return null;

			var resourceFlow = new List<BaseAgent> { agents[0] };

			for (var i = 1; i < agents.Length; ++i)
			{
				var connection = connectionManager.GetConnection(agents[i], agents[i-1]);
				if (connection == null) // connection unknown, but might still exist
				{
					var shortestPaths = ComputeShortestPaths(configurationSuggestion, agents[i - 1]);
					connection = shortestPaths.GetPathFromSource(destination: agents[i]);

					if (connection == null) // connection impossible with current agents, but perhaps possible with additional agents
					{
						var canFindConnection = await RecruitResourceFlowAgentsForPath(configurationSuggestion.Coalition, shortestPaths, agents[i]);
						if (!canFindConnection) // no connection possible at all
						{
							connectionManager.RecordConnectionFailure(agents[i-1], agents[i]);
							return null;
						}

						// a connection is possible with the newly recruited agents - find it
						connection = ComputeShortestPaths(configurationSuggestion, source: agents[i - 1]).GetPathFromSource(destination: agents[i]);
					}

					Debug.Assert(connection != null);
					connectionManager.RecordConnection(agents[i-1], agents[i], connection);
				}

				var connectionSuffix = connection.Skip(1); // connection.first == resourceFlow.last => don't duplicate it
				resourceFlow.AddRange(connectionSuffix);
				configurationSuggestion.AddResourceFlowAgents(connectionSuffix);
			}

			Debug.Assert(resourceFlow.Count >= agents.Length);
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
					if (neighbour.IsAlive && neighbour.Outputs.Contains(agent))
						stack.Push(neighbour);
				}

				visited.Add(agent);
			}

			return false;
		}

		// TODO: refactor and move role allocation to own class
		// postcondition:
		//   result allocates each agent i in resourceFlow a role r_i with input resourceFlow[i-1] and output resourceFlow[i+1], if defined, and those have been allocated connecting roles.
		//   if suggestion.EntryEdgeAgent != null, it has a role r so that it previously had role s with r.precondition = s.precondition. s has been deallocated.
		//   if suggestion.ExitEdgeAgent != null, it has a role r so that it previously had a role s with r.postcondition = s.postcondition. s has been deallocated.
		//   (r_0 + r_1 + ... + r_n).capabilities = suggestion.TFR.capabilities, where n = resourceFlow.Length
		//   if c = r_i.capabilities[k], then suggestion.CtfDistribution[suggestion.TFR.Start - suggestion.CTF.Start + r_i.precondition.stateLength + k] = resourceFlow[i]
		protected ConfigurationUpdate ComputeRoleAllocations(ConfigurationSuggestion suggestion, BaseAgent[] resourceFlow)
		{
			// preconditions
			Debug.Assert(resourceFlow.Length >= 1);
			Debug.Assert(suggestion.EntryEdgeAgent == null || suggestion.EntryEdgeAgent == resourceFlow[0]);
			Debug.Assert(suggestion.ExitEdgeAgent == null || suggestion.ExitEdgeAgent == resourceFlow[resourceFlow.Length - 1]);

			var task = suggestion.Coalition.Task;
			var config = new ConfigurationUpdate();
			var roles = new Role?[resourceFlow.Length];

			// handle entry edge agent: set input, add capabilities before ReconfiguredTaskFragment
			if (suggestion.EntryEdgeAgent != null)
			{
				var previousEntryRole = suggestion.EntryEdgeAgent.AllocatedRoles.Single(role =>
					role.Task == task && (role.IncludesCapability(suggestion.ReconfiguredTaskFragment.Start) || suggestion.ReconfiguredTaskFragment.Succeedes(role)));
				config.RemoveRoles(suggestion.EntryEdgeAgent, previousEntryRole);

				var prefixCapabilities = previousEntryRole.CapabilitiesToApply.Take(suggestion.ReconfiguredTaskFragment.Start - previousEntryRole.PreCondition.StateLength);
				roles[0] = Role.Empty(previousEntryRole.PreCondition).WithCapabilities(prefixCapabilities);
			}

			var currentState = suggestion.ReconfiguredTaskFragment.Start;
			for (var i = 0; i < resourceFlow.Length; ++i)
			{
				var lastRole = i == 0 ? null : roles[i - 1];
				var lastAgent = i == 0 ? null : resourceFlow[i - 1];
				var nextAgent = i + 1 < resourceFlow.Length ? resourceFlow[i + 1] : null;
				var agent = resourceFlow[i];

				var initialRole = roles[i] ?? GetRole(task, lastAgent, lastRole?.PostCondition);
				roles[i] = CollectCapabilities(suggestion, ref currentState, agent, initialRole).WithOutput(nextAgent);
			}
			Debug.Assert(currentState == suggestion.ReconfiguredTaskFragment.End + 1, "Every capability in the ReconfiguredTaskFragment must be included in a role.");

			// handle exit edge agent: set ouput, add capabilities after ReconfiguredTaskFragment
			if (suggestion.ExitEdgeAgent != null)
			{
				var previousExitRole = suggestion.ExitEdgeAgent.AllocatedRoles.Single(role =>
					role.Task == task && (role.IncludesCapability(suggestion.ReconfiguredTaskFragment.End) || suggestion.ReconfiguredTaskFragment.Precedes(role)));
				config.RemoveRoles(suggestion.ExitEdgeAgent, previousExitRole);

				var initialRole = roles[roles.Length - 1];
				Debug.Assert(initialRole.HasValue, "Uninitialized role encountered!");

				var suffixCapabilities = previousExitRole.CapabilitiesToApply.Skip(suggestion.ReconfiguredTaskFragment.End + 1 - previousExitRole.PreCondition.StateLength);
				roles[roles.Length - 1] = initialRole.Value.WithCapabilities(suffixCapabilities).WithOutput(previousExitRole.Output);

				Debug.Assert(roles[roles.Length - 1].Value.PostCondition == previousExitRole.PostCondition, "Exit agent's postcondition must not change.");
			}

			// allocate new roles
			for (var i = 0; i < resourceFlow.Length; ++i)
			{
				Debug.Assert(roles[i].HasValue, "Uninitialized role encountered!");
				config.AddRoles(resourceFlow[i], roles[i].Value);
			}
			// clear old roles from core agents
			foreach (var agent in suggestion.CoreAgents)
			{
				var obsoleteRoles = agent.AllocatedRoles.Where(suggestion.ReconfiguredTaskFragment.IntersectsWith);
				config.RemoveRoles(agent, obsoleteRoles.ToArray());
			}
			// clear old transport roles
			foreach (var agentRoleGroup in suggestion.EdgeTransportRoles.GroupBy(t => t.Item1))
				config.RemoveRoles(agentRoleGroup.Key, agentRoleGroup.Select(t => t.Item2).ToArray());

			return config;
		}

		private static Role CollectCapabilities(ConfigurationSuggestion suggestion, ref int currentState, BaseAgent agent, Role initialRole)
		{
			var offset = suggestion.CTF.Start;
			var end = suggestion.ReconfiguredTaskFragment.End;
			Debug.Assert(initialRole.PostCondition.StateLength == currentState);

			var role = initialRole;
			while (currentState <= end && suggestion.CtfDistribution[currentState - offset] == agent)
				role = role.WithCapability(suggestion.Coalition.Task.RequiredCapabilities[currentState++]);

			return role;
		}

		protected class ConfigurationSuggestion
		{
			public Coalition Coalition { get; }
			public BaseAgent[] CtfDistribution { get; }

			public TaskFragment CTF { get; }
			public TaskFragment ReconfiguredTaskFragment { get; private set; }

			public ISet<BaseAgent> CoreAgents { get; } = new HashSet<BaseAgent>();
			public ISet<BaseAgent> EdgeAgents { get; } = new HashSet<BaseAgent>();

			private readonly HashSet<BaseAgent> _resourceFlowAgents = new HashSet<BaseAgent>();

			[CanBeNull]
			public BaseAgent EntryEdgeAgent { get; private set; }
			[CanBeNull]
			public BaseAgent ExitEdgeAgent { get; private set; }

			public BaseAgent FirstCoreAgent => CtfDistribution[ReconfiguredTaskFragment.Start - CTF.Start];
			public BaseAgent LastCoreAgent => CtfDistribution[ReconfiguredTaskFragment.End - CTF.Start];

			public List<Tuple<BaseAgent, Role>> EdgeTransportRoles { get; } = new List<Tuple<BaseAgent, Role>>();


			public ConfigurationSuggestion(Coalition coalition, [NotNull, ItemNotNull] BaseAgent[] ctfDistribution)
			{
				Coalition = coalition;
				CTF = coalition.CTF;
				CtfDistribution = ctfDistribution;

				Debug.Assert(CTF.Length == CtfDistribution.Length);
			}

			/// <summary>
			/// Identifies the task fragment to reconfigure, i.e. the minimal subfragment of CTF
			/// where old and suggested new capability distribution differ.
			/// </summary>
			public void ComputeReconfiguredTaskFragment(TaskFragment minimalTfr)
			{
				// TODO: handle branches in resource flow (non-unique distribution);

				// extend ReconfiguredTaskFragment with modified capability allocations
				ReconfiguredTaskFragment = ModifiedTaskFragment().Merge(minimalTfr);

				// populate core agents
				CoreAgents.UnionWith(FindCoreAgents());

				Debug.WriteLine($"ReconfiguredTaskFragment: [{ReconfiguredTaskFragment.Start}, {ReconfiguredTaskFragment.End}]");
			}

			private TaskFragment ModifiedTaskFragment()
			{
				var oldDistribution = Coalition.RecoveredDistribution;
				var modifiedIndices = CTF.CapabilityIndices.Where(i => CtfDistribution[i - CTF.Start] != oldDistribution[i]).ToArray(); // includes oldDistribution[i] == null because agent dead

				if (modifiedIndices.Length > 0)
					return new TaskFragment(Coalition.Task, modifiedIndices[0], modifiedIndices[modifiedIndices.Length - 1]);
				return TaskFragment.Identity(Coalition.Task);
			}

			[NotNull, ItemNotNull]
			public BaseAgent[] ComputeAgentsToConnect()
			{
				var agents = new List<BaseAgent>();

				if (EntryEdgeAgent != null)
					agents.Add(EntryEdgeAgent);

				agents.AddRange(from index in ReconfiguredTaskFragment.CapabilityIndices
								let ctfIndex = index - CTF.Start
								select CtfDistribution[ctfIndex]);

				if (ExitEdgeAgent != null)
					agents.Add(ExitEdgeAgent);

				return agents.Where((agent, i) => i == 0 || !Equals(agents[i-1], agent)).ToArray();
			}

			[NotNull, MustUseReturnValue]
			private ISet<BaseAgent> FindCoreAgents()
			{
				// core agents are:
				//  (1) agents that will receive new roles, as indicated in CtfDistribution (restricted to ReconfiguredTaskFragment)
				//  (2) agents that will lose roles, because they either previously applied a capability in ReconfiguredTaskFragment
				//      or transported resources between such agents

				var coreAgents = new HashSet<BaseAgent>(
					CtfDistribution.Slice(ReconfiguredTaskFragment.Start - CTF.Start, ReconfiguredTaskFragment.End - CTF.Start) // (1)
				);

				// (2): go along the resource flow path, from ReconfiguredTaskFragment.Start to ReconfiguredTaskFragment.End
				var current = Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start];
				var currentPos = ReconfiguredTaskFragment.Start;
				while (currentPos <= ReconfiguredTaskFragment.End)
				{
					Role currentRole;

					if (current == null || !current.IsAlive)
					{
						// if agent is dead: find next known agent that isn't
						while ((current == null || !current.IsAlive) && currentPos <= ReconfiguredTaskFragment.End)
						{
							currentPos++;
							if (currentPos <= ReconfiguredTaskFragment.End)
								current = Coalition.RecoveredDistribution[currentPos];
						}
						if (currentPos > ReconfiguredTaskFragment.End)
							break; // all further agents are dead
						Debug.Assert(current != null && current.IsAlive);
						
						// otherwise, go back until just after dead agent
						currentRole = current.AllocatedRoles.Single(role => role.Task == Coalition.Task && (role.IncludesCapability(currentPos) || role.PreCondition.StateLength == currentPos));
						while (currentRole.Input != null && currentRole.Input.IsAlive)
						{
							current = currentRole.Input;
							currentPos = currentRole.PreCondition.StateLength;
							currentRole = current.AllocatedRoles.Single(role => role.Task == Coalition.Task && role.PostCondition.StateLength == currentPos);
						}
						currentPos = currentRole.PreCondition.StateLength;
					}

					CoreAgents.Add(current);
					currentRole = current.AllocatedRoles.Single(role => role.Task == Coalition.Task && (role.IncludesCapability(currentPos) || role.PreCondition.StateLength == currentPos));
					currentPos = currentRole.PostCondition.StateLength;
					current = currentRole.Output;
				}

				return coreAgents;
			}

			public async Task FindEdgeAgents()
			{
				if (ReconfiguredTaskFragment.Start > 0)
				{
					Debug.Assert(Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start] != null, "Dead agents should be surrounded in TFR.");

					if (CtfDistribution[ReconfiguredTaskFragment.Start - CTF.Start] == Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start])
						// first agent to apply a capability is the same -> can serve as edge agent
						EntryEdgeAgent = Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start];
					else // otherwise agent that applied capability ReconfiguredTaskFragment.Start-1 is chosen as edge agent
					{
						var currentAgent = Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start];
						var currentRole = FindRoleForCapability(ReconfiguredTaskFragment.Start);

						while (!currentRole.IncludesCapability(ReconfiguredTaskFragment.Start - 1))
						{
							EdgeTransportRoles.Add(Tuple.Create(currentAgent, currentRole));

							var nextAgent = currentRole.Input;
							Debug.Assert(nextAgent != null && nextAgent.IsAlive); // otherwise coalition merge would have occurred.
							await Coalition.Invite(nextAgent);

							currentRole = nextAgent.AllocatedRoles.Single(ReconfiguredTaskFragment.Succeedes);
							currentAgent = nextAgent;
						}

						EntryEdgeAgent = currentAgent;
					}

					Debug.Assert(EntryEdgeAgent != null && EntryEdgeAgent.IsAlive);
					EdgeAgents.Add(EntryEdgeAgent);
				}

				if (ReconfiguredTaskFragment.End < Coalition.Task.RequiredCapabilities.Length - 1)
				{
					Debug.Assert(Coalition.RecoveredDistribution[ReconfiguredTaskFragment.End] != null, "Dead agents should be surrounded in TFR.");

					if (CtfDistribution[ReconfiguredTaskFragment.End - CTF.Start] == Coalition.RecoveredDistribution[ReconfiguredTaskFragment.End])
						// last agent to apply a capability is the same -> can serve as edge agent
						ExitEdgeAgent = Coalition.RecoveredDistribution[ReconfiguredTaskFragment.End];
					else // otherwise agent that applied capability ReconfiguredTaskFragment.End+1 is chosen as edge agent
					{
						var currentAgent = Coalition.RecoveredDistribution[ReconfiguredTaskFragment.End];
						var currentRole = FindRoleForCapability(ReconfiguredTaskFragment.End);

						while (!currentRole.IncludesCapability(ReconfiguredTaskFragment.End + 1))
						{
							EdgeTransportRoles.Add(Tuple.Create(currentAgent, currentRole));

							var nextAgent = currentRole.Output;
							Debug.Assert(nextAgent != null && nextAgent.IsAlive); // otherwise coalition merge would have occurred.
							await Coalition.Invite(nextAgent);

							currentRole = nextAgent.AllocatedRoles.Single(ReconfiguredTaskFragment.Precedes);
							currentAgent = nextAgent;
						}

						ExitEdgeAgent = currentAgent;
					}

					Debug.Assert(ExitEdgeAgent != null && ExitEdgeAgent.IsAlive);
					EdgeAgents.Add(ExitEdgeAgent);
				}
			}

			private Role FindRoleForCapability(int capabilityIndex)
			{
				return Coalition.RecoveredDistribution[capabilityIndex]
								.AllocatedRoles
								.Single(role => role.Task == Coalition.Task && role.IncludesCapability(capabilityIndex));
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