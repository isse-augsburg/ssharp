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
	using System.Diagnostics;
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

			Debug.WriteLine("{0} created for agent {1}", nameof(CoalitionReconfigurationAgent), baseAgent.Id);
			Debug.Flush();
		}

		public Coalition CurrentCoalition { get; set; }
		public ReconfigurationReason ReconfigurationReason { get; private set; }

		private TaskCompletionSource<object> _acknowledgment;

		void IReconfigurationAgent.Acknowledge()
		{
			_acknowledgment?.SetResult(null);
		}

		void IReconfigurationAgent.StartReconfiguration(ReconfigurationRequest reconfiguration)
		{
			MicrostepScheduler.Schedule(() => ReconfigureAsync(reconfiguration));
		}

		private async Task ReconfigureAsync(ReconfigurationRequest reconfiguration)
		{
			ReconfigurationReason = reconfiguration.Reason;

			var participationRequest = reconfiguration.Reason as ReconfigurationReason.ParticipationRequested;
			if (participationRequest == null) // start own reconfiguration
			{
				await PerformReconfiguration(reconfiguration);
				return;
			}

			var source = (CoalitionReconfigurationAgent)participationRequest.RequestingAgent;
			Debug.WriteLine("Agent {0} received a participation request from agent {1}", BaseAgent.Id, source.BaseAgent.Id);

			if (CurrentCoalition != null)
			{
				Debug.WriteLine("Agent {0} is already part of a coalition with leader {1}", BaseAgent.Id, CurrentCoalition.Leader.BaseAgent.Id);
				Debug.Assert(CurrentCoalition.Leader != source, "Invited agent that is already a coalition member.");

				CurrentCoalition.MergeCoalition(source);
				source.CurrentCoalition.AwaitRendezvous(invitedAgent: this, leader: CurrentCoalition.Leader);
			}
			else
			{
				Debug.WriteLine("Agent {0} is joining coalition with leader {1}", BaseAgent.Id, source.BaseAgent.Id);
				source.ReceiveResponse(respondingAgent: this);
			}
		}

		private async Task PerformReconfiguration(ReconfigurationRequest reconfiguration)
		{
			var configs = await _controller.CalculateConfigurationsAsync(this, reconfiguration.Task);

			// Check whether reconf was successful or if the coalition was merged into another one and disbanded.
			// In the latter case, do nothing and wait for the merged coalition to complete.
			if (CurrentCoalition.Leader != this)
				return;

			// Reconfiguration was successful -- apply new configuration to members
			await Task.WhenAll(CurrentCoalition.Members
											   .Select(member => member.UpdateConfiguration(configs)));

			foreach (var member in CurrentCoalition.Members)
				member.ConcludeReconfiguration();
		}

		/// <summary>
		/// Receives a response from an agent that received a reconfiguration request from this instance.
		/// </summary>
		/// <param name="respondingAgent">The agent responding to the reconfiguration request</param>
		private void ReceiveResponse(CoalitionReconfigurationAgent respondingAgent)
		{
			Debug.Assert(respondingAgent.CurrentCoalition == null);
			CurrentCoalition.Join(respondingAgent);
		}

		/// <summary>
		/// Distributes the calculated configuration to the coalition members.
		/// </summary>
		private async Task UpdateConfiguration(ConfigurationUpdate configs)
		{
			_acknowledgment = new TaskCompletionSource<object>();
			_reconfAgentHandler.UpdateAllocatedRoles(CurrentCoalition.Task, configs);
			await _acknowledgment.Task;
			_acknowledgment = null;

			_reconfAgentHandler.Go(CurrentCoalition.Task);
		}

		private void ConcludeReconfiguration()
		{
			_reconfAgentHandler.Done(CurrentCoalition.Task);
		}
	}
}