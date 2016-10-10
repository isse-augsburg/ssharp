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
	using SafetySharp.Modeling;
	using Odp;

	using IReconfigurationStrategy = Odp.IReconfigurationStrategy<Agent, Task, Resource>;
	using Role = Odp.Role<Agent, Task, Resource>;

	internal class Agent : BaseAgent<Agent, Task, Resource>
	{
		public readonly Fault ConfigurationUpdateFailed = new TransientFault();

		public Agent(params Capability[] capabilities)
		{
			_availableCapabilities = new List<ICapability>(capabilities);
		}

		protected readonly List<ICapability> _availableCapabilities;
		public override IEnumerable<ICapability> AvailableCapabilities => _availableCapabilities;

		[Hidden]
		public string Name { get; set; }

		public bool HasResource => _resource != null;

		public override void Update()
		{
			CheckAllocatedCapabilities();
			base.Update();
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

		// TODO: switch to original robot cell approach (Capability.Execute()) in ODP core?
		public override void ApplyCapability(ICapability capability)
		{
			if (capability is ProduceCapability)
				Produce(capability as ProduceCapability);
			else if (capability is ProcessCapability)
				Process(capability as ProcessCapability);
			else if (capability is ConsumeCapability)
				Consume(capability as ConsumeCapability);
			throw new NotImplementedException();
		}

		// empty implementations for physical resource transfer -- RobotAgent, CartAgent override some of these
		protected override void PickupResource(Agent source) { }
		protected override void InitiateResourceTransfer(Agent agent) { }
		protected override void EndResourceTransfer(Agent source) { }

		public void CheckAllocatedCapabilities()
		{
			// We ignore faults for unused capabilities that are currently not used to improve general model checking efficiency
			// For DCCA efficiency, it would be beneficial, however, to check for faults of all capabilities and I/O relations;
			// this is also how the ODP seems to work

			// Using ToArray() to prevent modifications of the list during iteration...
			foreach (var capability in AvailableCapabilities.ToArray())
			{
				if (!CheckAllocatedCapability(capability))
					_availableCapabilities.Remove(capability);
			}

			foreach (var input in Inputs.ToArray())
			{
				if (!CheckInput(input))
					input.Disconnect(this);
			}

			foreach (var output in Outputs.ToArray())
			{
				if (!CheckOutput(output))
					this.Disconnect(output);
			}
		}

		protected virtual bool CheckAllocatedCapability(ICapability capability)
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

		protected bool TryChooseRole(Func<Role, bool> predicate, out Role role)
		{
			foreach (var allocatedRole in AllocatedRoles)
			{
				if (predicate(allocatedRole))
				{
					role = allocatedRole;
					return true;
				}
			}

			role = default(Role);
			return false;
		}

		private bool ChooseRole()
		{
			Role chosenRole;

			// Check if we can process
			if (_resourceRequests.Count != 0)
			{
				var otherAgent = _resourceRequests[0].Source;
				var condition = _resourceRequests[0].Condition;
				if (TryChooseRole(role => role.PreCondition.Port == otherAgent &&
										  role.PreCondition.StateMatches(condition), out chosenRole))
				{
					_currentRole = chosenRole;
					return true;
				}
			}

			// Check if we can produce
			if (_resource == null)
			{
				if (TryChooseRole(role => role.PreCondition.Port == null, out chosenRole))
				{
					_currentRole = chosenRole;
					return true;
				}
			}

			// Check if we can consume
			if (_resource != null)
			{
				if (TryChooseRole(role => role.PostCondition.Port == null, out chosenRole))
				{
					_currentRole = chosenRole;
					return true;
				}
			}

			return false;
		}

		public override string ToString()
		{
			return $"{Name}: State: {_stateMachine.State}, Resource: {_resource?.Workpiece.Name}, #Requests: {_resourceRequests.Count}";
		}

		public virtual void Configure(Role role)
		{
			AllocatedRoles.Add(role);
		}

		// TODO: integrate fault effect with Odp library
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