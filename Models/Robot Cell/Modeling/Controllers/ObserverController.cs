// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;

	internal abstract class ObserverController : Component
	{
		public const int MaxSteps = 350;
		private bool _hasReconfed;

		[NonSerializable]
		private Tuple<Agent, Capability[]>[] _lastRoleAllocations; // for debugging purposes

		//[Hidden]
		private bool _reconfigurationRequested = true;

		[Range(0, MaxSteps, OverflowBehavior.Clamp)]
		public int _stepCount;

		public AnalysisMode Mode = AnalysisMode.AllFaults;

		public readonly Fault ReconfigurationFailure = new TransientFault();

		protected ObserverController(IEnumerable<Agent> agents, List<Task> tasks)
		{
			Tasks = tasks;
			Agents = agents.ToArray();

			foreach (var agent in Agents)
			{
				agent.ObserverController = this;
				GenerateConstraints(agent);
			}
		}

		[Hidden]
		protected List<Task> Tasks { get; }

		[Hidden(HideElements = true)]
		public Agent[] Agents { get; }

		public ReconfStates ReconfigurationState { get; protected set; } = ReconfStates.NotSet;

		protected abstract void Reconfigure();

		public void ScheduleReconfiguration()
		{
			_reconfigurationRequested = true;
		}

		private void GenerateConstraints(Agent agent)
		{
			agent.Constraints = new List<Func<bool>>
			{
#if !ENABLE_KNOWN_ERRORS
				// I/O Consistency
				() => agent.AllocatedRoles.All(role => role.PreCondition.Port == null || agent.Inputs.Contains(role.PreCondition.Port)),
				() => agent.AllocatedRoles.All(role => role.PostCondition.Port == null || agent.Outputs.Contains(role.PostCondition.Port)),
#endif
				// Capability Consistency
				() =>
					agent.AllocatedRoles.All(
						role => role.CapabilitiesToApply.All(capability => agent.AvailableCapabilities.Any(c => c.IsEquivalentTo(capability)))),
				//   Pre-PostconditionConsistency
				() =>
					agent.AllocatedRoles.Any(role => role.PostCondition.Port == null || role.PreCondition.Port == null) ||
					agent.AllocatedRoles.TrueForAll(role => PostMatching(role, agent) && PreMatching(role, agent))
			};
		}

		private bool PostMatching(Role role, Agent agent)
		{
			if (role.PostCondition.Port.AllocatedRoles.All(role1 => role1.PreCondition.Port != agent))
			{
				;
			}
			else if (
				!role.PostCondition.Port.AllocatedRoles.Any(
					role1 =>
						role.PostCondition.StateMatches(role1.PreCondition.State)))
			{
				;
			}
			else if (role.PostCondition.Port.AllocatedRoles.All(role1 => role.PostCondition.Task != role1.PreCondition.Task))
			{
				;
			}

			return role.PostCondition.Port.AllocatedRoles.Any(role1 => role1.PreCondition.Port == agent
																	   &&
																	   role.PostCondition.StateMatches(role1.PreCondition.State)
																	   && role.PostCondition.Task == role1.PreCondition.Task);
		}

		private bool PreMatching(Role role, Agent agent)
		{
			return role.PreCondition.Port.AllocatedRoles.Any(role1 => role1.PostCondition.Port == agent
																	  && role.PreCondition.StateMatches(role1.PostCondition.State)
																	  && role.PreCondition.Task == role1.PostCondition.Task);
		}

		public override void Update()
		{
			if (Mode == AnalysisMode.IntolerableFaults)
			{
				++_stepCount;
				if (_stepCount >= MaxSteps)
					return;
			}

			if (ReconfigurationState == ReconfStates.Failed)
				return;

			foreach (var agent in Agents)
			{
				if (_reconfigurationRequested)
					break;

				agent.Update();
			}

			if (!_reconfigurationRequested)
				return;

			if (Mode == AnalysisMode.TolerableFaults)
			{
				// This speeds up analyses when checking for reconf failures with DCCA, but is otherwise
				// unacceptable for other kinds of analyses

				if (_hasReconfed)
					return;
			}

			foreach (var agent in Agents)
				agent.CheckAllocatedCapabilities();

			foreach (var agent in Agents)
			{
				agent.AllocatedRoles.Clear();
				agent.OnReconfigured();
			}

			Reconfigure();
			_reconfigurationRequested = false;
			_hasReconfed = true;
		}

		/// <summary>
		///   Applies the <paramref name="roleAllocations" /> to the system.
		/// </summary>
		/// <param name="roleAllocations">The sequence of agents and the capabilities they should execute.</param>
		protected virtual void ApplyConfiguration(Tuple<Agent, Capability[]>[] roleAllocations)
		{
			foreach (var task in Tasks)
				task.IsResourceInProduction = false;

			_lastRoleAllocations = roleAllocations;
			var allocatedCapabilities = 0;

			for (var i = 0; i < roleAllocations.Length; i++)
			{
				var agent = roleAllocations[i].Item1;
				var capabilities = roleAllocations[i].Item2;

				var preAgent = i == 0 ? null : roleAllocations[i - 1].Item1;
				var postAgent = i == roleAllocations.Length - 1 ? null : roleAllocations[i + 1].Item1;

				var preCondition = new Condition(Tasks[0], preAgent, allocatedCapabilities);
				var postCondition = new Condition(Tasks[0], postAgent, allocatedCapabilities + capabilities.Length);
				var role = new Role(preCondition, postCondition, allocatedCapabilities, capabilities.Length);

				allocatedCapabilities += capabilities.Length;

				agent.Configure(role);
			}
		}

		[FaultEffect(Fault = nameof(ReconfigurationFailure))]
		public abstract class ReconfigurationFailureEffect : ObserverController
		{
			protected ReconfigurationFailureEffect(IEnumerable<Agent> agents, List<Task> tasks)
				: base(agents, tasks)
			{
			}

			protected override void ApplyConfiguration(Tuple<Agent, Capability[]>[] roleAllocations)
			{
				ReconfigurationState = ReconfStates.Failed;
			}
		}
	}
}