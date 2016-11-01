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
		public readonly Fault Broken = new TransientFault();
		public readonly Fault ResourceTransportFault = new TransientFault();

		// In analyses without hardware components, these replace the Tool.Broken faults.
		// When hardware components are included, these faults are ignored.
		public readonly Fault DrillBroken = new TransientFault();
		public readonly Fault InsertBroken = new TransientFault();
		public readonly Fault TightenBroken = new TransientFault();
		public readonly Fault PolishBroken = new TransientFault();

		private ICapability _currentCapability;

		public RobotAgent(ICapability[] capabilities, Robot robot)
			: base(capabilities)
		{
			Robot = robot;

			Broken.Name = $"{Name}.{nameof(Broken)}";
			ResourceTransportFault.Name = $"{Name}.{nameof(ResourceTransportFault)}";

			AddTolerableFaultEffects();
		}

		protected RobotAgent() { } // for fault effects

		public override string Name => $"R{ID}";

		public Robot Robot { get; }

		protected override void DropResource()
		{
			// For now, the resource disappears magically...
			Robot?.DiscardWorkpiece();

			base.DropResource();
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

		protected override void TakeResource(Odp.Resource resource)
		{
			var agent = (CartAgent)_currentRole.PreCondition.Port;

			// If we fail to transfer the resource, the robot loses all of its connections
			if (TakeResource(agent.Cart))
			{
				base.TakeResource(resource);
				return;
			}

			Robot?.DiscardWorkpiece();
			ClearConnections();
		}

		protected override void TransferResource()
		{
			var agent = (CartAgent)_currentRole.PostCondition.Port;

			// If we fail to transfer the resource, the robot loses all of its connections
			if (PlaceResource(agent.Cart))
			{
				base.TransferResource(); // inform the cart
				return;
			}

			Robot?.DiscardWorkpiece();
			ClearConnections();
		}

		private void ClearConnections()
		{
			// Using ToArray() to prevent removal during collection iteration

			foreach (var input in Inputs.ToArray())
				input.Disconnect(this);

			foreach (var output in Outputs.ToArray())
				Disconnect(output);
		}

		public override bool CanExecute(Role role)
		{
			if (role.CapabilitiesToApply.FirstOrDefault() is ProduceCapability)
			{
				var capability = (ProduceCapability)role.CapabilitiesToApply.First();
				return (capability.Resources.Count > 0 && !capability.Tasks.Any(task => task.IsResourceInProduction))
					   && base.CanExecute(role);
			}
			return base.CanExecute(role);
		}

		public override void Produce(ProduceCapability capability)
		{
			Resource = capability.Resources[0];
			capability.Resources.RemoveAt(0);
			(Resource.Task as Task).IsResourceInProduction = true;
			Robot?.ProduceWorkpiece((Resource as Resource).Workpiece);
			Resource.OnCapabilityApplied(capability);
		}

		public override void Process(ProcessCapability capability)
		{
			if (Resource == null)
				return;

			if (!Equals(_currentCapability, capability))
			{
				// Switch the capability; if we fail to do so, remove all other capabilities from the available ones
				if (SwitchCapability(capability))
					_currentCapability = capability;
				else
				{
					_availableCapabilities.RemoveAll(c => !c.Equals(_currentCapability));
					return;
				}
			}

			// Apply the capability; if we fail to do so, remove it from the available ones
			if (!ApplyCurrentCapability())
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
			Robot?.ConsumeWorkpiece();
			(Resource.Task as Task).IsResourceInProduction = false;
			Resource = null;
		}

		// robot delegation methods
		// This enables tolerable faults to be analyzed both with and without hardware models.
		protected virtual bool CanSwitchTools()			=> Robot?.CanSwitch() ?? true;
		protected virtual bool ApplyCurrentCapability() => Robot?.ApplyCapability() ?? true;
		protected virtual bool TakeResource(Cart cart)	=> Robot?.TakeResource(cart) ?? true;
		protected virtual bool PlaceResource(Cart cart)	=> Robot?.PlaceResource(cart) ?? true;
		protected virtual bool CanApply(ProcessCapability capability)			=> Robot?.CanApply(capability) ?? true;
		protected virtual bool SwitchCapability(ProcessCapability capability)	=> Robot?.SwitchCapability(capability) ?? true;

		private void AddTolerableFaultEffects()
		{
			Broken.Subsumes(ResourceTransportFault);
			if (Robot != null)
				Robot.AddTolerableFaultEffects(Broken, ResourceTransportFault);
			else
			{
				Broken.AddEffect<BrokenEffect>(this);
				ResourceTransportFault.AddEffect<ResourceTransportEffect>(this);

				DrillBroken.AddEffect<DrillBrokenEffect>(this);
				InsertBroken.AddEffect<InsertBrokenEffect>(this);
				TightenBroken.AddEffect<TightenBrokenEffect>(this);
				PolishBroken.AddEffect<PolishBrokenEffect>(this);

				Broken.Subsumes(DrillBroken, InsertBroken, TightenBroken, PolishBroken);
			}
		}

		[FaultEffect, Priority(5)]
		internal class BrokenEffect : RobotAgent
		{
			protected override bool ApplyCurrentCapability() => false;
			protected override bool CanApply(ProcessCapability capability) => false;
			protected override bool TakeResource(Cart cart) => false;
			protected override bool PlaceResource(Cart cart) => false;

			protected override bool CheckInput(Agent agent) => false;
			protected override bool CheckOutput(Agent agent) => false;
		}

		[FaultEffect]
		internal class ResourceTransportEffect : RobotAgent
		{
			protected override bool TakeResource(Cart cart) => false;
			protected override bool PlaceResource(Cart cart) => false;

			protected override bool CheckInput(Agent agent) => false;
			protected override bool CheckOutput(Agent agent) => false;
		}

		// TODO: a common base class for these effects would be nice (once S# supports it)
		[FaultEffect, Priority(1)]
		internal class DrillBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Drill && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Drill
					&& base.ApplyCurrentCapability();
		}

		[FaultEffect, Priority(2)]
		internal class InsertBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Insert && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Insert
					&& base.ApplyCurrentCapability();
		}

		[FaultEffect, Priority(3)]
		internal class TightenBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Tighten && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Tighten
					&& base.ApplyCurrentCapability();
		}

		[FaultEffect, Priority(4)]
		internal class PolishBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Polish && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Polish
					&& base.ApplyCurrentCapability();
		}
	}
}