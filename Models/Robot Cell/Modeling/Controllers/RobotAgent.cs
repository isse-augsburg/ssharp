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
	using Plants;
	using SafetySharp.Modeling;
	using Odp;

	public class RobotAgent : Agent, ICapabilityHandler<ProduceCapability>, ICapabilityHandler<ProcessCapability>, ICapabilityHandler<ConsumeCapability>
	{

        [Reliability(mttf: 10000, mttr: 100)]
        public readonly Fault Broken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault ResourceTransportFault = new TransientFault();

        // In analyses without hardware components, these replace the Tool.Broken faults.
        // When hardware components are included, these faults are ignored.
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault DrillBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault InsertBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault TightenBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault PolishBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault GenericABroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault GenericBBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault GenericCBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault GenericDBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault GenericEBroken = new TransientFault();
        [Reliability(mttf: 1000, mttr: 100)]
        public readonly Fault GenericFBroken = new TransientFault();

        private ICapability _currentCapability;

		[Hidden(HideElements = true)]
		private readonly List<Task> _tasks;
		private readonly List<Resource> _resources;

        // All capabilities the robot can have, provided none are broken.
        private readonly ICapability[] _capabilities;
        // For each capability, remember if the hardware ever failed to apply it. If so, it is considered defect forever.
        private readonly bool[] _plantCapabilityDefects;
        // Actually available capabilities.
        public override IEnumerable<ICapability> AvailableCapabilities => _capabilities.Where((c, i) => !_plantCapabilityDefects[i] && CheckAllocatedCapability(c));



        public RobotAgent(ICapability[] capabilities, Robot robot, List<Task> tasks, List<Resource> resources)
        {
			Robot = robot;
			_tasks = tasks;
			_resources = resources;

		    if (HasDuplicates(capabilities))
		        throw new InvalidOperationException("Duplicate capabilities have no effect.");
            _capabilities = capabilities;
            _plantCapabilityDefects = new bool[capabilities.Length];

            Broken.Name = $"{Name}.{nameof(Broken)}";
			ResourceTransportFault.Name = $"{Name}.{nameof(ResourceTransportFault)}";
	        DrillBroken.Name = $"{Name}.{nameof(DrillBroken)}";
	        InsertBroken.Name = $"{Name}.{nameof(InsertBroken)}";
	        TightenBroken.Name = $"{Name}.{nameof(TightenBroken)}";
	        PolishBroken.Name = $"{Name}.{nameof(PolishBroken)}";
            GenericABroken.Name = $"{Name}.{nameof(PolishBroken)}";
            GenericABroken.Name = $"{Name}.{nameof(GenericABroken)}";
            GenericBBroken.Name = $"{Name}.{nameof(GenericBBroken)}";
            GenericCBroken.Name = $"{Name}.{nameof(GenericCBroken)}";
            GenericDBroken.Name = $"{Name}.{nameof(GenericDBroken)}";
            GenericEBroken.Name = $"{Name}.{nameof(GenericEBroken)}";
            GenericFBroken.Name = $"{Name}.{nameof(GenericFBroken)}";

            AddTolerableFaultEffects();
		}
 
	    /* TODO: agents cannot have duplicate capabilities
           *
           * Adding duplicate capabilities to agents (RobotAgents) has no effect:
           * a robot may have multiple tools that perform the same action (e.g. multiple drills),
           * but the agent has just one corresponding capability (as long as any of the tools are
           * functioning).
           *
           * In the future, multiple capabilities should be supported, each associated with one tool.
           * This would allow for the selection of less-used tools etc.
           * To support this, we need to distinguish between functional equivalence and reference equality
           * of capabilities in SafetySharp.Odp. For example, add IsEquivalentTo(ICapability) to the
           * ICapability interface. Adjust all configuration mechanisms, agents etc. to
           * use the appropriate comparison.
           *
           * */
	    private static bool HasDuplicates(ICapability[] capabilities)
	    {
	        var set = new HashSet<ICapability>();
	        foreach (var cap in capabilities)
	        {
	            if (set.Contains(cap))
	                return true;
	            set.Add(cap);
	        }
	        return false;
	    }

        protected RobotAgent() { } // for fault effects

		public override string Name => $"R{Id}";

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
			var agent = (CartAgent)RoleExecutor.Input;

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
			var agent = (CartAgent)RoleExecutor.Output;

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
				var availableResourceCount = _resources.Count(resource => resource.Task == role.Task);
				return availableResourceCount > 0
					   && !_tasks.Any(task => task.IsResourceInProduction)
					   && base.CanExecute(role);
			}
			return base.CanExecute(role);
		}

		public void ApplyCapability(ProduceCapability capability)
		{
			var index = _resources.FindIndex(resource => resource.Task == RoleExecutor.Task);
			if (index == -1)
				throw new InvalidOperationException("All resources for this task have already been produced");

			Resource = _resources[index];
			_resources.RemoveAt(index);

			(Resource.Task as Task).IsResourceInProduction = true;
			Robot?.ProduceWorkpiece(((Resource)Resource).Workpiece);
			Resource.OnCapabilityApplied(capability);
		}

		public void ApplyCapability(ProcessCapability capability)
		{
			if (Resource == null)
				throw new InvalidOperationException("Cannot process when no resource available");

			if (!Equals(_currentCapability, capability))
			{
				// Switch the capability; if we fail to do so, remove all other capabilities from the available ones
				if (!SwitchCapability(capability))
				{
				    for (var i = 0; i < _capabilities.Length; ++i)
				    {
				        if (_capabilities[i] is ProcessCapability && !_capabilities[i].Equals(_currentCapability))
				            _plantCapabilityDefects[i] = true;
				    }
                    return;
				}

			    _currentCapability = capability;
            }

			// Apply the capability; if we fail to do so, remove it from the available ones
			if (!ApplyCurrentCapability())
			{
			    _plantCapabilityDefects[Array.IndexOf(_capabilities, capability)] = true;
                return;
			}

            Resource.OnCapabilityApplied(capability);
		}

		public void ApplyCapability(ConsumeCapability capability)
		{
			if (Resource == null)
				throw new InvalidOperationException("Cannot consume when no resource available");

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
                GenericABroken.AddEffect<GenericABrokenEffect>(this);
                GenericBBroken.AddEffect<GenericBBrokenEffect>(this);
                GenericCBroken.AddEffect<GenericCBrokenEffect>(this);
                GenericDBroken.AddEffect<GenericDBrokenEffect>(this);
                GenericEBroken.AddEffect<GenericEBrokenEffect>(this);
                GenericFBroken.AddEffect<GenericFBrokenEffect>(this);

                Broken.Subsumes(DrillBroken, InsertBroken, TightenBroken, PolishBroken, GenericABroken, GenericBBroken, GenericCBroken, GenericDBroken, GenericEBroken, GenericFBroken);
			}
		}

		[FaultEffect, Priority(5)]
		public class BrokenEffect : RobotAgent
		{
			protected override bool ApplyCurrentCapability() => false;
			protected override bool CanApply(ProcessCapability capability) => false;
			protected override bool TakeResource(Cart cart) => false;
			protected override bool PlaceResource(Cart cart) => false;

			protected override bool CheckInput(Agent agent) => false;
			protected override bool CheckOutput(Agent agent) => false;

			public override IEnumerable<ICapability> AvailableCapabilities => Enumerable.Empty<ICapability>();
		}

		[FaultEffect]
		public class ResourceTransportEffect : RobotAgent
		{
			protected override bool TakeResource(Cart cart) => false;
			protected override bool PlaceResource(Cart cart) => false;

			protected override bool CheckInput(Agent agent) => false;
			protected override bool CheckOutput(Agent agent) => false;
		}

	    // TODO: a common base class for these effects would be nice (once S# supports it)
		[FaultEffect, Priority(1)]
		public class DrillBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Drill && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Drill
					&& base.ApplyCurrentCapability();
		}

		[FaultEffect, Priority(2)]
		public class InsertBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Insert && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Insert
					&& base.ApplyCurrentCapability();
		}

		[FaultEffect, Priority(3)]
		public class TightenBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Tighten && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Tighten
					&& base.ApplyCurrentCapability();
		}

		[FaultEffect, Priority(4)]
		public class PolishBrokenEffect : RobotAgent
		{
			protected override bool CanApply(ProcessCapability capability)
				=> capability.ProductionAction != ProductionAction.Polish && base.CanApply(capability);

			protected override bool ApplyCurrentCapability()
				=> (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.Polish
					&& base.ApplyCurrentCapability();
		}

        [FaultEffect, Priority(5)]
        public class GenericABrokenEffect : RobotAgent
        {
            protected override bool CanApply(ProcessCapability capability)
                => capability.ProductionAction != ProductionAction.GenericA && base.CanApply(capability);

            protected override bool ApplyCurrentCapability()
                => (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.GenericA
                   && base.ApplyCurrentCapability();
        }

        [FaultEffect, Priority(6)]
        public class GenericBBrokenEffect : RobotAgent
        {
            protected override bool CanApply(ProcessCapability capability)
                => capability.ProductionAction != ProductionAction.GenericB && base.CanApply(capability);

            protected override bool ApplyCurrentCapability()
                => (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.GenericB
                   && base.ApplyCurrentCapability();
        }

        [FaultEffect, Priority(7)]
        public class GenericCBrokenEffect : RobotAgent
        {
            protected override bool CanApply(ProcessCapability capability)
                => capability.ProductionAction != ProductionAction.GenericC && base.CanApply(capability);

            protected override bool ApplyCurrentCapability()
                => (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.GenericC
                   && base.ApplyCurrentCapability();
        }

        [FaultEffect, Priority(8)]
        public class GenericDBrokenEffect : RobotAgent
        {
            protected override bool CanApply(ProcessCapability capability)
                => capability.ProductionAction != ProductionAction.GenericD && base.CanApply(capability);

            protected override bool ApplyCurrentCapability()
                => (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.GenericD
                   && base.ApplyCurrentCapability();
        }

        [FaultEffect, Priority(8)]
        public class GenericEBrokenEffect : RobotAgent
        {
            protected override bool CanApply(ProcessCapability capability)
                => capability.ProductionAction != ProductionAction.GenericE && base.CanApply(capability);

            protected override bool ApplyCurrentCapability()
                => (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.GenericE
                   && base.ApplyCurrentCapability();
        }

        [FaultEffect, Priority(9)]
        public class GenericFBrokenEffect : RobotAgent
        {
            protected override bool CanApply(ProcessCapability capability)
                => capability.ProductionAction != ProductionAction.GenericF && base.CanApply(capability);

            protected override bool ApplyCurrentCapability()
                => (_currentCapability as ProcessCapability)?.ProductionAction != ProductionAction.GenericF
                   && base.ApplyCurrentCapability();
        }

    }
}