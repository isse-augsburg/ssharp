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

				do
				{
					Debug.WriteLine("Calculating distributions");
					foreach (var distribution in CalculateCapabilityDistributions(coalition))
					{
						Debug.WriteLine("Distribution found");
						var reconfSuggestion = new ConfigurationSuggestion(coalition, distribution);

						// compute tfr, edge, core agents
						reconfSuggestion.ComputeReconfiguredTaskFragment(minTfr);
						Debug.WriteLine("Inviting edge agents");
						foreach (var edgeAgent in reconfSuggestion.EdgeAgents)
							await coalition.Invite(edgeAgent);

						// use dijkstra to find resource flow
						Debug.WriteLine("Computing resource flow");
						var resourceFlow = await ComputeResourceFlow(reconfSuggestion);
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
		/// <returns>A lazily computed sequence of distributions, so that each distribution's length equals the CTF length at that moment.</returns>
		private static IEnumerable<BaseAgent[]> CalculateCapabilityDistributions(Coalition coalition)
		{
			DistributionCalculator calculator;
			int agentCount;

			// keep track of previous distributions to prevent an endless loop
			var previousDistributions = new HashSet<BaseAgent[]>(ArrayEqualityComparer<BaseAgent>.Default);

			do
			{
				calculator = new DistributionCalculator(coalition.CTF, coalition.RecoveredDistribution, coalition.BaseAgents);
				agentCount = coalition.BaseAgents.Count;

				using (var enumerator = calculator.CalculateDistributions().GetEnumerator())
					while (enumerator.MoveNext() && calculator.Fragment.Equals(coalition.CTF) && coalition.BaseAgents.Count == agentCount)
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

		protected async Task<IEnumerable<BaseAgent>> ComputeResourceFlow(ConfigurationSuggestion configurationSuggestion)
		{
			var resourceFlow = new List<BaseAgent>();
			var agents = configurationSuggestion.AgentsToConnect;

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
					if (neighbour.IsAlive && neighbour.Outputs.Contains(agent))
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

			// initialize with no predecessor (ReconfiguredTaskFragment starts at 0, no entry edge agent exists)
			BaseAgent previousAgent = null;
			Condition? preCondition = null;

			var currentState = suggestion.ReconfiguredTaskFragment.Start;
			var firstNonEdgeAgent = suggestion.EntryEdgeAgent == null ? 0 : 1;
			var lastNonEdgeAgent = suggestion.ExitEdgeAgent == null ? resourceFlow.Length - 1 : resourceFlow.Length - 2;

			// handle entry edge agent: set output port, append role if necessary
			if (suggestion.EntryEdgeAgent != null)
			{
				var entryEdgeAgent = resourceFlow[0];
				Debug.Assert(entryEdgeAgent == suggestion.EntryEdgeAgent);

				var previousEntryRole = entryEdgeAgent.AllocatedRoles.Single(role =>
					role.Task == task && (role.IncludesCapability(suggestion.ReconfiguredTaskFragment.Start) || role.PostCondition.StateLength == suggestion.ReconfiguredTaskFragment.Start));
				config.RemoveRoles(entryEdgeAgent, previousEntryRole);

				var updatedRole = CollectCapabilities(suggestion, ref currentState, entryEdgeAgent, previousEntryRole)
					.WithOutput(resourceFlow[firstNonEdgeAgent]);
				config.AddRoles(entryEdgeAgent, updatedRole);

				Debug.Assert(updatedRole.PreCondition == previousEntryRole.PreCondition, "Entry agent's precondition must not change.");

				preCondition = updatedRole.PostCondition;
				previousAgent = entryEdgeAgent;
			}

			// handle core agents: set capabilities & connections
			for (var i = firstNonEdgeAgent; i <= lastNonEdgeAgent; ++i)
			{
				var agent = resourceFlow[i];
				var role = CollectCapabilities(suggestion, ref currentState, agent, GetRole(task, previousAgent, preCondition))
							.WithOutput(i < resourceFlow.Length - 1 ? resourceFlow[i + 1] : null);

				config.AddRoles(agent, role);
				previousAgent = agent;
				preCondition = role.PostCondition;
			}

			// handle exit edge agent: set input port, prepend role if necessary
			if (suggestion.ExitEdgeAgent != null)
			{
				var exitEdgeAgent = resourceFlow[resourceFlow.Length - 1];
				Debug.Assert(exitEdgeAgent == suggestion.ExitEdgeAgent);

				var previousExitRole = exitEdgeAgent.AllocatedRoles
											.Single(role => role.Task == task && (role.IncludesCapability(suggestion.ReconfiguredTaskFragment.End) || role.PreCondition.StateLength == suggestion.ReconfiguredTaskFragment.End + 1));
				config.RemoveRoles(exitEdgeAgent, previousExitRole); // remove old role

				// collect capabilities if edge agent is also core agent
				var initialRole = GetRole(task, previousAgent, preCondition);
				var updatedRole = CollectCapabilities(suggestion, ref currentState, exitEdgeAgent, initialRole)
					.AppendCapabilities(previousExitRole)
					.WithOutput(previousExitRole.Output);
				config.AddRoles(exitEdgeAgent, updatedRole); // re-add updated role

				Debug.Assert(updatedRole.PostCondition == previousExitRole.PostCondition, "Exit agent's postcondition must not change.");
			}

			// clear old roles from core agents
			foreach (var agent in suggestion.CoreAgents)
			{
				var obsoleteRoles = agent.AllocatedRoles.Where(suggestion.ReconfiguredTaskFragment.IntersectsWith);
				config.RemoveRoles(agent, obsoleteRoles.ToArray());
			}

			return config;
		}

		private static Role CollectCapabilities(ConfigurationSuggestion suggestion, ref int currentState, BaseAgent agent, Role initialRole)
		{
			var offset = suggestion.CTF.Start;
			var end = suggestion.ReconfiguredTaskFragment.End;

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

			public BaseAgent[] AgentsToConnect { get; private set; }

			[CanBeNull]
			public BaseAgent EntryEdgeAgent { get; private set; }
			[CanBeNull]
			public BaseAgent ExitEdgeAgent { get; private set; }

			public BaseAgent FirstCoreAgent => CtfDistribution[ReconfiguredTaskFragment.Start - CTF.Start];
			public BaseAgent LastCoreAgent => CtfDistribution[ReconfiguredTaskFragment.End - CTF.Start];


			public ConfigurationSuggestion(Coalition coalition, BaseAgent[] ctfDistribution)
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

				// populate agent sets
				CoreAgents.UnionWith(FindCoreAgents());
				FindEdgeAgents();

				AgentsToConnect = ComputeAgentsToConnect();

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

			private BaseAgent[] ComputeAgentsToConnect()
			{
				var agents = new List<BaseAgent>();

				var capabilityAgents = CtfDistribution.Slice(ReconfiguredTaskFragment.Start - CTF.Start, ReconfiguredTaskFragment.End - CTF.Start).ToArray();
				if (EntryEdgeAgent != null && capabilityAgents.Length > 0 && EntryEdgeAgent != capabilityAgents[0])
					agents.Add(EntryEdgeAgent);

				agents.AddRange(capabilityAgents);

				if (ExitEdgeAgent != null && capabilityAgents.Length > 0 && ExitEdgeAgent != capabilityAgents[capabilityAgents.Length - 1])
					agents.Add(ExitEdgeAgent);

				return agents.ToArray();
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
						currentRole = current.AllocatedRoles.First(role => role.Task == Coalition.Task && role.PreCondition.StateLength == currentPos);
						while (currentRole.Input != null && currentRole.Input.IsAlive)
						{
							current = currentRole.Input;
							currentPos = currentRole.PreCondition.StateLength;
							currentRole = current.AllocatedRoles.First(role => role.Task == Coalition.Task && role.PostCondition.StateLength == currentPos);
						}
						currentPos = currentRole.PreCondition.StateLength;
					}

					coreAgents.Add(current);
					currentRole = current.AllocatedRoles.First(role => role.Task == Coalition.Task && role.PreCondition.StateLength == currentPos);
					currentPos = currentRole.PostCondition.StateLength;
					current = currentRole.Output;
				}

				return coreAgents;
			}

			private void FindEdgeAgents()
			{
				// TODO: edge agents must always apply at least one capability

				if (ReconfiguredTaskFragment.Start > 0)
				{
					Debug.Assert(Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start] != null, "Dead agents should be surrounded in TFR.");

					if (CtfDistribution[ReconfiguredTaskFragment.Start - CTF.Start] == Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start])
						// first agent to apply a capability is the same -> can serve as edge agent
						EntryEdgeAgent = Coalition.RecoveredDistribution[ReconfiguredTaskFragment.Start];
					else
						// otherwise find first role in ReconfiguredTaskFragment (or after, if ReconfiguredTaskFragment is empty) in old distribution and use predecessor
						EntryEdgeAgent = FindRoleForCapability(ReconfiguredTaskFragment.Start).Input;

					Debug.Assert(EntryEdgeAgent != null && EntryEdgeAgent.IsAlive);
					EdgeAgents.Add(EntryEdgeAgent);
				}

				if (ReconfiguredTaskFragment.End < Coalition.Task.RequiredCapabilities.Length - 1)
				{
					Debug.Assert(Coalition.RecoveredDistribution[ReconfiguredTaskFragment.End] != null, "Dead agents should be surrounded in TFR.");

					if (CtfDistribution[ReconfiguredTaskFragment.End - CTF.Start] == Coalition.RecoveredDistribution[ReconfiguredTaskFragment.End])
						// last agent to apply a capability is the same -> can serve as edge agent
						ExitEdgeAgent = Coalition.RecoveredDistribution[ReconfiguredTaskFragment.End];
					else
						// otherwise find last role in ReconfiguredTaskFragment (or before, if ReconfiguredTaskFragment is empty) in old distribution and use successor
						ExitEdgeAgent = FindRoleForCapability(ReconfiguredTaskFragment.End).Output;

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