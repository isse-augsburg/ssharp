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
	using System.Linq;
	using System.Threading.Tasks;

	public class Coalition
	{
		public ITask Task { get; }

		public CoalitionReconfigurationAgent Leader { get; }

		public List<CoalitionReconfigurationAgent> Members { get; }
			= new List<CoalitionReconfigurationAgent>();

		public HashSet<ICapability> AvailableCapabilities { get; } = new HashSet<ICapability>();
		public HashSet<ICapability> NeededCapabilities { get; } = new HashSet<ICapability>();

		private Dictionary<BaseAgent, TaskCompletionSource<object>> _invitations
			= new Dictionary<BaseAgent, TaskCompletionSource<object>>();

		public BaseAgent[] RecoveredDistribution { get; }

		public bool CapabilitiesSatisfied() => NeededCapabilities.IsSubsetOf(AvailableCapabilities);

		public bool IsInvited(CoalitionReconfigurationAgent agent) => _invitations.ContainsKey(agent.BaseAgent);

		public TaskFragment CTF { get; private set; }

		public Coalition(CoalitionReconfigurationAgent leader, ITask task)
		{
			RecoveredDistribution = new BaseAgent[task.RequiredCapabilities.Length];
			Task = task;
			Join(Leader = leader);
		}

		public async Task Invite(BaseAgent agent)
		{
			if (Members.Any(member => member.BaseAgent == agent))
				return;

			_invitations[agent] = new TaskCompletionSource<object>();

			// do not await, await invitation response (next line) instead (otherwise a deadlock occurs)
			agent.RequestReconfiguration(Leader, Task);

			await _invitations[agent].Task;
			_invitations.Remove(agent);
		}

		public void Join(CoalitionReconfigurationAgent newMember)
		{
			Members.Add(newMember);
			AvailableCapabilities.UnionWith(newMember.BaseAgentState.AvailableCapabilities);
			UpdateCTF(newMember.BaseAgent);
			newMember.CurrentCoalition = this;

			if (IsInvited(newMember))
			{
				_invitations[newMember.BaseAgent].SetResult(null);
				_invitations.Remove(newMember.BaseAgent);
			}

			// TODO: invitation might lead to coalition merge -- if no longer leader, stop here!?
			// TODO: (implementation: cancellation token?)
		}

		private void UpdateCTF(BaseAgent newAgent)
		{
			var minPreState = -1;
			var maxPostState = -1;

			foreach (var role in newAgent.AllocatedRoles)
			{
				if (role.Task != Task)
					continue;

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
	}
}
