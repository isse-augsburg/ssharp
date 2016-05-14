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

namespace SafetySharp.CaseStudies.ProductionCell.Modeling.Plants
{
	using System.Linq;
	using Controllers;
	using SafetySharp.Modeling;

	internal class Robot : Component
	{
		[Hidden(HideElements = true)]
		private readonly Tool[] _tools;

		private Tool _currentTool;
		private Workpiece _workpiece;

		public Fault ApplyFault = new PermanentFault();
		public Fault ResourceTransportFault = new PermanentFault();
		public Fault SwitchFault = new PermanentFault();

		public Robot(params Capability[] capabilities)
		{
			_tools = capabilities.Select(c => new Tool(c.ProductionAction)).ToArray();
		}

		public Robot()
		{
		}

		public virtual bool ApplyCapability()
		{
			return _currentTool.Apply(_workpiece);
		}

		public virtual bool SwitchCapability(Capability capability)
		{
			_currentTool = _tools.First(t => t.ProductionAction == capability.ProductionAction);
			return true;
		}

		public virtual bool TakeResource(Cart cart)
		{
			Workpiece.Transfer(ref cart.LoadedWorkpiece, ref _workpiece);
			return true;
		}

		public virtual bool PlaceResource(Cart cart)
		{
			Workpiece.Transfer(ref _workpiece, ref cart.LoadedWorkpiece);
			return true;
		}

		public void SetNames(int robotId)
		{
			ApplyFault.Name = $"R{robotId}.ApplyFailed";
			SwitchFault.Name = $"R{robotId}.SwitchFailed";
			ResourceTransportFault.Name = $"R{robotId}.TransportFailed";

			foreach (var group in _tools.GroupBy(t => t.ProductionAction))
			{
				var tools = group.ToArray();
				for (var i = 0; i < tools.Length; i++)
					tools[i].Broken.Name = $"R{robotId}.{tools[i].ProductionAction}{i}";
			}
		}

		[FaultEffect(Fault = nameof(ApplyFault))]
		internal class ApplyEffect : Robot
		{
			public override bool ApplyCapability() => false;
		}

		[FaultEffect(Fault = nameof(SwitchFault))]
		internal class SwitchEffect : Robot
		{
			public override bool SwitchCapability(Capability capability) => false;
		}

		[FaultEffect(Fault = nameof(ResourceTransportFault))]
		internal class ResourceTransportEffect : Robot
		{
			public override bool TakeResource(Cart cart) => false;
			public override bool PlaceResource(Cart cart) => false;
		}
	}
}