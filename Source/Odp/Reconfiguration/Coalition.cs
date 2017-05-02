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

namespace SafetySharp.Odp.Reconfiguration
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;

	public partial class Coalition
	{
		// task & configuration
		public ITask Task { get; }

		public InvariantPredicate[] ViolatedPredicates { get; private set; }

		public BaseAgent[] RecoveredDistribution { get; }

		/// <summary>
		/// The connected task fragment handled by coalition members
		/// </summary>
		public TaskFragment CTF { get; private set; }

		// members & invitations
		public CoalitionReconfigurationAgent Leader { get; }

		public HashSet<CoalitionReconfigurationAgent> Members { get; }
			= new HashSet<CoalitionReconfigurationAgent>();
		private readonly HashSet<BaseAgent> _baseAgents = new HashSet<BaseAgent>();

		private readonly Dictionary<BaseAgent, TaskCompletionSource<CoalitionReconfigurationAgent>> _invitations
			= new Dictionary<BaseAgent, TaskCompletionSource<CoalitionReconfigurationAgent>>();

		public bool IsInvited(CoalitionReconfigurationAgent agent) => _invitations.ContainsKey(agent.BaseAgent);

		private MergeSupervisor Merger { get; }

		public Coalition(CoalitionReconfigurationAgent leader, ITask task, InvariantPredicate[] violatedPredicates)
		{
			Merger = new MergeSupervisor(this);
			RecoveredDistribution = new BaseAgent[task.RequiredCapabilities.Length];
			Task = task;
			ViolatedPredicates = violatedPredicates;
			Join(Leader = leader);
		}

		public bool Contains(BaseAgent baseAgent)
		{
			return _baseAgents.Contains(baseAgent);
		}

		public async Task InviteCtfAgents()
		{
			var previous = RecoveredDistribution[CTF.Start];
			Debug.Assert(Contains(previous));

			int ctfStart;
			do
			{
				ctfStart = CTF.Start;
				for (int i = CTF.Start + 1; i < CTF.End; ++i)
				{
					var current = RecoveredDistribution[i];

					if (current == null)
					{
						var role = previous.AllocatedRoles.Single(r => r.Task == Task && r.PreCondition.StateLength < i && r.PostCondition.StateLength >= i);
						current = role.PostCondition.Port;
						await Invite(current);
					}

					previous = current;
				}
			} while (ctfStart > CTF.Start); // loop because invitations might have enlarged CTF
		}

		/// <summary>
		/// Invites a new agent into the coalition. Merge requests from other coalitions are handled first.
		/// </summary>
		/// <param name="agent">The <see cref="BaseAgent"/> that is invited.</param>
		/// <returns>The <see cref="CoalitionReconfigurationAgent"/> belonging to <paramref name="agent"/>.</returns>
		/// <exception cref="OperationCanceledException">Thrown if processing of pre-existing merge requests or the invite lead to a coalition merge,
		/// and the coalitions is disbanded (merged into another coalition).</exception>
		public async Task<CoalitionReconfigurationAgent> Invite(BaseAgent agent)
		{
			// If members were invited by other coalitions, process the merge requests.
			// If this coalition is disbanded during that process, execution of this method is cancelled.
			Merger.ProcessMergeRequests();

			// Do not re-invite current members.
			var existingMember = Members.FirstOrDefault(member => member.BaseAgent == agent);
			if (existingMember != null)
				return existingMember;

			// Invite the agent.
			_invitations[agent] = new TaskCompletionSource<CoalitionReconfigurationAgent>();
			agent.RequestReconfiguration(Leader, Task); // do NOT await; await invitation response (see below) instead (otherwise a deadlock occurs)
			var newMember = await _invitations[agent].Task; // wait for the invited agent to respond

			// If the invited agent already belongs to a coalition, a merge was initiated.
			// Wait for such a merge to complete, but avoid deadlocks when two leaders invite each other.
			await Merger.WaitForMergeCompletion();

			return newMember;
		}

		private void ReceiveInvitationResponse(CoalitionReconfigurationAgent invitedAgent)
		{
			_invitations[invitedAgent.BaseAgent].SetResult(invitedAgent);
			_invitations.Remove(invitedAgent.BaseAgent);
		}

		private void CancelInvitations()
		{
			foreach (var invitation in _invitations.Values)
				invitation.SetResult(null);
			_invitations.Clear();
		}

		/// <summary>
		/// Adds the given <paramref name="newMember"/> to the coalition, completing an open invitation if there is one.
		/// </summary>
		public void Join(CoalitionReconfigurationAgent newMember)
		{
			Members.Add(newMember);
			_baseAgents.Add(newMember.BaseAgent);
			newMember.CurrentCoalition = this;

			UpdateCTF(newMember.BaseAgent);

			if (IsInvited(newMember))
				ReceiveInvitationResponse(newMember);
		}

		/// <summary>
		/// Updates the CTF when <paramref name="newAgent"/> is added to the coalition
		/// </summary>
		private void UpdateCTF(BaseAgent newAgent)
		{
			var minPreState = -1;
			var maxPostState = -1;

			foreach (var role in newAgent.AllocatedRoles)
			{
				if (role.Task != Task)
					continue;

				// discover which capabilities were previously applied by newAgent
				for (var i = role.PreCondition.StateLength; i < role.PostCondition.StateLength; ++i)
				{
					if (RecoveredDistribution[i] != null)
						throw new InvalidOperationException("Branches in the resource flow are currently unsupported by the coalition-formation based reconfiguration algorithm.");
					RecoveredDistribution[i] = newAgent;
				}

				if (minPreState == -1 || role.PreCondition.StateLength < minPreState)
					minPreState = role.PreCondition.StateLength;
				maxPostState = Math.Max(maxPostState, role.PostCondition.StateLength - 1); // if StateLength = n, the first n capabilities (0, ..., n - 1) are applied => subtract 1
			}

			// newAgent is not configured for this.Task
			if (minPreState == -1) // && maxPostState == -1
				return;

			if (CTF == null) // first initialization
				CTF = new TaskFragment(Task, minPreState, maxPostState);
			else // enlarge fragment
			{
				CTF.Prepend(minPreState);
				CTF.Append(maxPostState);
			}
		}

		/// <summary>
		/// Called by members to notify the coalition they have been invited by another coalition.
		/// </summary>
		/// <param name="source">The agent that invited a member, i.e. the leader of the other coalition.</param>
		public void MergeCoalition(CoalitionReconfigurationAgent source)
		{
			Merger.MergeCoalition(source);
		}

		/// <summary>
		/// Notifies the coalition an invited agent already belongs to a different coalition,
		/// and that it will receive a <see cref="RendezvousRequest(Coalition, CoalitionReconfigurationAgent)"/> from
		/// the opposing <paramref name="leader"/>.
		/// </summary>
		public void AwaitRendezvous(CoalitionReconfigurationAgent invitedAgent, CoalitionReconfigurationAgent leader)
		{
			Merger.AwaitRendezvous(invitedAgent, leader);
		}
	}
}
