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

namespace ProductionCell
{
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Modeling;

	internal class Agent : Component
	{
		public List<Capability> AvailableCapabilites { get; set; }
		public List<OdpRole> AllocatedRoles { get; set; }
		public bool IsCart => !(this is Robot);
		public List<Agent> Outputs { get; set; } = new List<Agent>();
		public List<Agent> Inputs { get; set; } = new List<Agent>();

		//	public Resource Resource { get; set; }
	}

	class Cart : Agent
	{
		public readonly Fault RouteBlocked = new TransientFault();

		[FaultEffect(Fault = nameof(RouteBlocked)), Priority(2)]
		public class RouteBlockedEffect : Cart
		{
			public override void Update()
			{
				foreach (var i in Inputs)
				{
					i.Outputs.Remove(this);
				}

				foreach (var o in Outputs)
					o.Inputs.Remove(this);

				Inputs.Clear();
				Outputs.Clear();
			}
		}
	}

	internal class Robot : Agent
	{
		public readonly Fault ChangeFault = new TransientFault();
		public readonly Fault AllToolsFault = new TransientFault();

		private readonly Tool[] _tools;

		public Robot()
		{
		}

		public Robot(string name, List<Capability> capabilities)
		{
			AvailableCapabilites = capabilities;
			ChangeFault.Name += name;
			AllToolsFault.Name += name;

			_tools= capabilities.Select( c=> new Tool() { Capability = c, RemoveCapability = { Name = name + c.Type } }).ToArray();
		}

		public override void Update()
		{
			Update(_tools);

			foreach (var t in _tools)
			{
				if (t.IsDisabled)
				{
					AvailableCapabilites.Remove(t.Capability);
				}
			}
		}

		[FaultEffect(Fault = nameof(ChangeFault)), Priority(2)]
		public class ChangeFaultEffect : Robot
		{
			public override void Update()
			{
				foreach (var tool in _tools)
				{
					if (AllocatedRoles.All(r => r.CapabilitiesToApply[0] != tool.Capability))
						tool.IsDisabled = true;
				}

				base.Update();
			}
		}

		[FaultEffect(Fault = nameof(AllToolsFault)), Priority(1)]
		public class AllToolsFaultEffect : Robot
		{
			public override void Update()
			{
				foreach (var tool in _tools)
				{
					tool.IsDisabled = true;
				}

				base.Update();
			}
		}

		private class Tool : Component
		{
			public readonly Fault RemoveCapability = new TransientFault();
			public Capability Capability;

			public bool IsDisabled { get; set; }

			[FaultEffect(Fault = nameof(RemoveCapability))]
			public class RemoveCapabilityEffect : Tool
			{
				public override void Update()
				{
					IsDisabled = true;
				}
			}
		}
	}
}