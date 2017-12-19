﻿// The MIT License (MIT)
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
	using JetBrains.Annotations;
	using Modeling;

	public partial class Coalition
	{
		// task & configuration
		public ITask Task { get; }

		public InvariantPredicate[] ViolatedPredicates { get; private set; }
        public bool IsInitialConfiguration { get; private set; }

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
		public ISet<BaseAgent> BaseAgents => _baseAgents;

		private readonly Dictionary<BaseAgent, TaskCompletionSource<CoalitionReconfigurationAgent>> _invitations
			= new Dictionary<BaseAgent, TaskCompletionSource<CoalitionReconfigurationAgent>>();

		public bool IsInvited(CoalitionReconfigurationAgent agent) => _invitations.ContainsKey(agent.BaseAgent);

	    private readonly HashSet<BaseAgent> _neighbours = new HashSet<BaseAgent>();
	    public bool HasNeighbours => _neighbours.Count > 0;

        private MergeSupervisor Merger { get; }

		public Coalition(CoalitionReconfigurationAgent leader, ITask task, InvariantPredicate[] violatedPredicates, bool initialConf)
		{
			Merger = new MergeSupervisor(this);
			RecoveredDistribution = new BaseAgent[task.RequiredCapabilities.Length];
			Task = task;
            CTF = TaskFragment.Identity(task);
			ViolatedPredicates = violatedPredicates;
		    IsInitialConfiguration = initialConf;
			Join(Leader = leader);
		}

		public void MergeCtf(TaskFragment fragment)
		{
			CTF = fragment.Merge(CTF);
		}

		public bool Contains(BaseAgent baseAgent)
		{
			return _baseAgents.Contains(baseAgent);
		}

	    public Task InviteNeighbour()
	    {
	        return Invite(_neighbours.First());
	    }

		public async Task InviteCtfAgents()
		{
		    int ctfStart, ctfEnd;
			do
			{
				ctfStart = CTF.Start;
			    ctfEnd = CTF.End;

			    BaseAgent previous = null;
				var previousRole = default(Role);
				var currentPos = ctfStart;
			    var current = RecoveredDistribution[currentPos];

                while (currentPos <= CTF.End)
                {
                    // if we do not know current agent but know its predecessor, ask it
                    if (current == null && previous != null && previous.IsAlive)
				        current = previousRole.Output;

                    // if we (still) cannot contact current agent because we do not know it or it is dead:
                    if (current == null || !current.IsAlive)
				    {
				        // find next known & alive agent in CTF
				        while ((current == null || !current.IsAlive) && currentPos <= CTF.End)
				        {
				            currentPos++;
				            if (currentPos <= CTF.End)
				                current = RecoveredDistribution[currentPos];
				        }
				        if (currentPos > CTF.End)
				            break; // all further agents are dead
						Debug.Assert(current != null && current.IsAlive);

                        // otherwise, go back as far as possible
				        var currentRole = current.AllocatedRoles.Single(role => role.Task == Task && role.PreCondition.StateLength == currentPos);
				        while (currentRole.Input != null && currentRole.Input.IsAlive)
				        {
				            current = currentRole.Input;
					        currentRole = current.AllocatedRoles.Single(role => role.Task == Task && role.PostCondition.StateLength == currentPos);
							currentPos = currentRole.PreCondition.StateLength;
				        }
                    }

                    if (!Contains(current))
                        await Invite(current);

					previous = current;
		            previousRole = previous.AllocatedRoles.Single(role => role.Task == Task && role.PreCondition.StateLength == currentPos);
	                currentPos = previousRole.PostCondition.StateLength;
				    current = previousRole.Output;
				}
			} while (ctfStart > CTF.Start || ctfEnd < CTF.End); // loop because invitations might have enlarged CTF
		}

		/// <summary>
		///   Invites a new agent into the coalition. Merge requests from other coalitions are handled first.
		/// </summary>
		/// <param name="agent">The <see cref="BaseAgent"/> that is invited.</param>
		/// <returns>The <see cref="CoalitionReconfigurationAgent"/> belonging to <paramref name="agent"/>.</returns>
		/// <exception cref="OperationCanceledException">
		///   Thrown if processing of pre-existing merge requests or the invite lead to a coalition merge,
		///   and the coalitions is disbanded (merged into another coalition).
		/// </exception>
		[NotNull, ItemNotNull]
		public async Task<CoalitionReconfigurationAgent> Invite([NotNull] BaseAgent agent)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));

			Debug.WriteLine("Coalition with leader {0} inviting agent {1}", Leader.BaseAgent.Id, agent.Id);

			// sanity check: algorithm should never attempt to invite dead agents
			if (!agent.IsAlive)
				throw new InvalidOperationException("Cannot contact dead agents");

			// If members were invited by other coalitions, process the merge requests.
			// If this coalition is disbanded during that process, execution of this method is cancelled.
			Merger.ProcessMergeRequests();

			// Do not re-invite current members.
			var existingMember = Members.FirstOrDefault(member => member.BaseAgent == agent);
			if (existingMember != null)
			{
				Debug.WriteLine("Agent {0} is already a coalition member. No invite issued.", agent.Id);
				return existingMember;
			}
			Debug.WriteLine("No existing member for agent {0} found.", agent.Id);

			// Do not re-invite members currently invited
			if (!_invitations.ContainsKey(agent))
			{
				// Invite the agent.
				_invitations[agent] = new TaskCompletionSource<CoalitionReconfigurationAgent>();

				// Do NOT await this call. Await the invitation response instead (see below),
				// otherwise a deadlock occurs. Don't ignore task either, or any exception would
				// be swallowed. Thus, schedule the call. MicrostepScheduler handles swallowed
				// exceptions.
				MicrostepScheduler.Schedule(() => agent.RequestReconfiguration(Leader, Task));
				Debug.WriteLine("Invitation issued to agent {0}", agent.Id);
			}

			// wait for the invited agent to respond
			var newMember = await _invitations[agent].Task;
			Debug.WriteLine("Waiting for invitation response from {0} completed", agent.Id);

			// If the invited agent already belongs to a coalition, a merge was initiated.
			// Wait for such a merge to complete, but avoid deadlocks when two leaders invite each other.
			var mergeCanceled = await Merger.WaitForMergeCompletion();
			if (mergeCanceled)
			{
				// The invited agent was already member of a coalition. However, that coalition has
				// since disbanded without merging this coalition. Hence we can re-invite the agent now.
				newMember = await Invite(agent);
			}

			Debug.Assert(newMember != null);
			return newMember;
		}

		private void ReceiveInvitationResponse(CoalitionReconfigurationAgent invitedAgent)
		{
			Debug.WriteLine("Invitation to coalition with leader {0} accepted by agent {1}", Leader.BaseAgent.Id, invitedAgent.BaseAgent.Id);
			_invitations[invitedAgent.BaseAgent].SetResult(invitedAgent);
			_invitations.Remove(invitedAgent.BaseAgent);
		}

		private void CancelInvitations()
		{
			Debug.WriteLine("Cancelling all invitations into coalition with leader {0}", Leader.BaseAgent.Id);
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

            // update known neighbours
            _neighbours.UnionWith(newMember.BaseAgent.Inputs.Concat(newMember.BaseAgent.Outputs));
            _neighbours.ExceptWith(_baseAgents);

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

			CTF = new TaskFragment(Task, minPreState, maxPostState).Merge(CTF);
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
		/// and that its <see cref="MergeSupervisor"/> will receive a <see cref="MergeSupervisor.RendezvousRequest"/> from
		/// the opposing <paramref name="leader"/>.
		/// </summary>
		public void AwaitRendezvous(CoalitionReconfigurationAgent invitedAgent, CoalitionReconfigurationAgent leader)
		{
			Merger.AwaitRendezvous(invitedAgent, leader);
		}

		public void CancelMergeRequests()
		{
			Merger.CancelRequests();
		}
	}
}
