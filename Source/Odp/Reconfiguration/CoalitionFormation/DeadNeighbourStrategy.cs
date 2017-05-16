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

	public class DeadNeighbourStrategy : IRecruitingStrategy
	{
		private DeadNeighbourStrategy() { }

		public static DeadNeighbourStrategy Instance { get; } = new DeadNeighbourStrategy();

		public async Task<TaskFragment> RecruitNecessaryAgents(Coalition coalition)
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

			// TODO surround empty predecessors/successors

			// coalition might have lost capabilities due to dead agents
			await MissingCapabilitiesStrategy.Instance.RecruitNecessaryAgents(coalition);

			throw new NotImplementedException(); // TODO: return fragment
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
								|| (role.CapabilitiesToApply.Any() && predRole.PreCondition.StateLength == role.PreCondition.StateLength))
					orderby predRole.PreCondition.StateLength descending
					select predecessor).FirstOrDefault();
		}

		private Tuple<BaseAgent, Role> FindFirstSuccessor(Role role, IEnumerable<Tuple<BaseAgent, Role>> possibleSuccessors)
		{
			return (from successor in possibleSuccessors
					let succRole = successor.Item2
					where succRole.Task == role.Task
						&& (succRole.PostCondition.StateLength > role.PostCondition.StateLength
							|| (role.CapabilitiesToApply.Any() && succRole.PostCondition.StateLength == role.PostCondition.StateLength))
					orderby succRole.PostCondition.StateLength ascending
					select successor).FirstOrDefault();
		}
	}
}
