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
	using System.Linq;
	using Plants;
	using SafetySharp.Modeling;
	using Odp;

	internal class RobotAgent : Agent
	{
		public Fault ResourceTransportFault = new TransientFault();

		private ICapability _currentCapability;

		public RobotAgent(ICapability[] capabilities, Robot robot)
			: base(capabilities)
		{
			Robot = robot;
			robot?.AddTolerableFaultEffects(ResourceTransportFault);

			ResourceTransportFault.Name = $"{Name}.ResourceTransportFailed"; // TODO: Name is null at this point
		}

		public Robot Robot { get; }

		protected override void DropResource()
		{
			_currentCapability = null;
			base.DropResource();

			// For now, the resource disappears magically...
			Robot?.DiscardWorkpiece();
		}

		protected override bool CheckInput(Agent agent)
		{
			return Robot?.CanTransfer() ?? true;
		}

		protected override bool CheckOutput(Agent agent)
		{
			return Robot?.CanTransfer() ?? true;
		}

		protected override bool CheckAllocatedCapability(ICapability capability)
		{
			if (!CanSwitchTools())
				return false;

			var processCapability = capability as ProcessCapability;
			return processCapability == null || CanApply(processCapability);
		}

		private bool CanSwitchTools() => Robot?.CanSwitch() ?? true;
		private bool CanApply(ProcessCapability capability) => Robot?.CanApply(capability) ?? true;

		protected override void TakeResource(Resource<Task> resource)
		{
			var agent = (CartAgent)_currentRole?.PreCondition.Port;

			// If we fail to transfer the resource, the robot loses all of its connections
			if (TakeResource(agent.Cart))
			{
				base.TakeResource(resource);
				return;
			}

			Robot.DiscardWorkpiece();
			ClearConnections();
		}

		protected virtual bool TakeResource(Cart cart) => Robot?.TakeResource(cart) ?? true;

		protected override void TransferResource()
		{
			var agent = (CartAgent)_currentRole?.PostCondition.Port;

			// If we fail to transfer the resource, the robot loses all of its connections
			if (PlaceResource(agent.Cart))
			{
				base.TransferResource(); // inform the cart
				return;
			}

			Robot.DiscardWorkpiece();
			ClearConnections();
		}

		protected virtual bool PlaceResource(Cart cart) => Robot?.PlaceResource(cart) ?? true;

		private void ClearConnections()
		{
			// Using ToArray() to prevent removal during collection iteration

			foreach (var input in Inputs.ToArray())
				input.Disconnect(this);

			foreach (var output in Outputs.ToArray())
				this.Disconnect(output);
		}

		public override void Produce(ProduceCapability capability)
		{
			if (Resource != null || capability.Resources.Count == 0 || capability.Tasks.Any(task => task.IsResourceInProduction))
				return;

			Resource = capability.Resources[0];
			capability.Resources.RemoveAt(0);
			Resource.Task.IsResourceInProduction = true;
			Robot.ProduceWorkpiece((Resource as Resource).Workpiece);
			Resource.OnCapabilityApplied(capability);
		}

		public override void Process(ProcessCapability capability)
		{
			if (Resource == null)
				return;

			if (_currentCapability != capability)
			{
				// Switch the capability; if we fail to do so, remove all other capabilities from the available ones
				if (Robot.SwitchCapability(capability))
					_currentCapability = capability;
				else
				{
					_availableCapabilities.RemoveAll(c => c != _currentCapability);
					return;
				}
			}

			// Apply the capability; if we fail to do so, remove it from the available ones
			if (!Robot.ApplyCapability())
			{
				_availableCapabilities.Remove(capability);
			}
			else
			{
				Resource.OnCapabilityApplied(capability);
			}
		}

		public override void Consume(ConsumeCapability capability)
		{
			if (Resource == null)
				return;

			Resource.OnCapabilityApplied(capability);
			Robot.ConsumeWorkpiece();
			Resource.Task.IsResourceInProduction = false;
			Resource = null;
		}

		[FaultEffect(Fault = nameof(ResourceTransportFault))]
		internal class ResourceTransportEffect : RobotAgent
		{
			public ResourceTransportEffect(ICapability[] capabilities, Robot robot) : base(capabilities, robot) { }

			protected override bool TakeResource(Cart cart) => false;
			protected override bool PlaceResource(Cart cart) => false;

			protected override bool CheckInput(Agent agent) => false;
			protected override bool CheckOutput(Agent agent) => false;
		}
	}
}