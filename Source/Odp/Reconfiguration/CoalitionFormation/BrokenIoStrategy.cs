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
	using System.Linq;
	using System.Threading.Tasks;

	public class BrokenIoStrategy : IRecruitingStrategy
	{
		private BrokenIoStrategy() { }

		public static BrokenIoStrategy Instance { get; } = new BrokenIoStrategy();

		public async Task<TaskFragment> RecruitNecessaryAgents(Coalition coalition)
		{
			var affectedRoles = await FindDisconnectedRoles(coalition);

			foreach (var entry in affectedRoles)
				await RecruitNecessaryAgents(coalition, entry.Item1, entry.Item2);

			return TaskFragment.Merge(
				affectedRoles.Select(entry => DetermineFragmentToReconfigure(entry.Item2))
			);
		}

		/// <summary>
		/// Determine the fragment that must be reconfigured because of the given <paramref name="affectedRole"/>.
		/// The fragment might be empty (no capabilities changes needed), but its position within the task is important.
		/// If both ports are defect, the fragment encompasses exactly the affected role.
		/// </summary>
		private static TaskFragment DetermineFragmentToReconfigure(Role affectedRole)
		{
			// Initial fragment boundaries to last capability before affected role and first capability after affected role (reversed).
			var fragmentStart = affectedRole.PostCondition.StateLength; // StateLength is index of first capability to apply by next role
			var fragmentEnd = affectedRole.PreCondition.StateLength - 1; // StateLength is index of first capability to apply by affected role

			if (affectedRole.PreCondition.Port == null)
				fragmentStart = affectedRole.PreCondition.StateLength; // include empty fragment preceeding role in fragment to reconfigure
			if (affectedRole.PostCondition.Port == null)
				fragmentEnd = affectedRole.PostCondition.StateLength - 1; // include empty fragment following role in fragment to reconfigure

			return new TaskFragment(affectedRole.Task, fragmentStart, fragmentEnd);
		}

		/// <summary>
		/// Recruit necessary agents to fix the invariant violation concerning a specific role:
		/// Surround empty roles by predecessor / successor roles with at least one capability and recruit agents on the way.
		/// </summary>
		/// <param name="coalition">The coalition used for reconfiguration.</param>
		/// <param name="agent">The agent to which the <paramref name="affectedRole"/> is allocated.</param>
		/// <param name="affectedRole">The role whose output / input port is defect.</param>
		private static async Task RecruitNecessaryAgents(Coalition coalition, BaseAgent agent, Role affectedRole)
		{
			// predecessor
			var currentRole = affectedRole;
			var currentAgent = agent;
			var previousAgent = currentRole.PreCondition.Port;

			while (previousAgent != null && previousAgent.IsAlive && !currentRole.CapabilitiesToApply.Any())
			{
				await coalition.Invite(previousAgent);
				currentRole = previousAgent.AllocatedRoles.Single(otherRole =>
					otherRole.PostCondition.StateMatches(currentRole.PreCondition) && otherRole.PostCondition.Port == currentAgent
				);
				currentAgent = previousAgent;
				previousAgent = currentRole.PreCondition.Port;
			}

			// successor
			currentRole = affectedRole;
			currentAgent = agent;
			var nextAgent = currentRole.PostCondition.Port;

			while (nextAgent != null && nextAgent.IsAlive && !currentRole.CapabilitiesToApply.Any())
			{
				await coalition.Invite(nextAgent);
				currentRole = nextAgent.AllocatedRoles.Single(otherRole =>
					otherRole.PreCondition.StateMatches(currentRole.PostCondition) && otherRole.PreCondition.Port == currentAgent
				);
				currentAgent = nextAgent;
				nextAgent = currentRole.PostCondition.Port;
			}
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
					if (role.PreCondition.Port != null && !agent.Inputs.Contains(role.PreCondition.Port))
					{
						affected = true;
						if (!coalition.Contains(role.PreCondition.Port) && role.PreCondition.Port.IsAlive)
						{
							var newMember = await coalition.Invite(role.PreCondition.Port);
							members.Add(newMember);
						}
					}
					if (role.PostCondition.Port != null && !agent.Outputs.Contains(role.PostCondition.Port))
					{
						affected = true;
						if (!coalition.Contains(role.PostCondition.Port) && role.PostCondition.Port.IsAlive)
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
	}
}
