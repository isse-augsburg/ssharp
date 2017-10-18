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
		}

		public Coalition CurrentCoalition { get; set; }
		public InvariantPredicate[] ViolatedPredicates { get; private set; } = { };
		public bool IsInitialConfiguration { get; private set; }

		private TaskCompletionSource<object> _acknowledgment;

		void IReconfigurationAgent.Acknowledge()
		{
			_acknowledgment?.SetResult(null);
		}

		void IReconfigurationAgent.StartReconfiguration(ITask task, IAgent agent, ReconfigurationReason reason)
		{
			MicrostepScheduler.Schedule(() => ReconfigureAsync(task, agent, reason));
		}

		private async Task ReconfigureAsync(ITask task, IAgent agent, ReconfigurationReason reason)
		{
			ViolatedPredicates = (reason as InvariantsViolated)?.ViolatedPredicates ?? new InvariantPredicate[0];
			IsInitialConfiguration = reason is InitialReconfiguration;

			var participationRequest = reason as ParticipationRequested;
			if (participationRequest != null)
			{
				var source = (CoalitionReconfigurationAgent)participationRequest.RequestingAgent;
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
				var configs = await _controller.CalculateConfigurationsAsync(this, task);
				if (configs != null)
				{
					await Task.WhenAll(CurrentCoalition.Members
													   .Select(member => member.UpdateConfiguration(configs)));

					foreach (var member in CurrentCoalition.Members)
						member.ConcludeReconfiguration();
				}
			}
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