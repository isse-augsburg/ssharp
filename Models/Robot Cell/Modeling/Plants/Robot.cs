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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Plants
{
	using System;
	using System.Linq;
	using Controllers;
	using SafetySharp.Modeling;

	internal class Robot : Component
	{
		private Tool _currentTool;

		private Workpiece _workpiece;

		public Fault Broken = new TransientFault();
		//public Fault SwitchFault = new TransientFault();
		public Fault SwitchToWrongToolFault = new TransientFault();

		/// <summary>
		/// Creates a new robot.
		/// </summary>
		/// <param name="capabilities">The robot's processing capabilities.</param>
		/// <remarks>For each capability, one tool with the corresponding ProductionAction is created.
		/// In contrast to <see cref="RobotAgent"/>, duplicate capabilities ARE allowed
		/// and lead to the creation of multiple tools with the same ProductionAction.</remarks>
		public Robot(params ProcessCapability[] capabilities)
		{
			Tools = capabilities.Select(c => new Tool(c.ProductionAction)).ToArray();
			Broken.Subsumes(Tools.Select(tool => tool.Broken));
			// TODO: Broken.Subsumes(ResourceTransportFault)
		}

		protected Robot() { } // for fault effects

		[Hidden(HideElements = true)]
		public Tool[] Tools { get; }

		[Hidden]
		public string Name { get; set; }

		public bool HasWorkpiece => _workpiece != null;

		public virtual bool CanApply(ProcessCapability capability)
		{
			return Tools.Any(t => t.ProductionAction == capability.ProductionAction && t.CanApply());
		}

		public virtual bool CanSwitch()
		{
			return true;
		}

		public virtual bool ApplyCapability()
		{
			return _currentTool.Apply(_workpiece);
		}

		public virtual bool SwitchCapability(ProcessCapability capability)
		{
			_currentTool = Tools.First(t => t.ProductionAction == capability.ProductionAction && t.CanApply());
			return true;
		}

		public virtual bool TakeResource(Cart cart)
		{
			if (!cart.IsPositionedAt(this))
				return false;

			Workpiece.Transfer(ref cart.LoadedWorkpiece, ref _workpiece);
			return true;
		}

		public virtual bool PlaceResource(Cart cart)
		{
			if (!cart.IsPositionedAt(this))
				return false;

			Workpiece.Transfer(ref _workpiece, ref cart.LoadedWorkpiece);
			return true;
		}

		public void SetNames(int robotId)
		{
			Name = $"R{robotId}";
			Broken.Name = $"R{robotId}.Broken";
			//SwitchFault.Name = $"R{robotId}.ToolSwitchFailed";
			SwitchToWrongToolFault.Name = $"R{robotId}.WrongToolSelected";

			foreach (var group in Tools.GroupBy(t => t.ProductionAction))
			{
				var tools = group.ToArray();
				for (var i = 0; i < tools.Length; i++)
					tools[i].Broken.Name = $"R{robotId}.{tools[i].ProductionAction}{i + 1}";
			}
		}

		public void ProduceWorkpiece(Workpiece workpiece)
		{
			if (_workpiece != null)
				throw new InvalidOperationException("There is already a workpiece located at the robot.");

			_workpiece = workpiece;
		}

		public void ConsumeWorkpiece()
		{
			if (_workpiece == null)
				throw new InvalidOperationException("There is no workpiece located at the robot.");

			_workpiece = null;
		}

		public void DiscardWorkpiece()
		{
			_workpiece?.Discard();
			_workpiece = null;
		}

		public override string ToString()
		{
			return $"{Name}: Workpiece: {_workpiece?.Name}";
		}

		public virtual bool CanTransfer() => true;

		internal void AddTolerableFaultEffects(Fault resourceTransportFault)
		{
			resourceTransportFault.AddEffect<ResourceTransportEffect>(this);
		}

		[FaultEffect(Fault = nameof(Broken)), Priority(2)]
		internal class BrokenEffect : Robot
		{
			public override bool ApplyCapability() => false;
			public override bool CanApply(ProcessCapability capability) => false;
			public override bool TakeResource(Cart cart) => false;
			public override bool PlaceResource(Cart cart) => false;
			public override bool CanTransfer() => false;
		}

		//[FaultEffect(Fault = nameof(SwitchFault)), Priority(1)]
		//internal class SwitchEffect : Robot
		//{
		//	public override bool SwitchCapability(ProcessCapability capability) => false;
		//	public override bool CanSwitch() => false;
		//}

		[FaultEffect(Fault = nameof(SwitchToWrongToolFault)), Priority(1)]
		internal class SwitchToWrongToolEffect : Robot
		{
			public override bool SwitchCapability(ProcessCapability capability)
			{
				_currentTool = Choose(Tools);
				return true;
			}
		}

		internal class ResourceTransportEffect : Robot
		{
			public override bool TakeResource(Cart cart) => false;
			public override bool PlaceResource(Cart cart) => false;
			public override bool CanTransfer() => false;
		}
	}
}