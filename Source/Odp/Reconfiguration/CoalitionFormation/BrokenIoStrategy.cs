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

	public class BrokenIoStrategy : IRecruitingStrategy
	{
		private BrokenIoStrategy() { }

		public static BrokenIoStrategy Instance { get; } = new BrokenIoStrategy();

		public async Task<TaskFragment> RecruitNecessaryAgents(Coalition coalition)
		{
			var affectedRoles = await FindDisconnectedRoles(coalition);

			return TaskFragment.Merge(
				coalition.Task,
				await Task.WhenAll(
					from entry in affectedRoles select RecruitNecessaryAgents(coalition, entry.Item1, entry.Item2)
				)
			);
		}

		/// <summary>
		/// Recruit necessary agents to fix the invariant violation concerning a specific role:
		/// Surround empty roles by predecessor / successor roles with at least one capability and recruit agents on the way.
		/// </summary>
		/// <param name="coalition">The coalition used for reconfiguration.</param>
		/// <param name="agent">The agent to which the <paramref name="affectedRole"/> is allocated.</param>
		/// <param name="affectedRole">The role whose output / input port is defect.</param>
		/// <returns>A <see cref="TaskFragment"/> that must be included in the TFR because of this role.</returns>
		private static async Task<TaskFragment> RecruitNecessaryAgents(Coalition coalition, BaseAgent agent, Role affectedRole)
		{
			Debug.WriteLine("{0}: recruiting agents for role", nameof(BrokenIoStrategy));

			// predecessor
			var currentRole = affectedRole;
			var currentAgent = agent;
			var previousAgent = currentRole.Input;

			Debug.WriteLine("Finding non-empty predecessor");
			while (previousAgent != null && previousAgent.IsAlive && !currentRole.CapabilitiesToApply.Any())
			{
				await coalition.Invite(previousAgent);
				currentRole = previousAgent.AllocatedRoles.Single(otherRole =>
					otherRole.PostCondition.StateMatches(currentRole.PreCondition) && otherRole.Output == currentAgent
				);
				currentAgent = previousAgent;
				previousAgent = currentRole.Input;
			}
			var fragmentStart = currentRole.PreCondition.StateLength; // first capability to be applied by non-empty predecessor role

			// successor
			currentRole = affectedRole;
			currentAgent = agent;
			var nextAgent = currentRole.Output;

			Debug.WriteLine("Finding non-empty successor");
			while (nextAgent != null && nextAgent.IsAlive && !currentRole.CapabilitiesToApply.Any())
			{
				await coalition.Invite(nextAgent);
				currentRole = nextAgent.AllocatedRoles.Single(otherRole =>
					otherRole.PreCondition.StateMatches(currentRole.PostCondition) && otherRole.Input == currentAgent
				);
				currentAgent = nextAgent;
				nextAgent = currentRole.Output;
			}
			var fragmentEnd = currentRole.PostCondition.StateLength - 1; // last capability to be applied by non-empty successor
			// Note: subtraction is not a problem since there's never a role with PostCondition.StateLength = 0, as a resource is always produced before being transported.

			return new TaskFragment(coalition.Task, fragmentStart, fragmentEnd);
		}

		/// <summary>
		/// Find roles where the input/output invariant is violated and invite disconnected agents in the coalition.
		/// </summary>
		/// <param name="coalition">The coalition whose agents should be searched for violations.</param>
		/// <returns>The affected roles and the agents to which they are allocated.</returns>
		private static async Task<Tuple<BaseAgent, Role>[]> FindDisconnectedRoles(Coalition coalition)
		{
			var affectedRoles = new List<Tuple<BaseAgent, Role>>();

			var members = coalition.Members.ToList(); // use list because coalition.Members is modified during iteration
			for (var i = 0; i < members.Count; ++i)
			{
				var agent = members[i].BaseAgent;
				foreach (var role in agent.AllocatedRoles)
				{
					if (role.Task != coalition.Task)
						continue;

					// 1. invite disconnected agents (so their roles can be removed / updated)
					var affected = false;
					if (role.Input != null && !agent.Inputs.Contains(role.Input))
					{
						affected = true;
						if (!coalition.Contains(role.Input) && role.Input.IsAlive)
						{
							var newMember = await coalition.Invite(role.Input);
							members.Add(newMember);
						}
					}
					if (role.Output != null && !agent.Outputs.Contains(role.Output))
					{
						affected = true;
						if (!coalition.Contains(role.Output) && role.Output.IsAlive)
						{
							var newMember = await coalition.Invite(role.Output);
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
	}
}
