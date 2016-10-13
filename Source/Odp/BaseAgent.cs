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

    public abstract partial class BaseAgent<TAgent, TTask> : Component, IAgent
		where TAgent : BaseAgent<TAgent, TTask>
		where TTask : class, ITask
    {
		// configuration options
		public static int MaximumAgentCount = 20;
		public static int MaximumResourceCount = 30;
		public static int MaximumReconfigurationRequests = 30;
		public static int MaximumRoleCount = 100;

		public abstract IEnumerable<ICapability> AvailableCapabilities { get; }

		// TODO: AllocatedRoles must be consistent with _applicationTimes -- no outside modifications!!
		public List<Role<TAgent, TTask>> AllocatedRoles { get; } = new List<Role<TAgent, TTask>>(MaximumRoleCount);
		private readonly List<uint> _applicationTimes = new List<uint>();

		private uint _timeStamp = 0;
		private uint _roleApplications = 0;

		public override void Update()
		{
			_timeStamp++;
			Observe();
			Work();
		}

		#region functional part

		protected Role<TAgent, TTask>? _currentRole;

		public Resource<TTask> Resource { get; protected set; }

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
				guard: _currentRole != null && _deficientConfiguration,
				action: () =>
				{
					DropResource();
					_currentRole = null;
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
					guard: _currentRole == null)
				.Transition( // going to work on pre-existing resource (first transfer it)
					from: States.ChooseRole,
					to: States.WaitingForResource,
					guard: _currentRole?.PreCondition.Port != null,
					action: () => InitiateResourceTransfer(_currentRole?.PreCondition.Port))
				.Transition( // going to produce new resource (no transfer necessary)
					from: States.ChooseRole,
					to: States.ExecuteRole,
					guard: _currentRole != null && _currentRole?.PreCondition.Port == null)
				.Transition( // actual work on resource
					from : States.ExecuteRole,
					to: States.ExecuteRole,
					guard: !_currentRole.Value.IsCompleted,
					action: () => _currentRole?.ExecuteStep((TAgent)this))
				.Transition( // work is done -- pass resource on
					from: States.ExecuteRole,
					to: States.Output,
					guard: _currentRole.Value.IsCompleted && _currentRole?.PostCondition.Port != null,
					action: () =>
					{
						_currentRole?.PostCondition.Port.ResourceReady((TAgent)this, _currentRole.Value.PostCondition);
						_currentRole?.Reset();
					})
				.Transition( // resource has been consumed
					from: States.ExecuteRole,
					to: States.Idle,
					guard: _currentRole.Value.IsCompleted && Resource == null && _currentRole?.PostCondition.Port == null,
					action: () => {
						_currentRole?.Reset();
						_currentRole = null;
					});
		}

		protected virtual void DropResource()
		{
			Resource = null;
		}

		public abstract void ApplyCapability(ICapability capability);

		#region role selection algorithm

		// fair role selection algorithm
		// (Konstruktion selbst-organisierender Softwaresysteme, section 6.3)
		//
		// TODO: deadlock avoidance
		// (Konstruktion selbst-organisierender Softwaresysteme, section 6.4)

		private void ChooseRole()
		{
			// producer roles and roles with open resource requests can be chosen, unless they're locked
			var candidateRoles = AllocatedRoles.Where(role => !role.IsLocked
				&& (role.PreCondition.Port == null || _resourceRequests.Any(req => role.Equals(req.Role))));

			if (candidateRoles.Any())
			{
				// fair role selection
				_currentRole = candidateRoles.Aggregate(ChooseRole);

				// update data
				_roleApplications++;
				_applicationTimes[AllocatedRoles.IndexOf(_currentRole.Value)] = _roleApplications;
				_resourceRequests.RemoveAll(request => request.Source == _currentRole?.PreCondition.Port);
			}
		}

		private Role<TAgent, TTask> ChooseRole(Role<TAgent, TTask> role1, Role<TAgent, TTask> role2)
		{
			var fitness1 = Fitness(role1);
			var fitness2 = Fitness(role2);

			// role with higher fitness wins
			if (fitness1 > fitness2)
				return role1;
			else if (fitness1 < fitness2)
				return role2;
			else
			{
				// same fitness => older resource request wins
				var timeStamp1 = GetTimeStamp(role1);
				var timeStamp2 = GetTimeStamp(role2);

				if (timeStamp1 <= timeStamp2)
					return role1;
				return role2;
			}
		}

		private const uint alpha = 1;
		private const uint beta = 1;
		private uint Fitness(Role<TAgent, TTask> role)
		{
			var applicationTime = _applicationTimes[AllocatedRoles.IndexOf(role)];
			return alpha * (_roleApplications - applicationTime)
				+ beta * (uint)(role.Task.RequiredCapabilities.Length - role.PreCondition.State.Count());
		}

		private uint GetTimeStamp(Role<TAgent, TTask> role)
		{
			// for roles without request (production roles) use current time
			return (from request in _resourceRequests
					where role.Equals(request.Role)
					select request.TimeStamp
				).DefaultIfEmpty(_timeStamp).Single();
		}

		private Role<TAgent, TTask>[] GetRoles(TAgent source, Condition<TAgent, TTask> condition)
		{
			return AllocatedRoles.Where(role =>
				role.PreCondition.Port == source && role.PreCondition.StateMatches(condition)
			).ToArray();
		}

		#endregion

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
			public ResourceRequest(TAgent source, Role<TAgent, TTask> role, uint timeStamp)
			{
				Source = source;
				Role = role;
				TimeStamp = timeStamp;
			}

			public TAgent Source { get; }
			public Role<TAgent, TTask> Role { get; }
			public uint TimeStamp { get; }
		}

		protected virtual void InitiateResourceTransfer(TAgent source)
		{
			source.TransferResource();
		}

		public virtual void ResourceReady(TAgent agent, Condition<TAgent, TTask> condition)
		{
			var roles = GetRoles(agent, condition);
			if (roles.Length == 0)
				throw new InvalidOperationException("no role found for resource request: invariant violated!");

			foreach (var role in roles)
			{
				_resourceRequests.Add(new ResourceRequest(agent, role, _timeStamp));
			}
		}

		public virtual void TransferResource()
		{
			_stateMachine.Transition(
				from: States.Output,
				to: States.ResourceGiven,
				action: () => _currentRole?.PostCondition.Port.TakeResource(Resource)
			);
		}

		public virtual void TakeResource(Resource<TTask> resource)
		{
			// assert resource != null
			Resource = resource;

			_stateMachine.Transition(
				from: States.WaitingForResource,
				to: States.ExecuteRole,
				action: _currentRole.Value.PreCondition.Port.ResourcePickedUp
			);
		}

		public virtual void ResourcePickedUp()
		{
			Resource = null;

			_stateMachine.Transition(
				from: States.ResourceGiven,
				to: States.Idle,
				action: () => _currentRole = null
			);
		}

		#endregion

		#endregion

		#region observer

		public virtual bool IsAlive => true;

		[Hidden]
		private bool _deficientConfiguration = false;

		[Hidden]
		public IReconfigurationStrategy<TAgent, TTask> ReconfigurationStrategy { get; set; }

		public delegate IEnumerable<TTask> InvariantPredicate(TAgent agent);

		protected virtual InvariantPredicate[] MonitoringPredicates => new InvariantPredicate[] {
			Invariant.IOConsistency,
			Invariant.NeighborsAliveGuarantee,
			Invariant.ResourceConsistency,
			Invariant.CapabilityConsistency
		};

		// TODO: use to verify configuration
		protected virtual InvariantPredicate[] ConsistencyPredicates => new InvariantPredicate[] {
			Invariant.PrePostConditionConsistency,
			Invariant.TaskEquality,
			Invariant.StateConsistency
		};

		private void Observe()
		{
			var violations = FindInvariantViolations();

			PerformReconfiguration(
				from vio in violations
				let state = new State(this, null, vio.Value.ToArray())
				select Tuple.Create(vio.Key, state)
			);
		}

		protected void PerformReconfiguration(IEnumerable<Tuple<TTask, State>> reconfigurations)
		{
			var deficientTasks = new HashSet<TTask>(reconfigurations.Select(t => t.Item1));

			// stop work on deficient tasks
			_resourceRequests.RemoveAll(request => deficientTasks.Contains(request.Role.Task));
			// abort execution of current role if necessary
			_deficientConfiguration = deficientTasks.Contains(_currentRole?.Task);

			// initiate reconfiguration to fix violations
			ReconfigurationStrategy.Reconfigure(reconfigurations);
		}

		private Dictionary<TTask, IEnumerable<InvariantPredicate>> FindInvariantViolations()
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

		public virtual void RequestReconfiguration(IAgent agent, TTask task)
		{
			PerformReconfiguration(new[] {
				Tuple.Create(task, new State(this, agent))
			});
		}

		#endregion
	}
}