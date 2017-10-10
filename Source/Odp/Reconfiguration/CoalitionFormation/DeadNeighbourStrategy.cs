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
								where role.Task == coalition.Task && (role.Input?.IsAlive == false || role.Output?.IsAlive == false)
								select role;

			var fragments = new List<TaskFragment>();

			// for each role, must find predecessor / successor role
			foreach (var role in affectedRoles)
			{
				var start = role.PreCondition.StateLength;
				var end = role.PostCondition.StateLength;

				if (role.Input?.IsAlive == false) // find predecessor
				{
					var predecessor = await RecruitConnectedAgent(role, coalition, FindLastPredecessor);
					start = predecessor.Item2.PreCondition.StateLength;
				}

				if (role.Output?.IsAlive == false) // find successor
				{
					var successor = await RecruitConnectedAgent(role, coalition, FindFirstSuccessor);
					end = successor.Item2.PostCondition.StateLength;
				}

				fragments.Add(new TaskFragment(coalition.Task, start, end));
			}

			// once predecessor / successor are in the coalition,
			// Coalition.InviteCtf() will fill the gaps in the CTF
			// (called in CalculateConfigurationsAsync()).

			// TODO unclear: is it necessary to surround empty predecessors / successors by empty roles? ~> test such situations

			// coalition might have lost capabilities due to dead agents
			await MissingCapabilitiesStrategy.Instance.RecruitNecessaryAgents(coalition);

			return TaskFragment.Merge(coalition.Task, fragments);
		}

		// used to recruit predecessor / successor of a role connected to a dead agent
		private static async Task<Tuple<BaseAgent, Role>> RecruitConnectedAgent(Role role, Coalition coalition,
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
					if (agent != null && agent.Item1.IsAlive)
						break;
				}

				if (agent == null)
					throw new Exception("Successor / predecessor role is unreachable or dead");

				await coalition.Invite(agent.Item1);
			}

			return agent;
		}

		private static Tuple<BaseAgent, Role> FindLastPredecessor(Role role, IEnumerable<Tuple<BaseAgent, Role>> possiblePredecessors)
		{
			return (from predecessor in possiblePredecessors
					let predRole = predecessor.Item2
					where predRole.Task == role.Task
						&& (predRole.PreCondition.StateLength < role.PreCondition.StateLength
								|| (role.CapabilitiesToApply.Any() && predRole.PreCondition.StateLength == role.PreCondition.StateLength))
					orderby predRole.PreCondition.StateLength descending
					select predecessor).FirstOrDefault();
		}

		private static Tuple<BaseAgent, Role> FindFirstSuccessor(Role role, IEnumerable<Tuple<BaseAgent, Role>> possibleSuccessors)
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
