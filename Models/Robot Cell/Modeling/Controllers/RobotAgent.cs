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
	using System.Linq;
	using Plants;
	using Odp;

	internal class RobotAgent : Agent
	{
		private Capability _currentCapability;

		public RobotAgent(Capability[] capabilities, Robot robot)
			: base(capabilities)
		{
			Robot = robot;
		}

		public Robot Robot { get; }

		protected override void DropResource()
		{
			_currentCapability = null;
			base.DropResource();

			// For now, the resource disappears magically...
			Robot.DiscardWorkpiece();
		}

		protected override bool CheckInput(Agent agent)
		{
			return Robot.CanTransfer();
		}

		protected override bool CheckOutput(Agent agent)
		{
			return Robot.CanTransfer();
		}

		protected override bool CheckAllocatedCapability(ICapability capability)
		{
			if (!Robot.CanSwitch())
				return false;

			var processCapability = capability as ProcessCapability;
			return processCapability == null || Robot.CanApply(processCapability);
		}

		// TODO: move or remove code
		protected void PickupResource(Agent agent)
		{
			// If we fail to transfer the resource, the robot loses all of its connections
			if (Robot.TakeResource(((CartAgent)agent).Cart))
				return;

			Robot.DiscardWorkpiece();
			ClearConnections();
		}

		// TODO: move or remove code
		protected void InitiateResourceTransfer(Agent agent)
		{
			// If we fail to transfer the resource, the robot loses all of its connections
			if (Robot.PlaceResource(((CartAgent)agent).Cart))
				return;

			Robot.DiscardWorkpiece();
			ClearConnections();
			CheckConstraints();
		}

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
			Robot.ProduceWorkpiece(Resource.Workpiece);
			Resource.OnCapabilityApplied();
		}

		public override void Process(ProcessCapability capability)
		{
			if (Resource == null)
				return;

			if (_currentCapability != capability)
			{
				// Switch the capability; if we fail to do so, remove all other capabilities from the available ones and
				// trigger a reconfiguration
				if (Robot.SwitchCapability(capability))
					_currentCapability = capability;
				else
				{
					_availableCapabilities.RemoveAll(c => c != _currentCapability);
					CheckConstraints();
					return;
				}
			}

			// Apply the capability; if we fail to do so, remove it from the available ones and trigger a reconfiguration
			if (!Robot.ApplyCapability())
			{
				_availableCapabilities.Remove(capability);
				CheckConstraints();
			}
			else
			{
				if (Resource.State.Count() == Resource.Task.RequiredCapabilities.Length)
					throw new InvalidOperationException();

				Resource.OnCapabilityApplied();
			}
		}

		public override void Consume(ConsumeCapability capability)
		{
			if (Resource == null)
				return;

			Resource.OnCapabilityApplied();
			Robot.ConsumeWorkpiece();
			Resource.Task.IsResourceInProduction = false;
			Resource = null;
		}
	}
}