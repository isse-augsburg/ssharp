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
	using System.Diagnostics;
	using System.Linq;
	using System.Threading.Tasks;
	using JetBrains.Annotations;
	using Modeling;
	using Reconfiguration;

	/// <summary>
	///   Represents an agent in the self-organizing system.
	/// </summary>
	public abstract class BaseAgent : Component, IAgent
	{
		// configuration options
		public static int MaximumAgentCount = 20;
		public static int MaximumResourceCount = 20;
		public static int MaximumRoleCount = 40;

		/// <summary>
		///   A unique identifier for the agent.
		/// </summary>
		public uint Id { get; }
		private static uint _maxId;

		/// <summary>
		///   Lists the capabilities an agent can apply to resources.
		/// </summary>
		[NotNull, ItemNotNull]
		public abstract IEnumerable<ICapability> AvailableCapabilities { get; }

		/// <summary>
		///   The roles allocated to the agent, according to which it processes resources.
		/// </summary>
		[NotNull]
		public IEnumerable<Role> AllocatedRoles => _allocatedRoles;
		private readonly List<Role> _allocatedRoles = new List<Role>(MaximumRoleCount);

		#region delegated responsibilities

		/// <summary>
		///   Helper class that chooses the next role the agent will execute from its <see cref="AllocatedRoles"/>.
		/// </summary>
		[Hidden, NotNull]
		protected IRoleSelector RoleSelector { get; set; }

		/// <summary>
		///   Helper class that handles step-by-step role execution for the agent.
		/// </summary>
		[NotNull]
		public RoleExecutor RoleExecutor { get; }

		#endregion

		protected BaseAgent()
		{
			Id = _maxId++;
			RoleSelector = new FairRoleSelector(this);
			RoleExecutor = new RoleExecutor(this);
		}

		public override void Update()
		{
			MicrostepScheduler.Schedule(UpdateAsync);
		}

		protected virtual async Task UpdateAsync()
		{
			Debug.WriteLine("Agent {0}: Observe", Id);
			await Observe();
			Debug.WriteLine("Agent {0}: Work", Id);
			Work();
			Debug.WriteLine("Agent {0}: Done", Id);
		}

		#region functional part

		/// <summary>
		///   The resource the agent is currently holding, if any.
		/// </summary>
		[CanBeNull]
		public Resource Resource { get; protected set; }

		// state machine for different processing steps
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

		// accepts, processes and relays resources
		private void Work()
		{
			_stateMachine
				.Transition( // abort work if current task has configuration issues
					from: new[] { States.ChooseRole, States.WaitingForResource, States.ExecuteRole, States.ResourceGiven },
					to: States.Idle,
					guard: RoleExecutor.IsExecuting && _deficientConfiguration,
					action: () =>
					{
						if (Resource != null)
							DropResource();
						RoleExecutor.EndExecution();
						_deficientConfiguration = false;
					})
				.Transition( // if currently in Output state, additionally cancel resource request at successor
					from: States.Output,
					to: States.Idle,
					guard: _deficientConfiguration,
					action: () =>
					{
						DropResource();
						RoleExecutor.Output.CancelResourceRequest(this, RoleExecutor.Role.Value.PostCondition);
						RoleExecutor.EndExecution();
						_deficientConfiguration = false;
					})
				.Transition( // see if there is work to do
					from: States.Idle,
					to: States.ChooseRole,
					action: ChooseRole)
				.Transition( // no work found (or deadlock avoidance or similar reasons)
					from: States.ChooseRole,
					to: States.Idle,
					guard: !RoleExecutor.IsExecuting)
				.Transition( // going to work on pre-existing resource (first transfer it)
					from: States.ChooseRole,
					to: States.WaitingForResource,
					guard: RoleExecutor.IsExecuting && RoleExecutor.Input != null,
					action: () => InitiateResourceTransfer(RoleExecutor.Input))
				.Transition( // going to produce new resource (no transfer necessary)
					from: States.ChooseRole,
					to: States.ExecuteRole,
					guard: RoleExecutor.IsExecuting && RoleExecutor.Input == null)
				.Transition( // actual work on resource
					from: States.ExecuteRole,
					to: States.ExecuteRole,
					guard: !RoleExecutor.IsCompleted,
					action: () => RoleExecutor.ExecuteStep())
				.Transition( // work is done -- pass resource on
					from: States.ExecuteRole,
					to: States.Output,
					guard: RoleExecutor.IsCompleted && RoleExecutor.Output != null && RoleExecutor.CanHandover, // if cannot (yet) handover, wait
					action: () => RoleExecutor.Output.ResourceReady(this, RoleExecutor.Role.Value.PostCondition))
				.Transition( // resource has been consumed
					from: States.ExecuteRole,
					to: States.Idle,
					guard: RoleExecutor.IsCompleted && Resource == null && RoleExecutor.Output == null,
					action: RoleExecutor.EndExecution);
		}

		/// <summary>
		///   Discards the currently held resource.
		/// </summary>
		/// <remarks>Override this if further action is necessary, do not forget to call the base method.</remarks>
		protected virtual void DropResource()
		{
			Resource = null;
		}

		private void ChooseRole()
		{
			var role = RoleSelector.ChooseRole(_resourceRequests);
			if (!role.HasValue)
				return;

			RoleExecutor.BeginExecution(role.Value);
			_resourceRequests.RemoveAll(request => request.Source == role.Value.Input);
		}

		/// <summary>
		///   Indicates if the agent is currently able to execute one of its <see cref="AllocatedRoles"/>.
		///   For instance, the role must not be locked and a resource to apply it to must be available (if required).
		/// </summary>
		public virtual bool CanExecute(Role role)
		{
			// producer roles and roles with open resource requests can be executed, unless they're locked
			return !role.IsLocked
				   && (role.Input == null || _resourceRequests.Any(req => role.Equals(req.Role)));
		}

		/// <summary>
		///   Find roles that accept resources from <paramref name="source"/> in the given <paramref name="condition"/>.
		/// </summary>
		[NotNull]
		private Role[] GetRoles([NotNull] BaseAgent source, Condition condition)
		{
			return AllocatedRoles.Where(role =>
				!role.IsLocked && role.Input == source && role.PreCondition.StateMatches(condition)
			).ToArray();
		}

		#region resource flow

		private readonly List<BaseAgent> _inputs = new List<BaseAgent>(MaximumAgentCount);
		private readonly List<BaseAgent> _outputs = new List<BaseAgent>(MaximumAgentCount);

		/// <summary>
		///   The agents from which this instance can receive resources.
		/// </summary>
		[NotNull, ItemNotNull]
		public virtual IEnumerable<BaseAgent> Inputs => _inputs;

		/// <summary>
		///   The agents to which this instance can send resources.
		/// </summary>
		[NotNull, ItemNotNull]
		public virtual IEnumerable<BaseAgent> Outputs => _outputs;

		public virtual void Connect([NotNull] BaseAgent successor)
		{
			if (successor == null)
				throw new ArgumentNullException(nameof(successor));

			if (!_outputs.Contains(successor))
				_outputs.Add(successor);
			if (!successor._inputs.Contains(this))
				successor._inputs.Add(this);
		}

		public virtual void BidirectionallyConnect([NotNull] BaseAgent neighbor)
		{
			Connect(neighbor);
			neighbor.Connect(this);
		}

		public virtual void Disconnect([NotNull] BaseAgent successor)
		{
			if (successor == null)
				throw new ArgumentNullException(nameof(successor));

			_outputs.Remove(successor);
			successor._inputs.Remove(this);
		}

		public virtual void BidirectionallyDisconnect(BaseAgent neighbor)
		{
			Disconnect(neighbor);
			neighbor.Disconnect(this);
		}

		#endregion

		#region resource handshake

		private readonly List<ResourceRequest> _resourceRequests
			= new List<ResourceRequest>(MaximumResourceCount);

		/// <summary>
		///   Represents a request from a neighbouring agent to accept a resource.
		/// </summary>
		public struct ResourceRequest
		{
			internal ResourceRequest([NotNull] BaseAgent source, Role role)
			{
				if (source == null)
					throw new ArgumentNullException(nameof(source));

				Source = source;
				Role = role;
			}

			/// <summary>
			///   The agent that sent the request.
			/// </summary>
			[NotNull]
			public BaseAgent Source { get; }

			/// <summary>
			///   The role that will be applied when the request is accepted.
			/// </summary>
			public Role Role { get; }
		}

		/// <summary>
		///   An agent calls this when it has chosen to accept a <see cref="ResourceRequest"/>.
		///   Notifies the agent that sent the request to begin transfer of the resource.
		/// </summary>
		/// <param name="source">The agent that sent the <see cref="ResourceRequest"/>.</param>
		protected virtual void InitiateResourceTransfer([NotNull] BaseAgent source)
		{
			source.TransferResource();
		}

		/// <summary>
		///   Checks if the agent is currently able to receive and process a resource
		///   in the given <paramref name="condition"/> from the given <paramref name="agent"/>.
		/// </summary>
		public bool CanReceive([NotNull] BaseAgent agent, Condition condition)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));

			return GetRoles(agent, condition).Length > 0;
		}

		/// <summary>
		///   Called to notify the agent that another agent wants to send it a resource.
		///   Creates and queues the appropriate <see cref="ResourceRequest"/>.
		/// </summary>
		/// <param name="agent">The agent sending the resource.</param>
		/// <param name="condition">The resource's condition.</param>
		private void ResourceReady([NotNull] BaseAgent agent, Condition condition)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));

			var roles = GetRoles(agent, condition);
			if (roles.Length == 0)
				throw new InvalidOperationException($"Agent {Id}: no role found for resource request - invariant violated!");

			foreach (var role in roles)
				_resourceRequests.Add(new ResourceRequest(agent, role));
		}

		/// <summary>
		///   Called to notify the agent an agent that previously invoked <see cref="ResourceReady"/>
		///   no longer wishes to send a resource.
		/// </summary>
		/// <param name="agent">The agent passed to <see cref="ResourceReady"/>.</param>
		/// <param name="condition">The <see cref="Condition"/> passed to <see cref="ResourceReady"/>.</param>
		private void CancelResourceRequest([NotNull] BaseAgent agent, Condition condition)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));

			_resourceRequests.RemoveAll(request => request.Source == agent && request.Role.PreCondition.StateMatches(condition));
			_stateMachine.Transition(
				from: new[] { States.ChooseRole, States.WaitingForResource },
				to: States.Idle,
				guard: RoleExecutor.IsExecuting && RoleExecutor.Role.Value.PreCondition.StateMatches(condition),
				action: RoleExecutor.EndExecution
			);
		}

		/// <summary>
		///   Notifies the agent it can now send its current resource to the receiving agent.
		/// </summary>
		protected virtual void TransferResource()
		{
			Debug.Assert(_stateMachine.State == States.Output);
			Debug.Assert(Resource != null);

			_stateMachine.Transition(
				from: States.Output,
				to: States.ResourceGiven,
				action: () => RoleExecutor.Output.TakeResource(Resource)
			);
		}

		/// <summary>
		///   Hands the agent a <paramref name="resource"/> to process.
		/// </summary>
		protected virtual void TakeResource([NotNull] Resource resource)
		{
			if (resource == null)
				throw new ArgumentNullException(nameof(resource));
			Debug.Assert(_stateMachine.State == States.WaitingForResource);

			Resource = resource;
			_stateMachine.Transition(
				from: States.WaitingForResource,
				to: States.ExecuteRole,
				action: RoleExecutor.Input.ResourcePickedUp
			);
		}

		/// <summary>
		///   Notifies the agent the resource it transferred to another agent
		///   has been picked up by the receiving agent.
		/// </summary>
		protected virtual void ResourcePickedUp()
		{
			Debug.Assert(_stateMachine.State == States.ResourceGiven);

			Resource = null;
			_stateMachine.Transition(
				from: States.ResourceGiven,
				to: States.Idle,
				action: () => RoleExecutor.EndExecution()
			);
		}

		#endregion

		#endregion

		#region observer

		/// <summary>
		///   Indicates if the agent is currently able to communicate with other agents.
		/// </summary>
		public virtual bool IsAlive => true;

		// indicates the currently executed role belongs to a task that is being reconfigured
		[Hidden]
		private bool _deficientConfiguration;

		/// <summary>
		///   The strategy used by the agent to reconfigure if configuration invariants are violated.
		/// </summary>
		[Hidden, NotNull]
		public IReconfigurationStrategy ReconfigurationStrategy { get; set; }

		/// <summary>
		///   A set of configuration invariants that are consistently monitored for violations.
		/// </summary>
		[NotNull, ItemNotNull]
		protected virtual InvariantPredicate[] MonitoringPredicates { get; } = {
			Invariant.IoConsistency,
			Invariant.NeighborsAliveGuarantee,
			Invariant.ResourceConsistency,
			Invariant.CapabilityConsistency
		};

		/// <summary>
		///   A set of configuration invariants that need only be checked immediately after a reconfiguration.
		/// </summary>
		[NotNull, ItemNotNull]
		protected virtual InvariantPredicate[] ConsistencyPredicates { get; } = {
			Invariant.PrePostConditionConsistency,
			Invariant.TaskEquality,
			Invariant.StateConsistency
		};

		/// <summary>
		///   Checks for invariant violations and performs reconfigurations if necessary.
		/// </summary>
		private async Task Observe()
		{
			var violations = FindInvariantViolations();

			await PerformReconfiguration(
				from vio in violations
				select ReconfigurationRequest.Violation(vio.Key, vio.Value.ToArray())
			);
		}

		/// <summary>
		///   Performs a reconfiguration of the given tasks.
		/// </summary>
		/// <param name="reconfigurations">A list of reconfiguration requests.</param>
		protected async Task PerformReconfiguration([NotNull] IEnumerable<ReconfigurationRequest> reconfigurations)
		{
			if (reconfigurations == null)
				throw new ArgumentNullException(nameof(reconfigurations));

			if (!reconfigurations.Any())
                return;

            // initiate reconfiguration to fix violations
            await ReconfigurationStrategy.Reconfigure(reconfigurations);
			// verify correctness of new configuration
			VerifyInvariants();
		}

        /// <summary>
        ///   Prepares the agent for reconfiguration of some given <paramref name="task"/>.
        /// </summary>
	    public void PrepareReconfiguration([NotNull] ITask task)
	    {
		    if (task == null)
			    throw new ArgumentNullException(nameof(task));

		    // stop work on deficient tasks
	        _resourceRequests.RemoveAll(request => request.Role.Task == task);
	        // abort execution of current role if necessary
	        _deficientConfiguration = RoleExecutor.IsExecuting && RoleExecutor.Task == task;
        }

		/// <summary>
		///   Verifies that the current configuration fulfills all invariants.
		///   Otherwise, an exception is thrown.
		/// </summary>
		private void VerifyInvariants()
		{
			foreach (var predicate in MonitoringPredicates.Concat(ConsistencyPredicates))
				if (predicate(this).Any())
					throw new InvalidOperationException($"New configuration for agent {Id} violates invariant: {predicate.Method.Name}");
		}

		/// <summary>
		///   Finds violations of the <see cref="MonitoringPredicates"/> by the current configuration.
		/// </summary>
		[NotNull]
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

		/// <summary>
		///   Called by other agents to request the agent partakes in a reconfiguration.
		/// </summary>
		/// <returns>A <see cref="Task"/> that completes once the entire reconfiguration has finished.</returns>
		public virtual Task RequestReconfiguration([NotNull] IAgent agent, [NotNull] ITask task)
		{
			if (agent == null)
				throw new ArgumentNullException(nameof(agent));
			if (task == null)
				throw new ArgumentNullException(nameof(task));

			return PerformReconfiguration(new[] { ReconfigurationRequest.Request(task, agent) });
		}

		/// <summary>
		///   Adds the given <paramref name="roles"/> to the agent's <see cref="AllocatedRoles"/>.
		/// </summary>
		public virtual void AllocateRoles([NotNull] IEnumerable<Role> roles)
		{
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));

			foreach (var role in roles)
			{
				Debug.Assert(role.IsValid);
#if DEBUG
				// Check for duplicate conditions between roles. While not really a problem for ODP (use case: load distribution), current
				// reconfiguration algorithms shouldn't produce such configurations. Hence this check to indicate potential problems.
				// Previously only checked for conditions with the same state, instead of exactly the same. This check is currently too strict,
				// since the coalition algorithm sometimes produces such configurations - although inefficient, they are still valid.

				var index = _allocatedRoles.FindIndex(r => r.PreCondition == role.PreCondition);
				if (index != -1)
					throw new Exception($"Duplicate precondition at agent {Id}: pre-existing role {_allocatedRoles[index]} and new role {role}");

				index = _allocatedRoles.FindIndex(r => r.PostCondition == role.PostCondition);
				if (index != -1)
					throw new Exception($"Duplicate postcondition at agent {Id}: pre-existing role {_allocatedRoles[index]} and new role {role}");
#endif
				_allocatedRoles.Add(role);
			}

			RoleSelector.OnRoleAllocationsChanged();
		}

		/// <summary>
		///   Removes the given <paramref name="roles"/> from the agent's <see cref="AllocatedRoles"/>.
		/// </summary>
		public virtual void RemoveAllocatedRoles([NotNull] IEnumerable<Role> roles)
		{
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));

			foreach (var role in roles)
				_allocatedRoles.Remove(role);
			RoleSelector.OnRoleAllocationsChanged();
		}

		/// <summary>
		///   Locks (or unlocks) the given <paramref name="roles"/> in the agent's configuration.
		/// </summary>
		public void LockRoles([NotNull] IEnumerable<Role> roles, bool locked = true)
		{
			if (roles == null)
				throw new ArgumentNullException(nameof(roles));

			var set = new HashSet<Role>(roles);
			for (var i = 0; i < _allocatedRoles.Count; ++i)
			{
				if (set.Contains(_allocatedRoles[i]))
					_allocatedRoles[i] = _allocatedRoles[i].Lock(locked);
			}
		}

		#endregion
    }
}