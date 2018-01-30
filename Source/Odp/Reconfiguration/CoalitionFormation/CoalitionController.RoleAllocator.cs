// The MIT License (MIT)
//
// Copyright (c) 2014-2018, Institute for Software & Systems Engineering
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

	partial class CoalitionController
	{
		private class RoleAllocator
		{
			public static Task<ConfigurationUpdate> Allocate(TaskFragment fragment, Configuration solution, Coalition coalition, TaskFragment minimalReconfiguredFragment)
			{
				return new RoleAllocator(fragment, solution, coalition, minimalReconfiguredFragment).AllocateRoles();
			}

			private readonly TaskFragment _fragment;
			private readonly Configuration _solution;
			private readonly Coalition _coalition;
			private readonly TaskFragment _reconfiguredFragment;

			private ITask Task => _fragment.Task;
			private BaseAgent[] OldDistribution => _coalition.RecoveredDistribution;
			private BaseAgent[] NewDistribution => _solution.Distribution;
			private BaseAgent[] ResourceFlow => _solution.ResourceFlow;
			
			private RoleAllocator(TaskFragment fragment, Configuration solution, Coalition coalition, TaskFragment minimalReconfiguredFragment)
			{
				_fragment = fragment;
				_solution = solution;
				_coalition = coalition;

				_reconfiguredFragment = ModifiedFragment().Merge(minimalReconfiguredFragment);
			}

			private async Task<ConfigurationUpdate> AllocateRoles()
			{
				var tuple1 = await FindEntryEdgeAgent();
				var entryEdgeAgent = tuple1.Item1;
				Debug.Assert(entryEdgeAgent == null || entryEdgeAgent.IsAlive);

				var tuple2 = await FindExitEdgeAgent();
				var exitEdgeAgent = tuple2.Item1;
				Debug.Assert(exitEdgeAgent == null || exitEdgeAgent.IsAlive);

				var edgeTransportRoles = tuple1.Item2.Concat(tuple2.Item2).ToArray();

				// restrict resource flow to the part relevant for TFR
				var restrictedResourceFlow = RestrictResourceFlow(entryEdgeAgent, exitEdgeAgent);

				return ReconfigureTFR(restrictedResourceFlow, entryEdgeAgent, exitEdgeAgent, edgeTransportRoles);
			}

			private ConfigurationUpdate ReconfigureTFR(BaseAgent[] resourceFlow, BaseAgent entryEdgeAgent, BaseAgent exitEdgeAgent, Role.Allocation[] edgeTransportRoles)
			{
				// preconditions
				Debug.Assert(resourceFlow.Length >= 1);
				Debug.Assert(entryEdgeAgent == null || entryEdgeAgent == resourceFlow[0]);
				Debug.Assert(exitEdgeAgent == null || exitEdgeAgent == resourceFlow[resourceFlow.Length - 1]);

				var config = new ConfigurationUpdate();
				var roles = new Role?[resourceFlow.Length];

				// handle entry edge agent: set input, add capabilities before ReconfiguredTaskFragment
				if (entryEdgeAgent != null)
				{
					var previousEntryRole = entryEdgeAgent.AllocatedRoles.Single(role =>
						role.Task == Task && (role.IncludesCapability(_reconfiguredFragment.Start) || _reconfiguredFragment.Succeedes(role)));
					config.RemoveRoles(entryEdgeAgent, previousEntryRole);

					var prefixCapabilities = previousEntryRole.CapabilitiesToApply.Take(_reconfiguredFragment.Start - previousEntryRole.PreCondition.StateLength);
					roles[0] = Role.Empty(previousEntryRole.PreCondition).WithCapabilities(prefixCapabilities);
				}

				var currentState = _reconfiguredFragment.Start;
				for (var i = 0; i < resourceFlow.Length; ++i)
				{
					var lastRole = i == 0 ? null : roles[i - 1];
					var lastAgent = i == 0 ? null : resourceFlow[i - 1];
					var nextAgent = i + 1 < resourceFlow.Length ? resourceFlow[i + 1] : null;
					var agent = resourceFlow[i];

					var initialRole = roles[i] ?? GetRole(Task, lastAgent, lastRole?.PostCondition);
					roles[i] = CollectCapabilities(ref currentState, agent, initialRole).WithOutput(nextAgent);
				}
				Debug.Assert(currentState == _reconfiguredFragment.End + 1, "Every capability in the ReconfiguredTaskFragment must be included in a role.");

				// handle exit edge agent: set ouput, add capabilities after ReconfiguredTaskFragment
				if (exitEdgeAgent != null)
				{
					var previousExitRole = exitEdgeAgent.AllocatedRoles.Single(role =>
						role.Task == Task && (role.IncludesCapability(_reconfiguredFragment.End) || _reconfiguredFragment.Precedes(role)));
					config.RemoveRoles(exitEdgeAgent, previousExitRole);

					var initialRole = roles[roles.Length - 1];
					Debug.Assert(initialRole.HasValue, "Uninitialized role encountered!");

					var suffixCapabilities = previousExitRole.CapabilitiesToApply.Skip(_reconfiguredFragment.End + 1 - previousExitRole.PreCondition.StateLength);
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
				foreach (var agent in FindCoreAgents())
				{
					var obsoleteRoles = agent.AllocatedRoles.Where(_reconfiguredFragment.IntersectsWith);
					config.RemoveRoles(agent, obsoleteRoles.ToArray());
				}
				// clear old transport roles
				foreach (var agentRoleGroup in edgeTransportRoles.GroupBy(t => t.Agent))
					config.RemoveRoles(agentRoleGroup.Key, agentRoleGroup.Select(t => t.Role).ToArray());

				return config;
			}

			private Role CollectCapabilities(ref int currentState, BaseAgent agent, Role initialRole)
			{
				var offset = _fragment.Start;
				var end = _reconfiguredFragment.End;
				Debug.Assert(initialRole.PostCondition.StateLength == currentState);

				var role = initialRole;
				while (currentState <= end && NewDistribution[currentState - offset] == agent)
					role = role.WithCapability(Task.RequiredCapabilities[currentState++]);

				return role;
			}

			private TaskFragment ModifiedFragment()
			{
				// includes OldDistribution[i] == null because agent dead
				var modifiedIndices = _fragment.CapabilityIndices.Where(i => NewDistribution[i - _fragment.Start] != OldDistribution[i]).ToArray();

				if (modifiedIndices.Length > 0)
					return new TaskFragment(Task, modifiedIndices[0], modifiedIndices[modifiedIndices.Length - 1]);
				return TaskFragment.Identity(Task);
			}

			private async Task<Tuple<BaseAgent, Role.Allocation[]>> FindEntryEdgeAgent()
			{
				// TFR includes first capability - no edge agent needed
				if (_reconfiguredFragment.Start == 0)
					return Tuple.Create((BaseAgent)null, new Role.Allocation[0]);

				Debug.Assert(OldDistribution[_reconfiguredFragment.Start] != null, "Dead agents should be surrounded in TFR.");

				// first agent to apply a capability is the same -> can serve as edge agent
				if (NewDistribution[_reconfiguredFragment.Start - _fragment.Start] == OldDistribution[_reconfiguredFragment.Start])
					return Tuple.Create(OldDistribution[_reconfiguredFragment.Start], new Role.Allocation[0]);

				// Otherwise we need an agent we know is connected to whoever applies capability TFR.Start-1.
				// If TFR.Start > CTF.Start, capability TFR.Start-1 was and is applied by an agent in ResourceFlow - choose it!
				if (_reconfiguredFragment.Start > _fragment.Start)
				{
					var edgeAgent = OldDistribution[_reconfiguredFragment.Start - 1];

					// Find roles that transport resources between edge agent and TFR
					var edgeTransportRoles = ResourceFlowWalker
						.WalkBackward(OldDistribution[_reconfiguredFragment.Start], FindRoleForCapability(_reconfiguredFragment.Start))
						.TakeWhile(allocation => !allocation.Role.IncludesCapability(_reconfiguredFragment.Start-1))
						.ToArray();

					return Tuple.Create(edgeAgent, edgeTransportRoles);
				}

				Debug.Assert(_reconfiguredFragment.Start == _fragment.Start);
				Debug.Assert(ResourceFlow[0] == OldDistribution[_reconfiguredFragment.Start]);

				// Otherwise TFR.Start-1 is outside of the fragment.
				// We know the agent that applies it is connected to ResourceFlow[0], and ResourceFlow connects ResourceFlow[0] to TFR.
				// But we need the concatenated connection (from TFR.Start-1 over edge agent to TFR) to be cycle-free.

				// Find TFR start in resource flow.
				var resourceFlowIndex = Array.IndexOf(ResourceFlow, NewDistribution[_reconfiguredFragment.Start - _fragment.Start]);
				Debug.Assert(resourceFlowIndex != -1);

				// Find agent between ResourceFlow[0] and TFR that also connects TFR.Start-1 to ResourceFlow[0].
				BaseAgent duplicateAgent = null;
				var duplicateRole = default(Role);

				for (var i = resourceFlowIndex - 1; i >= 0 && duplicateAgent == null; --i)
					foreach (var role in ResourceFlow[i].AllocatedRoles)
						if (_reconfiguredFragment.Succeedes(role))
						{
							duplicateAgent = ResourceFlow[i];
							duplicateRole = role;
							break;
						}

				// There's no agent between ResourceFlow[0] and TFR that also transports resources to ResourceFlow[0].
				if (duplicateAgent == null)
					return Tuple.Create(ResourceFlow[0], new Role.Allocation[0]); // Choose ResourceFlow[0] as edge agent.

				// Choose duplicateAgent as edge agent. Collect transport roles between that agent and TFR.
				var transportRoles = new List<Role.Allocation>();
				foreach (var allocation in ResourceFlowWalker.WalkForward(duplicateAgent, duplicateRole))
				{
					if (allocation.Agent == OldDistribution[_reconfiguredFragment.Start])
						break;

					Debug.Assert(allocation.Agent.IsAlive);
					await _coalition.Invite(allocation.Agent);

					transportRoles.Add(allocation);
				}
				return Tuple.Create(duplicateAgent, transportRoles.ToArray());
			}

			private async Task<Tuple<BaseAgent, Role.Allocation[]>> FindExitEdgeAgent()
			{
				// TFR includes last capability in task - no edge agent needed
				if (_reconfiguredFragment.End == Task.RequiredCapabilities.Length - 1)
					return Tuple.Create((BaseAgent)null, new Role.Allocation[0]);

				Debug.Assert(OldDistribution[_reconfiguredFragment.End] != null, "Dead agents should be surrounded in TFR.");

				// last agent to apply a capability in TFR is the same -> can serve as edge agent
				if (NewDistribution[_reconfiguredFragment.End - _fragment.Start] == OldDistribution[_reconfiguredFragment.End])
					return Tuple.Create(OldDistribution[_reconfiguredFragment.End], new Role.Allocation[0]);

				// Otherwise we need an agent we know is connected to whoever applies capability TFR.End+1.
				// If TFR.End < CTF,End, capability TFR.End+1 was and is applied by an agent in ResourceFlow - choose it!
				if (_reconfiguredFragment.End < _fragment.End)
				{
					var edgeAgent = OldDistribution[_reconfiguredFragment.End + 1];

					// Find roles that transport resources between TFR and edge agent
					var edgeTransportRoles = ResourceFlowWalker
						.WalkForward(OldDistribution[_reconfiguredFragment.End], FindRoleForCapability(_reconfiguredFragment.End))
						.TakeWhile(allocation => !allocation.Role.IncludesCapability(_reconfiguredFragment.End + 1))
						.ToArray();

					return Tuple.Create(edgeAgent, edgeTransportRoles);
				}

				Debug.Assert(_reconfiguredFragment.End == _fragment.End);
				Debug.Assert(ResourceFlow[ResourceFlow.Length - 1] == OldDistribution[_reconfiguredFragment.End]);

				// Otherwise TFR.End+1 is outside of the fragment.
				// We know ResourceFlow.Last is connected to the agent that applies it is, and ResourceFlow connects TFR to ResourceFlow.Last.
				// But we need the concatenated connection (from TFR over edge agent to TFR.End+1) to be cycle-free.

				// Find TFR end in resource flow.
				var resourceFlowIndex = Array.LastIndexOf(ResourceFlow, NewDistribution[_reconfiguredFragment.End - _fragment.Start]);
				Debug.Assert(resourceFlowIndex != -1);

				// Find agent between TFR and ResourceFlow.Last that also connects ResourceFlow.Last to TFR.End+1.
				BaseAgent duplicateAgent = null;
				var duplicateRole = default(Role);

				for (var i = resourceFlowIndex + 1; i < ResourceFlow.Length && duplicateAgent == null; ++i)
					foreach (var role in ResourceFlow[i].AllocatedRoles)
						if (_reconfiguredFragment.Precedes(role))
						{
							duplicateAgent = ResourceFlow[i];
							duplicateRole = role;
							break;
						}

				// There's no agent between TFR and ResourceFlow.Last that also transports resources from ResourceFlow.Last.
				if (duplicateAgent == null)
					return Tuple.Create(ResourceFlow[ResourceFlow.Length - 1], new Role.Allocation[0]); // Choose ResourceFlow.Last as edge agent.

				// Choose duplicateAgent as edge agent. Collect transport roles between TFR and that agent.
				var transportRoles = new List<Role.Allocation>();
				foreach (var allocation in ResourceFlowWalker.WalkBackward(duplicateAgent, duplicateRole))
				{
					if (allocation.Agent == OldDistribution[_reconfiguredFragment.End])
						break;

					Debug.Assert(allocation.Agent.IsAlive);
					await _coalition.Invite(allocation.Agent);

					transportRoles.Add(allocation);
				}
				return Tuple.Create(duplicateAgent, transportRoles.ToArray());
			}

			private Role FindRoleForCapability(int capabilityIndex)
			{
				return OldDistribution[capabilityIndex]
								.AllocatedRoles
								.Single(role => role.Task == Task && role.IncludesCapability(capabilityIndex));
			}

			private ISet<BaseAgent> FindCoreAgents()
			{
				// core agents are:
				//  (1) agents that will receive new roles, as indicated in NewDistribution (restricted to _reconfiguredFragment)
				//  (2) agents that will lose roles, because they either previously applied a capability in _reconfiguredFragment
				//      or transported resources between such agents

				var coreAgents = new HashSet<BaseAgent>(
					NewDistribution.Slice(_reconfiguredFragment.Start - _fragment.Start, _reconfiguredFragment.End - _fragment.Start) // (1)
				);

				// (2): go along the resource flow path, from _reconfiguredFragment.Start to _reconfiguredFragment.End
				var current = OldDistribution[_reconfiguredFragment.Start];
				var currentPos = _reconfiguredFragment.Start;
				while (currentPos <= _reconfiguredFragment.End)
				{
					Role currentRole;

					if (current == null || !current.IsAlive)
					{
						// if agent is dead: find next known agent that isn't
						while ((current == null || !current.IsAlive) && currentPos <= _reconfiguredFragment.End)
						{
							currentPos++;
							if (currentPos <= _reconfiguredFragment.End)
								current = OldDistribution[currentPos];
						}
						if (currentPos > _reconfiguredFragment.End)
							break; // all further agents are dead
						Debug.Assert(current != null && current.IsAlive);

						// otherwise, go back until just after dead agent
						currentRole = current.AllocatedRoles.Single(role => role.Task == Task && (role.IncludesCapability(currentPos) || role.PreCondition.StateLength == currentPos));
						while (currentRole.Input != null && currentRole.Input.IsAlive)
						{
							current = currentRole.Input;
							currentPos = currentRole.PreCondition.StateLength;
							currentRole = current.AllocatedRoles.Single(role => role.Task == Task && role.PostCondition.StateLength == currentPos);
						}
						currentPos = currentRole.PreCondition.StateLength;
					}

					coreAgents.Add(current);
					currentRole = current.AllocatedRoles.Single(role => role.Task == Task && (role.IncludesCapability(currentPos) || role.PreCondition.StateLength == currentPos));
					currentPos = currentRole.PostCondition.StateLength;
					current = currentRole.Output;
				}

				return coreAgents;
			}

			/// <summary>
			///   Returns a subrange of <see cref="ResourceFlow"/> starting with the <paramref name="entryEdgeAgent"/> and ending with the <paramref name="exitEdgeAgent"/> (if non-null, respectively).
			/// </summary>
			private BaseAgent[] RestrictResourceFlow(BaseAgent entryEdgeAgent, BaseAgent exitEdgeAgent)
			{
				// find entryEdgeAgent, exitEdgeAgent in ResourceFlow, searching backwards/forwards from TFR.Start/.End
				// If they exist (i.e. are non-null), they must be present because:
				// 1) either TFR.Start == CTF.Start > 0, hence ??? -- same for exitedgeagent
				// 2) TFR.Start > CTF.Start, hence capability TFR.Start - 1 >= CTF.Start remains unmodified and part of distribution -> agent that (still) applies it is in resourceFlow -- same for exitedgeagent

				// for all agents in NewDistribution, determine position in ResourceFlow
				var distributionPositions = new int[_fragment.Length];
				var j = 0;
				for (var i = 0; i < distributionPositions.Length; ++i)
				{
					while (ResourceFlow[j] != NewDistribution[i])
						++j;
					distributionPositions[i] = j;
				}

				// determine start of restricted resource flow
				var entryIndex = distributionPositions[_reconfiguredFragment.Start - _fragment.Start];
				if (entryEdgeAgent != null)
					while (ResourceFlow[entryIndex] != entryEdgeAgent)
						entryIndex--;

				// determine end of restricted resource flow
				var exitIndex = distributionPositions[_reconfiguredFragment.End - _fragment.Start];
				if (exitEdgeAgent != null)
					while (ResourceFlow[exitIndex] != exitEdgeAgent)
						exitIndex++;

				// restrict resourceflow to the respective subrange
				var restrictedResourceFlow = new BaseAgent[exitIndex - entryIndex + 1];
				Console.WriteLine($"source.length={ResourceFlow.Length}, start={entryIndex}, end={exitIndex}, length={restrictedResourceFlow.Length}");
				Array.Copy(ResourceFlow, entryIndex, restrictedResourceFlow, 0, restrictedResourceFlow.Length);
				return restrictedResourceFlow;
			}
		}
	}
}
