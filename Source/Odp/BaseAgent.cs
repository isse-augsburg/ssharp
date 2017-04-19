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
	using System.Threading.Tasks;
	using Modeling;

	public abstract partial class BaseAgent : Component, IAgent
	{
		// configuration options
		public static int MaximumAgentCount = 20;
		public static int MaximumResourceCount = 20;
		public static int MaximumRoleCount = 40;

		private static uint _maxID = 0;
		public uint ID { get; }

		public abstract IEnumerable<ICapability> AvailableCapabilities { get; }

		private readonly List<Role> _allocatedRoles = new List<Role>(MaximumRoleCount);

		public IEnumerable<Role> AllocatedRoles => _allocatedRoles;

		[Hidden]
		public IRoleSelector RoleSelector { get; protected set; }

		protected BaseAgent()
		{
			ID = _maxID++;
			RoleSelector = new FairRoleSelector(this);
		}

		public override void Update()
		{
			MicrostepScheduler.Schedule(UpdateAsync);
		}

		protected virtual async Task UpdateAsync()
		{
			await Observe();
			Work();
		}

		#region functional part

		private Role _currentRole;
		private bool _hasRole;

		protected internal Role? CurrentRole => _hasRole ? (Role?)_currentRole : null;

		public Resource Resource { get; protected set; }

		private readonly StateMachine<States> _stateMachine = States.Idle;

		private enum States
		{
			Idle,
			ChooseRole,
			WaitingForResource,
			ExecuteRole,
			Output,
			ResourceGiven
		}

		private void Work()
		{
			_stateMachine.Transition( // abort work if current task has configuration issues
				from: new[] { States.ChooseRole, States.WaitingForResource, States.ExecuteRole, States.Output, States.ResourceGiven },
				to: States.Idle,
				guard: _hasRole && _deficientConfiguration,
				action: () =>
				{
					if (Resource != null)
						DropResource();
					_hasRole = false;
					_deficientConfiguration = false;
				});

			_stateMachine
				.Transition( // see if there is work to do
					from: States.Idle,
					to: States.ChooseRole,
					action: ChooseRole)
				.Transition( // no work found (or deadlock avoidance or similar reasons)
					from: States.ChooseRole,
					to: States.Idle,
					guard: !_hasRole)
				.Transition( // going to work on pre-existing resource (first transfer it)
					from: States.ChooseRole,
					to: States.WaitingForResource,
					guard: _hasRole && _currentRole.PreCondition.Port != null,
					action: () => InitiateResourceTransfer(_currentRole.PreCondition.Port))
				.Transition( // going to produce new resource (no transfer necessary)
					from: States.ChooseRole,
					to: States.ExecuteRole,
					guard: _hasRole && _currentRole.PreCondition.Port == null)
				.Transition( // actual work on resource
					from: States.ExecuteRole,
					to: States.ExecuteRole,
					guard: !_currentRole.IsCompleted,
					action: () => _currentRole.ExecuteStep(this))
				.Transition( // work is done -- pass resource on
					from: States.ExecuteRole,
					to: States.Output,
					guard: _currentRole.IsCompleted && _currentRole.PostCondition.Port != null,
					action: () => _currentRole.PostCondition.Port.ResourceReady(this, _currentRole.PostCondition))
				.Transition( // resource has been consumed
					from: States.ExecuteRole,
					to: States.Idle,
					guard: _currentRole.IsCompleted && Resource == null && _currentRole.PostCondition.Port == null,
					action: () => _hasRole = false);
		}

		protected virtual void DropResource()
		{
			Resource = null;
		}

		private void ChooseRole()
		{
			var role = RoleSelector.ChooseRole(_resourceRequests);
			_hasRole = role.HasValue;
			if (_hasRole)
			{
				_currentRole = role.Value;
				_resourceRequests.RemoveAll(request => request.Source == _currentRole.PreCondition.Port);
			}
		}

		public virtual bool CanExecute(Role role)
		{
			// producer roles and roles with open resource requests can be executed, unless they're locked
			return !role.IsLocked
				   && (role.PreCondition.Port == null || _resourceRequests.Any(req => role.Equals(req.Role)));
		}

		private Role[] GetRoles(BaseAgent source, Condition condition)
		{
			return AllocatedRoles.Where(role =>
				!role.IsLocked && role.PreCondition.Port == source && role.PreCondition.StateMatches(condition)
			).ToArray();
		}

		#region resource flow

		public List<BaseAgent> Inputs { get; } = new List<BaseAgent>(MaximumAgentCount);
		public List<BaseAgent> Outputs { get; } = new List<BaseAgent>(MaximumAgentCount);

		public void Connect(BaseAgent successor)
		{
			if (!Outputs.Contains(successor))
				Outputs.Add(successor);
			if (!successor.Inputs.Contains(this))
				successor.Inputs.Add(this);
		}

		public void BidirectionallyConnect(BaseAgent neighbor)
		{
			Connect(neighbor);
			neighbor.Connect(this);
		}

		public void Disconnect(BaseAgent successor)
		{
			Outputs.Remove(successor);
			successor.Inputs.Remove(this);
		}

		public void BidirectionallyDisconnect(BaseAgent neighbor)
		{
			Disconnect(neighbor);
			neighbor.Disconnect(this);
		}

		#endregion

		#region resource handshake

		private readonly List<ResourceRequest> _resourceRequests
			= new List<ResourceRequest>(MaximumResourceCount);

		public struct ResourceRequest
		{
			internal ResourceRequest(BaseAgent source, Role role)
			{
				Source = source;
				Role = role;
			}

			public BaseAgent Source { get; }
			public Role Role { get; }
		}

		protected virtual void InitiateResourceTransfer(BaseAgent source)
		{
			source.TransferResource();
		}

		private void ResourceReady(BaseAgent agent, Condition condition)
		{
			var roles = GetRoles(agent, condition);
			if (roles.Length == 0)
				throw new InvalidOperationException("no role found for resource request: invariant violated!");

			foreach (var role in roles)
			{
				_resourceRequests.Add(new ResourceRequest(agent, role));
			}
		}

		protected virtual void TransferResource()
		{
			_stateMachine.Transition(
				from: States.Output,
				to: States.ResourceGiven,
				action: () => _currentRole.PostCondition.Port.TakeResource(Resource)
			);
		}

		protected virtual void TakeResource(Resource resource)
		{
			// assert resource != null
			Resource = resource;

			_stateMachine.Transition(
				from: States.WaitingForResource,
				to: States.ExecuteRole,
				action: _currentRole.PreCondition.Port.ResourcePickedUp
			);
		}

		protected virtual void ResourcePickedUp()
		{
			Resource = null;

			_stateMachine.Transition(
				from: States.ResourceGiven,
				to: States.Idle,
				action: () => _hasRole = false
			);
		}

		#endregion

		#endregion

		#region observer

		public virtual bool IsAlive => true;

		[Hidden]
		private bool _deficientConfiguration = false;

		[Hidden]
		public Reconfiguration.IReconfigurationStrategy ReconfigurationStrategy { get; set; }

		protected virtual InvariantPredicate[] MonitoringPredicates { get; } = {
			Invariant.IOConsistency,
			Invariant.NeighborsAliveGuarantee,
			Invariant.ResourceConsistency,
			Invariant.CapabilityConsistency
		};

		protected virtual InvariantPredicate[] ConsistencyPredicates { get; } = {
			Invariant.PrePostConditionConsistency,
			Invariant.TaskEquality,
			Invariant.StateConsistency
		};

		private async Task Observe()
		{
			var violations = FindInvariantViolations();

			await PerformReconfiguration(
				from vio in violations
				let state = new State(this, null, vio.Value.ToArray())
				select Tuple.Create(vio.Key, state)
			);
		}

		protected async Task PerformReconfiguration(IEnumerable<Tuple<ITask, State>> reconfigurations)
		{
			var deficientTasks = new HashSet<ITask>(reconfigurations.Select(t => t.Item1));
			if (deficientTasks.Count == 0)
				return;

			// stop work on deficient tasks
			_resourceRequests.RemoveAll(request => deficientTasks.Contains(request.Role.Task));
			// abort execution of current role if necessary
			_deficientConfiguration = _hasRole && deficientTasks.Contains(_currentRole.Task);

			// initiate reconfiguration to fix violations
			await ReconfigurationStrategy.Reconfigure(reconfigurations);
			// verify correctness of new configuration
			VerifyInvariants();
		}

		private void VerifyInvariants()
		{
			foreach (var predicate in MonitoringPredicates.Concat(ConsistencyPredicates))
				if (predicate(this).Any())
					throw new InvalidOperationException("New configuration violates invariant.");
		}

		private Dictionary<ITask, IEnumerable<InvariantPredicate>> FindInvariantViolations()
		{
			var violations = new Dictionary<ITask, IEnumerable<InvariantPredicate>>();
			foreach (var predicate in MonitoringPredicates)
			{
				foreach (var violatingTask in predicate(this))
				{
					if (!violations.ContainsKey(violatingTask))
						violations.Add(violatingTask, new HashSet<InvariantPredicate>());
					(violations[violatingTask] as HashSet<InvariantPredicate>).Add(predicate);
				}
			}
			return violations;
		}

		public virtual Task RequestReconfiguration(IAgent agent, ITask task)
		{
			return PerformReconfiguration(new[] {
				Tuple.Create(task, new State(this, agent))
			});
		}

		public virtual void AllocateRoles(IEnumerable<Role> roles)
		{
			_allocatedRoles.AddRange(roles);
			RoleSelector.OnRoleAllocationsChanged();
		}

		public virtual void RemoveAllocatedRoles(IEnumerable<Role> roles)
		{
			foreach (var role in roles.ToArray())
				_allocatedRoles.Remove(role);
			RoleSelector.OnRoleAllocationsChanged();
		}

		public void LockRoles(IEnumerable<Role> roles, bool locked = true)
		{
			var set = new HashSet<Role>(roles);
			for (var i = 0; i < _allocatedRoles.Count; ++i)
			{
				if (set.Contains(_allocatedRoles[i]))
					_allocatedRoles[i] = _allocatedRoles[i].Lock(locked);
			}
		}

		#endregion


	    public static bool operator ==(BaseAgent a, BaseAgent b)
	    {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

	        return a.ID == b.ID;
	    }

	    public static bool operator !=(BaseAgent a, BaseAgent b)
	    {
	        return !(a == b);
	    }

	    public override bool Equals(System.Object obj)
	    {
            // If parameter cannot be cast to ThreeDPoint return false:
            BaseAgent p = obj as BaseAgent;
            if ((object)p == null)
            {
                return false;
            }

            return base.Equals(obj) && ID == p.ID;
        }

	    public bool Equals(BaseAgent a)
	    {
	        return base.Equals((BaseAgent)a) && ID == a.ID;
	    }

	}
}