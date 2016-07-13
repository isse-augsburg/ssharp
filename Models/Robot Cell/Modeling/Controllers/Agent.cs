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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using CompilerServices;
	using SafetySharp.Modeling;

	internal class Agent : Component
	{
		private readonly List<Agent> _requests = new List<Agent>(Model.MaxAgentRequests);
		private readonly StateMachine<State> _stateMachine = State.Idle;
		protected Role _currentRole;

		public Fault ConfigurationUpdateFailed = new TransientFault();

		public Agent(params Capability[] capabilities)
		{
			AvailableCapabilities = new List<Capability>(capabilities);
		}

		[Hidden(HideElements = true)]
		public List<Func<bool>> Constraints { get; set; }

		public List<Capability> AvailableCapabilities { get; }
		public List<Role> AllocatedRoles { get; } = new List<Role>(10);

		[Hidden]
		public List<Agent> Outputs { get; } = new List<Agent>();

		[Hidden]
		public List<Agent> Inputs { get; } = new List<Agent>();

		[Hidden]
		public string Name { get; set; }

		[Hidden]
		public ObserverController ObserverController { get; set; }

		public Resource Resource { get; set; }

		public bool HasResource => Resource != null;

		public static void Connect(Agent from, Agent to)
		{
			if (!from.Outputs.Contains(to))
				from.Outputs.Add(to);

			if (!to.Inputs.Contains(from))
				to.Inputs.Add(from);
		}

		public static void Disconnect(Agent from, Agent to)
		{
			from.Outputs.Remove(to);
			to.Inputs.Remove(from);
		}

		public virtual void OnReconfigured()
		{
			// For now, the resource disappears magically...
			Resource = null;
			_currentRole?.Reset();
			_currentRole = null;
			_requests.Clear();
			_stateMachine.ChangeState(State.Idle); // Todo: This is a bit of a hack
		}

		protected void CheckConstraints()
		{
			var constraints = Constraints.Select(constraint => constraint());
			if (constraints.Any(constraint => !constraint))
			{
				ObserverController.ScheduleReconfiguration();
				return;
			}
		}

		public virtual void Produce(ProduceCapability capability)
		{
		}

		public virtual void Process(ProcessCapability capability)
		{
		}

		public virtual void Consume(ConsumeCapability capability)
		{
		}

		public virtual void TakeResource(Agent agent)
		{
		}

		public virtual void PlaceResource(Agent agent)
		{
		}

		public void CheckAllocatedCapabilities()
		{
			// We ignore faults for unused capabilities that are currently not used to improve general model checking efficiency
			// For DCCA efficiency, it would be beneficial, however, to check for faults of all capabilities and I/O relations;
			// this is also how the ODP seems to work

			// Using ToArray() to prevent modifications of the list during iteration...
			foreach (var capability in AvailableCapabilities.ToArray())
			{
				if (!CheckAllocatedCapability(capability))
					AvailableCapabilities.Remove(capability);
			}

			foreach (var input in Inputs.ToArray())
			{
				if (!CheckInput(input))
					Disconnect(input, this);
			}

			foreach (var output in Outputs.ToArray())
			{
				if (!CheckOutput(output))
					Disconnect(this, output);
			}
		}

		protected virtual bool CheckAllocatedCapability(Capability capability)
		{
			return true;
		}

		protected virtual bool CheckInput(Agent agent)
		{
			return true;
		}

		protected virtual bool CheckOutput(Agent agent)
		{
			return true;
		}

		public override void Update()
		{
			_stateMachine
				.Transition(
					from: State.Idle,
					to: State.RoleChosen,
					guard: ChooseRole())
				.Transition(
					from: State.RoleChosen,
					to: State.WaitForResource,
					guard: _currentRole?.PreCondition.Port != null,
					action: () => _currentRole.PreCondition.Port.TransferResource(this))
				.Transition(
					from: State.RoleChosen,
					to: State.ExecuteRole,
					guard: _currentRole != null && _currentRole.PreCondition.Port == null)
				.Transition(
					from: State.WaitForResource,
					to: State.WaitForResource,
					guard: Resource == null,
					action: () => _currentRole.PreCondition.Port.TransferResource(this))
				.Transition(
					from: State.WaitForResource,
					to: State.ExecuteRole,
					guard: Resource != null,
					action: () => _currentRole.PreCondition.Port.ResourcePickedUp())
				.Transition(
					from: State.ExecuteRole,
					to: State.ExecuteRole,
					guard: (Resource != null || _currentRole.PreCondition.Port == null) && !_currentRole.IsCompleted,
					action: () => _currentRole.Execute(this))
				.Transition(
					from: State.ExecuteRole,
					to: State.Output,
					guard: _currentRole.IsCompleted && _currentRole.PostCondition.Port != null && Resource != null,
					action: () =>
					{
						_currentRole.PostCondition.Port.ResourceReady(this);
						_currentRole.Reset();
						_requests.Remove(_currentRole.PreCondition.Port);
					})
				.Transition(
					from: State.ExecuteRole,
					to: State.Idle,
					guard: Resource == null && (_currentRole.IsCompleted || _currentRole.PostCondition.Port == null),
					action: () =>
					{
						_currentRole.Reset();
						_requests.Remove(_currentRole.PreCondition.Port);
					})
				.Transition(
					from: State.Output,
					to: State.Output,
					guard: Resource != null,
					action: () => _currentRole.PostCondition.Port.ResourceReady(this));
		}

		private void TransferResource(Agent agent)
		{
			_stateMachine.Transition(
				from: State.Output,
				to: State.ResourceGiven,
				guard: Resource != null,
				action: () =>
				{
					agent.TakeResource(this);
					PlaceResource(agent);

					agent.Resource = Resource;
					Resource = null;
				});
		}

		private void ResourcePickedUp()
		{
			_stateMachine.Transition(from: State.ResourceGiven, to: State.Idle);
		}

		private bool ChooseRole()
		{
			// Check if we can process
			if (_requests.Count != 0)
			{
				var otherAgent = _requests[0];
				_currentRole = AllocatedRoles.FirstOrDefault(role => role.PreCondition.Port == otherAgent &&
																	 role.PreCondition.State.SequenceEqual(otherAgent.Resource.State));

				if (_currentRole != null)
					return true;
			}

			// Check if we can produce
			if (Resource == null)
			{
				_currentRole = AllocatedRoles.FirstOrDefault(role => role.PreCondition.Port == null);
				if (_currentRole != null)
					return true;
			}

			// Check if we can consume
			if (Resource != null)
			{
				_currentRole = AllocatedRoles.FirstOrDefault(role => role.PostCondition.Port == null);
				if (_currentRole != null)
					return true;
			}

			return false;
		}

		private void ResourceReady(Agent otherAgent)
		{
			if (_requests.Count < Model.MaxAgentRequests && !_requests.Contains(otherAgent))
				_requests.Add(otherAgent);
		}

		public override string ToString()
		{
			return $"{Name}: State: {_stateMachine.State}, Resource: {Resource?.Workpiece.Name}, #Requests: {_requests.Count}";
		}

		public virtual void Configure(Role role)
		{
			AllocatedRoles.Add(role);
		}

		private enum State
		{
			Idle,
			RoleChosen,
			WaitForResource,
			ExecuteRole,
			Output,
			ResourceGiven
		}

		[FaultEffect(Fault = nameof(ConfigurationUpdateFailed))]
		public class ConfigurationUpdateFailedEffect : Agent
		{
			public ConfigurationUpdateFailedEffect(params Capability[] capabilities)
				: base(capabilities)
			{
			}

			public override void Configure(Role role)
			{
			}
		}
	}
}