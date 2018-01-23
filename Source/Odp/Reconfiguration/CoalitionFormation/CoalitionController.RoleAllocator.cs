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
	using System.Threading.Tasks;
	using Modeling;

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

			private ConfigurationUpdate ReconfigureTFR(BaseAgent[] resourceFlow, BaseAgent entryEdgeAgent, BaseAgent exitEdgeAgent, Tuple<BaseAgent, Role>[] edgeTransportRoles)
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
				foreach (var agentRoleGroup in edgeTransportRoles.GroupBy(t => t.Item1))
					config.RemoveRoles(agentRoleGroup.Key, agentRoleGroup.Select(t => t.Item2).ToArray());

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

			private async Task<Tuple<BaseAgent, Tuple<BaseAgent, Role>[]>> FindEntryEdgeAgent()
			{
				// TFR includes first capability - no edge agent needed
				if (_reconfiguredFragment.Start == 0)
					return Tuple.Create((BaseAgent)null, new Tuple<BaseAgent, Role>[0]);

				Debug.Assert(OldDistribution[_reconfiguredFragment.Start] != null, "Dead agents should be surrounded in TFR.");

				// first agent to apply a capability is the same -> can serve as edge agent
				if (NewDistribution[_reconfiguredFragment.Start - _fragment.Start] == OldDistribution[_reconfiguredFragment.Start])
					return Tuple.Create(OldDistribution[_reconfiguredFragment.Start], new Tuple<BaseAgent, Role>[0]);

				// otherwise agent that applied (and still applies) capability ReconfiguredTaskFragment.Start-1 is chosen as edge agent
				var currentAgent = OldDistribution[_reconfiguredFragment.Start];
				var currentRole = FindRoleForCapability(_reconfiguredFragment.Start);

				var edgeTransportRoles = new List<Tuple<BaseAgent, Role>>();

				while (!currentRole.IncludesCapability(_reconfiguredFragment.Start - 1))
				{
					edgeTransportRoles.Add(Tuple.Create(currentAgent, currentRole));

					var nextAgent = currentRole.Input;
					Debug.Assert(nextAgent != null && nextAgent.IsAlive); // otherwise coalition merge would have occurred.
					await _coalition.Invite(nextAgent);

					currentRole = nextAgent.AllocatedRoles.Single(_reconfiguredFragment.Succeedes);
					currentAgent = nextAgent;
				}

				return Tuple.Create(currentAgent, edgeTransportRoles.ToArray());
			}

			private async Task<Tuple<BaseAgent, Tuple<BaseAgent, Role>[]>> FindExitEdgeAgent()
			{
				// TFR includes last capability - no edge agent needed
				if (_reconfiguredFragment.End == Task.RequiredCapabilities.Length - 1)
					return Tuple.Create((BaseAgent)null, new Tuple<BaseAgent, Role>[0]);

				Debug.Assert(OldDistribution[_reconfiguredFragment.End] != null, "Dead agents should be surrounded in TFR.");

				// last agent to apply a capability is the same -> can serve as edge agent
				if (NewDistribution[_reconfiguredFragment.End - _fragment.Start] == OldDistribution[_reconfiguredFragment.End])
					return Tuple.Create(OldDistribution[_reconfiguredFragment.End], new Tuple<BaseAgent, Role>[0]);

				// otherwise agent that applied (and still applies) capability ReconfiguredTaskFragment.End+1 is chosen as edge agent
				var currentAgent = OldDistribution[_reconfiguredFragment.End];
				var currentRole = FindRoleForCapability(_reconfiguredFragment.End);

				var edgeTransportRoles = new List<Tuple<BaseAgent, Role>>();

				while (!currentRole.IncludesCapability(_reconfiguredFragment.End + 1))
				{
					edgeTransportRoles.Add(Tuple.Create(currentAgent, currentRole));

					var nextAgent = currentRole.Output;
					Debug.Assert(nextAgent != null && nextAgent.IsAlive); // otherwise coalition merge would have occurred.
					await _coalition.Invite(nextAgent);

					currentRole = nextAgent.AllocatedRoles.Single(_reconfiguredFragment.Precedes);
					currentAgent = nextAgent;
				}

				return Tuple.Create(currentAgent, edgeTransportRoles.ToArray());
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
				// find entryEdgeAgent, exitEdgeAgent in _resourceFlow, searching backwards/forwards from TFR.Start/.End
				// they must be present because:
				// 1) either TFR.Start == CTF.Start, hence NewDistribution[TFR.Start] == OldDistribution[TFR.Start] is edge agent (see isProducer in CoalitionController) -- same for exitedgeagent
				// 2) TFR.Start > CTF.Start, hence capability TFR.Start - 1 >= CTF.Start remains unmodified and part of distribution -> agent that (still) applies it is in resourceFlow -- same for exitedgeagent
				
				var entryIndex = _reconfiguredFragment.Start - _fragment.Start;
				while (entryIndex > 0 && ResourceFlow[entryIndex] != entryEdgeAgent)
					entryIndex--;
				Debug.Assert(entryEdgeAgent == null && entryIndex == 0 || ResourceFlow[entryIndex] == entryEdgeAgent);

				var exitIndex = _reconfiguredFragment.End - _fragment.Start;
				while (exitIndex < ResourceFlow.Length - 1 && ResourceFlow[entryIndex] != exitEdgeAgent)
					exitIndex++;
				Debug.Assert(exitEdgeAgent == null && exitIndex == ResourceFlow.Length-1 || ResourceFlow[exitIndex] == exitEdgeAgent);

				// restrict resourceflow to the respective subrange
				var restrictedResourceFlow = new BaseAgent[exitIndex - entryIndex + 1];
				Array.Copy(ResourceFlow, entryIndex, restrictedResourceFlow, 0, restrictedResourceFlow.Length);
				return restrictedResourceFlow;
			}
		}
	}
}
