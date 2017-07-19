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
	using System.Diagnostics;
	using System.Linq;
	using Modeling;

	public class ControllerReconfigurationAgent : IReconfigurationAgent
	{
		// used in this class
		private readonly BaseAgent _baseAgent;
		private readonly ReconfigurationAgentHandler _reconfAgentHandler;
		private RoleCalculationAgent _roleCalculationAgent;
		private ITask _task;

		// passed on to RoleCalculationAgent
		private readonly IController _controller;

		public ControllerReconfigurationAgent(
			BaseAgent baseAgent,
			ReconfigurationAgentHandler reconfAgentHandler,
			IController controller
		)
		{
			_baseAgent = baseAgent;
			_reconfAgentHandler = reconfAgentHandler;

			_controller = controller;
		}

		public void Acknowledge()
		{
			_roleCalculationAgent.Acknowledge(_task);
		}

		public void StartReconfiguration(ITask task, IAgent agent, BaseAgent.State baseAgentState)
		{
			_task = task;

			if (agent == _baseAgent) // invariant violation detected
			{
				_roleCalculationAgent = new RoleCalculationAgent(_controller);
				_roleCalculationAgent.StartCentralReconfiguration(task, _baseAgent, baseAgentState);
			}
			else // a reconfiguration has already been already started
			{
				_roleCalculationAgent = (RoleCalculationAgent)agent; // may already have this value, if reconfiguration initiated by this instance
				_roleCalculationAgent.AcknowledgeReconfigurationRequest(task, this, baseAgentState);
			}
		}

		public void UpdateAllocatedRoles(ConfigurationUpdate config)
		{
			_reconfAgentHandler.UpdateAllocatedRoles(_task, config);
		}

		public void Go(ITask task)
		{
			_roleCalculationAgent = null;

			_reconfAgentHandler.Go(task);
			_reconfAgentHandler.Done(task);
		}

		protected class RoleCalculationAgent : IAgent
		{
			private readonly BaseAgent[] _functioningAgents;
			private readonly IController _controller;

			[NonDiscoverable, Hidden(HideElements = true)]
			private readonly Dictionary<uint, ControllerReconfigurationAgent> _reconfAgents
				= new Dictionary<uint, ControllerReconfigurationAgent>();

			private int _ackCounter = 0;

			private enum State { Idle, GatherGlobalKnowledge, CalculateRoles, AllocateRoles }
			private readonly StateMachine<State> _stateMachine = State.Idle;

			public RoleCalculationAgent(IController controller)
			{
				_controller = controller;
				_functioningAgents = _controller.Agents.Where(agent => agent.IsAlive).ToArray();
			}

			public void StartCentralReconfiguration(ITask task, BaseAgent agent, object bastate)
			{
				_stateMachine.Transition(
					from: State.Idle,
					to: State.GatherGlobalKnowledge,
					action: () => {
						foreach (var baseAgent in _functioningAgents)
							baseAgent.RequestReconfiguration(this, task); // The returned task represents the end of the reconfiguration -> don't await
					}
				);
			}

			public void AcknowledgeReconfigurationRequest(ITask task, ControllerReconfigurationAgent agent, BaseAgent.State baseAgentState)
			{
				_stateMachine.Transition(
					from: State.GatherGlobalKnowledge,
					to: State.GatherGlobalKnowledge,
					action: () => _reconfAgents.Add(baseAgentState.ID, agent)
				);
				_stateMachine.Transition(
					from: State.GatherGlobalKnowledge,
					to: State.CalculateRoles,
					guard: _reconfAgents.Count == _functioningAgents.Length,
					action: () => CalculateRoles(task)
				);
			}

			private void CalculateRoles(ITask task)
			{
				var t = _controller.CalculateConfigurations(null, task);
				Debug.Assert(t.IsCompleted); // assume synchronous controller
				var configs = t.Result;

				_stateMachine.Transition(
					from: State.CalculateRoles,
					to: State.AllocateRoles,
					guard: configs != null,
					action: () =>
					{
						foreach (var agent in _reconfAgents.Values)
							agent.UpdateAllocatedRoles(configs);
					}
				);
			}

			public void Acknowledge(ITask task)
			{
				_stateMachine.Transition(
					from: State.AllocateRoles,
					to: State.AllocateRoles,
					action: () => _ackCounter++
				);

				_stateMachine.Transition(
					from: State.AllocateRoles,
					to: State.Idle,
					guard: _ackCounter == _reconfAgents.Count,
					action: () => {
						foreach (var reconfAgent in _reconfAgents.Values)
							reconfAgent.Go(task);
					}
				);
			}
		}
	}
}