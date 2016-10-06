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

namespace SafetySharp.Odp
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Modeling;

    public abstract class BaseAgent<A, T, R> : Component
		where A : BaseAgent<A, T, R>
		where T : class, ITask
    {
		// configuration options
		public static int MaximumResourceCount = 30;
		public static int MaximumReconfigurationRequests = 30;
		public static int MaximumRoleCount = 30;

		public override void Update()
		{
			Observe();
			Work();
		}

		#region configuration

		public abstract ICapability[] AvailableCapabilities { get; }

		public List<Role<A,T,R>> AllocatedRoles { get; } = new List<Role<A,T,R>>(MaximumRoleCount);

		#endregion

		#region functional part

		protected virtual void Work()
		{
			_stateMachine.Transition( // abort work if current task has configuration issues
				from: new[] { State.ChooseRole, State.WaitingForResource, State.ExecuteRole, State.Output, State.ResourceGiven },
				to: State.Idle,
				guard: _hasRole && _deficientConfiguration,
				action: () =>
				{
					_hasRole = false;
					_resource = default(R);
					_deficientConfiguration = false;
				});

			_stateMachine
				.Transition( // see if there is work to do
					from: State.Idle,
					to: State.ChooseRole,
					action: ChooseRole)
				.Transition( // no work found (or deadlock avoidance or similar reasons)
					from: State.ChooseRole,
					to: State.Idle,
					guard: !_hasRole)
				.Transition( // going to work on pre-existing resource (first transfer it)
					from: State.ChooseRole,
					to: State.WaitingForResource,
					guard: _hasRole && _currentRole.PreCondition.Port != null,
					action: _currentRole.PreCondition.Port.TransferResource)
				.Transition( // going to produce new resource (no transfer necessary)
					from: State.ChooseRole,
					to: State.ExecuteRole,
					guard: _hasRole && _currentRole.PreCondition.Port == null)
				.Transition( // actual work on resource
					from : State.ExecuteRole,
					to: State.ExecuteRole,
					guard: !_currentRole.IsCompleted,
					action: ExecuteRole)
				.Transition( // work is done -- pass resource on
					from: State.ExecuteRole,
					to: State.Output,
					guard: _currentRole.IsCompleted && _currentRole.PostCondition.Port != null,
					action: () =>
					{
						_currentRole.PostCondition.Port.ResourceReady((A)this, _currentRole.PostCondition);
						RemoveResourceRequest(_currentRole.PreCondition.Port, _currentRole.PreCondition);
					})
				.Transition( // resource has been consumed
					from: State.ExecuteRole,
					to: State.Idle,
					guard: _currentRole.IsCompleted && _resource == null && _currentRole.PostCondition.Port == null,
					action: () => RemoveResourceRequest(_currentRole.PreCondition.Port, _currentRole.PreCondition));
		}

		protected readonly StateMachine<State> _stateMachine = State.Idle;

		public enum State
		{
			Idle,
			ChooseRole,
			WaitingForResource,
			ExecuteRole,
			Output,
			ResourceGiven
		}

		protected bool _hasRole;
		protected Role<A, T, R> _currentRole;
		protected R _resource;

		protected virtual void ChooseRole() { }

		protected virtual void ExecuteRole() { }

		public abstract void ApplyCapability(ICapability capability);

		#region resource flow

		// TODO: can these be hidden?
		// in pill production, yes (connections never change, only agents fail)
		// in robot cell: individual connections are removed -- but hidden in model (incorrect?)
		public virtual List<A> Inputs { get; } = new List<A>();
		public virtual List<A> Outputs { get; } = new List<A>();

		public void Connect(A successor)
		{
			if (!Outputs.Contains(successor))
				Outputs.Add(successor);
			if (!successor.Inputs.Contains(this))
				successor.Inputs.Add((A)this);
		}

		public void BidirectionallyConnect(A neighbor)
		{
			Connect(neighbor);
			neighbor.Connect((A)this);
		}

		public void Disconnect(A successor)
		{
			Outputs.Remove(successor);
			successor.Inputs.Remove((A)this);
		}

		public void BidirectionallyDisconnect(A neighbor)
		{
			Disconnect(neighbor);
			neighbor.Disconnect((A)this);
		}

		#endregion

		#region resource handshake

		protected readonly List<ResourceRequest> _resourceRequests
			= new List<ResourceRequest>(MaximumResourceCount);

		protected struct ResourceRequest
		{
			public ResourceRequest(A source, Condition<A, T> condition)
			{
				Source = source;
				Condition = condition;
			}

			public A Source { get; }
			public Condition<A, T> Condition { get; }
		}

		public virtual void ResourceReady(A agent, Condition<A, T> condition)
		{
			_resourceRequests.Add(new ResourceRequest(agent, condition));
		}

		protected virtual void RemoveResourceRequest(A agent, Condition<A, T> condition)
		{
			_resourceRequests.RemoveAll(
				request => request.Source == agent && request.Condition.StateMatches(condition));
		}

		public virtual void TransferResource()
		{
			InitiateResourceTransfer();

			_stateMachine.Transition(
				from: State.Output,
				to: State.ResourceGiven,
				action: () => _currentRole.PostCondition.Port.Resource(_resource)
			);
		}

		public virtual void Resource(R resource)
		{
			// assert resource != null

			PickupResource();
			_resource = resource;

			_stateMachine.Transition(
				from: State.WaitingForResource,
				to: State.ExecuteRole,
				action: _currentRole.PostCondition.Port.ResourcePickedUp
			);
		}

		public virtual void ResourcePickedUp()
		{
			EndResourceTransfer();
			_resource = default(R);

			_stateMachine.Transition(
				from: State.ResourceGiven,
				to: State.Idle,
				action: () => _hasRole = false
			);
		}

		#endregion

		#region physical resource transfer

		protected abstract void PickupResource();

		protected abstract void InitiateResourceTransfer();

		protected abstract void EndResourceTransfer();

		#endregion

		#endregion

		#region observer

		private bool _deficientConfiguration = false;

		protected virtual void Observe()
		{
			var inactiveNeighbors = PingNeighbors();

			var deficientTasks = _reconfigurationRequests.Select(request => request.Task)
				.Union(FindInvariantViolations(inactiveNeighbors));

			foreach (var task in deficientTasks)
			{
				// TODO: what are these values?
				object agent = null;
				object state = null;

				LockConfigurations(task);
				if (_hasRole && _currentRole.Task == task)
					_deficientConfiguration = true; // abort execution of current role

				if (!_tasksUnderReconstruction.ContainsKey(task))
				{
					_tasksUnderReconstruction.Add(task, CreateReconfigurationAgent(task));
				}
				_tasksUnderReconstruction[task].StartReconfiguration(task, agent, state);
			}
		}

		protected abstract T[] FindInvariantViolations(IEnumerable<A> inactiveNeighbors);

		#region ping

		protected readonly ISet<A> _responses = new HashSet<A>();

		protected virtual IEnumerable<A> PingNeighbors()
		{
			var neighbors = Inputs.Union(Outputs);

			// reset previous responses
			_responses.Clear();

			// ping neighboring agents to determine if they are still functioning
			foreach (var neighbor in neighbors)
				neighbor.SayHello((A)this);

			return neighbors.Except(_responses);
		}

		public virtual void SayHello(A agent)
		{
			agent.SendHello((A)this);
		}

		public virtual void SendHello(A agent)
		{
			_responses.Add(agent);
		}

		#endregion

		#endregion

		#region controller

		protected readonly Dictionary<T, IReconfigurationAgent<T>> _tasksUnderReconstruction
			= new Dictionary<T, IReconfigurationAgent<T>>();

		protected struct ReconfigurationRequest
		{
			public ReconfigurationRequest(A source, T task)
			{
				Source = source;
				Task = task;
			}

			public A Source { get; }
			public T Task { get; }
		}

		protected readonly List<ReconfigurationRequest> _reconfigurationRequests
			= new List<ReconfigurationRequest>(MaximumReconfigurationRequests);

		public virtual void RequestReconfiguration(A agent, T task)
		{
			_reconfigurationRequests.Add(new ReconfigurationRequest(agent, task));
		}

		protected abstract IReconfigurationAgent<T> CreateReconfigurationAgent(T task);

		public virtual void UpdateConfigurations(object conf)
		{
			throw new NotImplementedException();
		}

		public virtual void Go(T task)
		{
			UnlockConfigurations(task);
			_resourceRequests.RemoveAll(request => request.Condition.Task == task);
		}

		protected virtual void LockConfigurations(T task)
		{
			throw new NotImplementedException();
		}

		protected virtual void UnlockConfigurations(T task)
		{
			throw new NotImplementedException();
		}

		public virtual void Done(T task)
		{
			_tasksUnderReconstruction.Remove(task);
		}

		#endregion		
	}
}