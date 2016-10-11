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

    public abstract partial class BaseAgent<TAgent, TTask, TResource> : Component
		where TAgent : BaseAgent<TAgent, TTask, TResource>
		where TTask : class, ITask
    {
		// configuration options
		public static int MaximumAgentCount = 20;
		public static int MaximumResourceCount = 30;
		public static int MaximumReconfigurationRequests = 30;
		public static int MaximumRoleCount = 100;

		public abstract IEnumerable<ICapability> AvailableCapabilities { get; }

		public List<Role<TAgent, TTask, TResource>> AllocatedRoles { get; } = new List<Role<TAgent, TTask, TResource>>(MaximumRoleCount);

		public override void Update()
		{
			Observe();
			Work();
		}

		#region functional part

		protected Role<TAgent, TTask, TResource>? _currentRole;

		public TResource Resource { get; protected set; }

		private readonly StateMachine<State> _stateMachine = State.Idle;

		private void Work()
		{
			_stateMachine.Transition( // abort work if current task has configuration issues
				from: new[] { State.ChooseRole, State.WaitingForResource, State.ExecuteRole, State.Output, State.ResourceGiven },
				to: State.Idle,
				guard: _currentRole != null && _deficientConfiguration,
				action: () =>
				{
					DropResource();
					_currentRole = null;
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
					guard: _currentRole == null)
				.Transition( // going to work on pre-existing resource (first transfer it)
					from: State.ChooseRole,
					to: State.WaitingForResource,
					guard: _currentRole?.PreCondition.Port != null,
					action: () => InitiateResourceTransfer(_currentRole?.PreCondition.Port))
				.Transition( // going to produce new resource (no transfer necessary)
					from: State.ChooseRole,
					to: State.ExecuteRole,
					guard: _currentRole != null && _currentRole?.PreCondition.Port == null)
				.Transition( // actual work on resource
					from : State.ExecuteRole,
					to: State.ExecuteRole,
					guard: !_currentRole.Value.IsCompleted,
					action: () => _currentRole?.ExecuteStep((TAgent)this))
				.Transition( // work is done -- pass resource on
					from: State.ExecuteRole,
					to: State.Output,
					guard: _currentRole.Value.IsCompleted && _currentRole?.PostCondition.Port != null,
					action: () =>
					{
						_currentRole?.PostCondition.Port.ResourceReady((TAgent)this, _currentRole.Value.PostCondition);
						_currentRole?.Reset();
						RemoveResourceRequest(_currentRole?.PreCondition.Port, _currentRole.Value.PreCondition);
					})
				.Transition( // resource has been consumed
					from: State.ExecuteRole,
					to: State.Idle,
					guard: _currentRole.Value.IsCompleted && Resource == null && _currentRole?.PostCondition.Port == null,
					action: () => {
						_currentRole?.Reset();
						RemoveResourceRequest(_currentRole?.PreCondition.Port, _currentRole.Value.PreCondition);
						_currentRole = null;
					});
		}

		private enum State
		{
			Idle,
			ChooseRole,
			WaitingForResource,
			ExecuteRole,
			Output,
			ResourceGiven
		}

		protected virtual void DropResource()
		{
			Resource = default(TResource);
		}

		private void ChooseRole()
		{
			// Fairness is guaranteed by using the oldest resource request (_resourceRequests is a queue).
			// This includes production roles: eventually, there are no more _resourceRequests (because all
			// resources have been processed and consumed) and thus the agent eventually chooses a
			// production role again.
			//
			// TODO: prioritization, deadlock avoidance
			// (see chapters 6.3 & 6.4, Konstruktion selbst-organisierender Softwaresysteme)

			// try processing or consuming
			if (_resourceRequests.Count > 0)
				if (TryChooseRole(role => _resourceRequests[0].Source == role.PreCondition.Port
					&& role.PreCondition.StateMatches(_resourceRequests[0].Condition), out _currentRole))
					return;

			// try producing
			TryChooseRole(role => role.PreCondition.Port == null, out _currentRole);
		}

		private bool TryChooseRole(Predicate<Role<TAgent, TTask, TResource>> predicate, out Role<TAgent, TTask, TResource>? chosenRole)
		{
			foreach (var role in AllocatedRoles)
			{
				if (predicate(role))
				{
					chosenRole = role;
					return true;
				}
			}

			chosenRole = null;
			return false;
		}

		public abstract void ApplyCapability(ICapability capability);

		#region resource flow

		public List<TAgent> Inputs { get; } = new List<TAgent>();
		public List<TAgent> Outputs { get; } = new List<TAgent>();

		public void Connect(TAgent successor)
		{
			if (!Outputs.Contains(successor))
				Outputs.Add(successor);
			if (!successor.Inputs.Contains(this))
				successor.Inputs.Add((TAgent)this);
		}

		public void BidirectionallyConnect(TAgent neighbor)
		{
			Connect(neighbor);
			neighbor.Connect((TAgent)this);
		}

		public void Disconnect(TAgent successor)
		{
			Outputs.Remove(successor);
			successor.Inputs.Remove((TAgent)this);
		}

		public void BidirectionallyDisconnect(TAgent neighbor)
		{
			Disconnect(neighbor);
			neighbor.Disconnect((TAgent)this);
		}

		#endregion

		#region resource handshake

		private readonly List<ResourceRequest> _resourceRequests
			= new List<ResourceRequest>(MaximumResourceCount);

		private struct ResourceRequest
		{
			public ResourceRequest(TAgent source, Condition<TAgent, TTask> condition)
			{
				Source = source;
				Condition = condition;
			}

			public TAgent Source { get; }
			public Condition<TAgent, TTask> Condition { get; }
		}

		protected virtual void InitiateResourceTransfer(TAgent source)
		{
			source.TransferResource();
		}

		public virtual void ResourceReady(TAgent agent, Condition<TAgent, TTask> condition)
		{
			_resourceRequests.Add(new ResourceRequest(agent, condition));
		}

		protected virtual void RemoveResourceRequest(TAgent agent, Condition<TAgent, TTask> condition)
		{
			_resourceRequests.RemoveAll(
				request => request.Source == agent && request.Condition.StateMatches(condition));
		}

		public virtual void TransferResource()
		{
			_stateMachine.Transition(
				from: State.Output,
				to: State.ResourceGiven,
				action: () => _currentRole?.PostCondition.Port.TakeResource(Resource)
			);
		}

		public virtual void TakeResource(TResource resource)
		{
			// assert resource != null
			Resource = resource;

			_stateMachine.Transition(
				from: State.WaitingForResource,
				to: State.ExecuteRole,
				action: _currentRole.Value.PreCondition.Port.ResourcePickedUp
			);
		}

		public virtual void ResourcePickedUp()
		{
			Resource = default(TResource);

			_stateMachine.Transition(
				from: State.ResourceGiven,
				to: State.Idle,
				action: () => _currentRole = null
			);
		}

		#endregion

		#endregion

		#region observer

		[Hidden]
		private bool _deficientConfiguration = false;

		[Hidden]
		public IReconfigurationStrategy<TAgent, TTask, TResource> ReconfigurationStrategy { get; set; }

		private readonly List<ReconfigurationRequest> _reconfigurationRequests
			= new List<ReconfigurationRequest>(MaximumReconfigurationRequests);

		public delegate IEnumerable<TTask> InvariantPredicate(TAgent agent);

		protected virtual InvariantPredicate[] MonitoringPredicates => new InvariantPredicate[] {
			Invariant.IOConsistency,
			// Invariant.ResourceConsistency,
			Invariant.CapabilityConsistency
		};

		protected virtual InvariantPredicate[] ConsistencyPredicates => new InvariantPredicate[] {
			Invariant.PrePostConditionConsistency,
			Invariant.TaskEquality,
			Invariant.StateConsistency
		};

		private void Observe()
		{
			// find tasks that need to be reconfigured
			var inactiveNeighbors = PingNeighbors();
			var deficientTasks = new HashSet<TTask>(
				_reconfigurationRequests.Select(request => request.Task)
				.Union(FindInvariantViolations(inactiveNeighbors).Select(kv => kv.Key))
			);
			// TODO: pass invariant violation info to reconf strategy

			// stop work on deficient tasks
			_resourceRequests.RemoveAll(request => deficientTasks.Contains(request.Condition.Task));
			// abort execution of current role if necessary
			_deficientConfiguration = deficientTasks.Contains(_currentRole?.Task);

			// initiate reconfiguration to fix violations, satisfy requests
			ReconfigurationStrategy.Reconfigure(deficientTasks);
			_reconfigurationRequests.Clear();
		}

		private Dictionary<TTask, IEnumerable<InvariantPredicate>> FindInvariantViolations(IEnumerable<TAgent> inactiveNeighbors)
		{
			var violations = new Dictionary<TTask, IEnumerable<InvariantPredicate>>();
			foreach (var predicate in MonitoringPredicates)
			{
				foreach (var violatingTask in predicate((TAgent)this))
				{
					if (!violations.ContainsKey(violatingTask))
						violations.Add(violatingTask, new HashSet<InvariantPredicate>());
					(violations[violatingTask] as HashSet<InvariantPredicate>).Add(predicate);
				}
			}
			return violations;
		}

		public virtual void RequestReconfiguration(TAgent agent, TTask task)
		{
			_reconfigurationRequests.Add(new ReconfigurationRequest(agent, task));
		}

		private struct ReconfigurationRequest
		{
			public ReconfigurationRequest(TAgent source, TTask task)
			{
				Source = source;
				Task = task;
			}

			public TAgent Source { get; }
			public TTask Task { get; }
		}

		#region ping

		private readonly List<TAgent> _responses = new List<TAgent>(MaximumAgentCount);

		// TODO: abstract from ping mechanism again?
		protected virtual IEnumerable<TAgent> PingNeighbors()
		{
			var neighbors = Inputs.Union(Outputs);

			// reset previous responses
			_responses.Clear();

			// ping neighboring agents to determine if they are still functioning
			foreach (var neighbor in neighbors)
				neighbor.SayHello((TAgent)this);

			return neighbors.Except(_responses);
		}

		public virtual void SayHello(TAgent agent)
		{
			agent.SendHello((TAgent)this);
		}

		public virtual void SendHello(TAgent agent)
		{
			_responses.Add(agent);
		}

		#endregion

		#endregion
	}
}