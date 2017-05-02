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

		public async Task RecruitNecessaryAgents(Coalition coalition)
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
	}
}
