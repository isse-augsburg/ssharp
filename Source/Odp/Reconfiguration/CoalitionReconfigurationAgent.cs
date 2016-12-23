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
	using Modeling;

	public class CoalitionReconfigurationAgent : IReconfigurationAgent
	{
		public BaseAgent BaseAgent { get; }
		private readonly ReconfigurationAgentHandler _reconfAgentHandler;
		private readonly IController _controller;

		public CoalitionReconfigurationAgent(BaseAgent baseAgent, ReconfigurationAgentHandler reconfAgentHandler, IController controller)
		{
			BaseAgent = baseAgent;
			_reconfAgentHandler = reconfAgentHandler;
			_controller = controller;
		}

		protected Coalition CurrentCoalition { get; set; }
		public BaseAgent.State BaseAgentState { get; private set; }

		public void Acknowledge()
		{
			throw new NotImplementedException();
		}

		public void StartReconfiguration(ITask task, IAgent agent, BaseAgent.State baseAgentState)
		{
			MicrostepScheduler.Schedule(() => ReconfigureAsync(task, agent, baseAgentState));
		}

		public async Task ReconfigureAsync(ITask task, IAgent agent, BaseAgent.State baseAgentState)
		{
			BaseAgentState = baseAgentState;

			if (baseAgentState.ReconfRequestSource != null)
				((CoalitionReconfigurationAgent)agent).Respond(this);
			else
			{
			}
		}

		/// <summary>
		/// Receives a response from an agent that received a reconfiguration request from this instance.
		/// </summary>
		/// <param name="agent">The agent responding to the reconfiguration request</param>
		private void Respond(CoalitionReconfigurationAgent agent)
		{
			if (CurrentCoalition.IsInvited(agent))
			{
				if (agent.CurrentCoalition == null)
					CurrentCoalition.Join(agent);
				// TODO: else merge coalitions (how?)
			}

		}

		private bool IsConfiguredFor(ITask task)
		{
			return BaseAgent.AllocatedRoles.Any(role => role.Task == task);
		}

		public bool GetConfigurationBounds(ITask task, out Role firstRole, out Role lastRole)
		{
			firstRole = default(Role);
			lastRole = default(Role);

			if (!IsConfiguredFor(task))
				return false;

			firstRole = BaseAgent.AllocatedRoles
								 .Where(role => role.Task == task)
								 .MinBy(role => role.PreCondition.StateLength);
			lastRole = BaseAgent.AllocatedRoles
								.Where(role => role.Task == task)
								.MaxBy(role => role.PostCondition.StateLength);
			return true;
		}
	}
}