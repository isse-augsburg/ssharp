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
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using Modeling;
	using System.Diagnostics;
	using System;

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

		public Coalition CurrentCoalition { get; set; }
		public BaseAgent.State BaseAgentState { get; private set; }

		private TaskCompletionSource<object> _acknowledgment;

		void IReconfigurationAgent.Acknowledge()
		{
			_reconfAgentHandler.Go(CurrentCoalition.Task);
			_acknowledgment?.SetResult(null);
			_acknowledgment = null;
			_reconfAgentHandler.Done(CurrentCoalition.Task);
		}

		void IReconfigurationAgent.StartReconfiguration(ITask task, IAgent agent, BaseAgent.State baseAgentState)
		{
			MicrostepScheduler.Schedule(() => ReconfigureAsync(task, agent, baseAgentState));
		}

		public async Task ReconfigureAsync(ITask task, IAgent agent, BaseAgent.State baseAgentState)
		{
			BaseAgentState = baseAgentState;

			if (baseAgentState.ReconfRequestSource != null)
			{
				var source = (CoalitionReconfigurationAgent)baseAgentState.ReconfRequestSource;
				if (CurrentCoalition != null && CurrentCoalition.Leader != source)
				{
					CurrentCoalition.MergeCoalition(source);
					source.CurrentCoalition.AwaitRendezvous(invitedAgent: this, leader: CurrentCoalition.Leader);
				}
				else
					source.ReceiveResponse(respondingAgent: this);
			}
			else
			{
				var configs = await _controller.CalculateConfigurations(this, task);
				if (configs != null)
					await Task.WhenAll(CurrentCoalition.Members
						.Select(member => member.UpdateConfiguration(configs)));
			}
		}

		/// <summary>
		/// Receives a response from an agent that received a reconfiguration request from this instance.
		/// </summary>
		/// <param name="agent">The agent responding to the reconfiguration request</param>
		private void ReceiveResponse(CoalitionReconfigurationAgent respondingAgent)
		{
			Debug.Assert(respondingAgent.CurrentCoalition == null);
			CurrentCoalition.Join(respondingAgent);
		}

		/// <summary>
		/// Distributes the calculated configuration to the coalition members.
		/// </summary>
		private Task UpdateConfiguration(ConfigurationUpdate configs)
		{
			_acknowledgment = new TaskCompletionSource<object>();
			_reconfAgentHandler.UpdateAllocatedRoles(configs);
			return _acknowledgment.Task;
		}
	}
}