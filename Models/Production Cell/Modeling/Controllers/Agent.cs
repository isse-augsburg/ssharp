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

namespace SafetySharp.CaseStudies.ProductionCell.Modeling.Controllers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;

	internal class Agent : Component
	{
		private readonly Stack<Agent> _requests = new Stack<Agent>(Model.MaxAgentRequests);
		private readonly StateMachine<State> _stateMachine = State.Idle;
		protected Role _currentRole;

		public Agent(params Capability[] capabilities)
		{
			AvailableCapabilites.AddRange(capabilities);
			AllocatedRoles.Capacity = Math.Max(1, capabilities.Length);
		}

		public List<Capability> AvailableCapabilites { get; } = new List<Capability>();
		public List<Role> AllocatedRoles { get; } = new List<Role>();

		[Hidden(HideElements = true)]
		public List<Agent> Outputs { get; } = new List<Agent>();

		[Hidden(HideElements = true)]
		public List<Agent> Inputs { get; } = new List<Agent>();

		[Hidden]
		public string Name { get; set; }

		[Hidden]
		public ObserverController ObserverController { get; set; }

		public Resource Resource { get; set; }

		public bool HasResource => Resource != null;

		public static void Connect(Agent from, Agent to)
		{
			from.Outputs.Add(to);
			to.Inputs.Add(from);
		}

		public static void Disconnect(Agent from, Agent to)
		{
			from.Outputs.Remove(to);
			to.Inputs.Remove(from);
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
					to: State.ExecuteRole,
					guard: Resource != null,
					action: () => _currentRole.PreCondition.Port.ResourcePickedUp())
				.Transition(
					from: State.ExecuteRole,
					to: State.ExecuteRole,
					guard: !_currentRole.IsCompleted,
					action: () => _currentRole.Execute(this))
				.Transition(
					from: State.ExecuteRole,
					to: State.Output,
					guard: _currentRole.IsCompleted && _currentRole.PostCondition.Port != null && Resource != null,
					action: () =>
					{
						_currentRole.PostCondition.Port.ResourceReady(this);
						_currentRole.Reset();
					})
				.Transition(
					from: State.ExecuteRole,
					to: State.Idle,
					guard: Resource == null || (_currentRole.IsCompleted && _currentRole.PostCondition.Port == null),
					action: () => _currentRole.Reset())
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

			// Check if we can process
			if (_requests.Count == 0)
				return false;

			var otherAgent = _requests.Pop();
			_currentRole = AllocatedRoles.FirstOrDefault(role => role.PreCondition.Port == otherAgent);

			return true;
		}

		private void ResourceReady(Agent otherAgent)
		{
			if (_requests.Count < Model.MaxAgentRequests && !_requests.Contains(otherAgent))
				_requests.Push(otherAgent);
		}

		public override string ToString()
		{
			return $"{Name}: State: {_stateMachine.State}, HasResource: {HasResource}";
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
	}
}