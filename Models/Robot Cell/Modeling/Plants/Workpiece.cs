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
	using SafetySharp.Modeling;

	internal class Workpiece : Component
	{
		[Hidden(HideElements = true)]
		private readonly ProductionAction[] _productionActions;

		private int _productionStep;

		public Fault IncorrectlyPositionedFault = new TransientFault();
		public Fault ToolApplicationFailed = new TransientFault();

		public Workpiece(params ProductionAction[] productionActions)
		{
			_productionActions = productionActions;
			Range.Restrict(_productionStep, 0, _productionActions.Length, OverflowBehavior.Error);
		}

		[Hidden]
		public string Name { get; set; }

		public bool IsDamaged { get; private set; }

		public bool IsComplete => _productionStep == _productionActions.Length;
		public bool IsDiscarded { get; private set; }

		public void Discard()
		{
			IsDiscarded = true;
		}

		public virtual void Apply(ProductionAction action)
		{
			IsDamaged |= _productionActions.Length <= _productionStep || _productionActions[_productionStep] != action;
			if (!IsDamaged)
				++_productionStep;
		}

		public static void Transfer(ref Workpiece origin, ref Workpiece destination)
		{
			if (origin == null)
				throw new InvalidOperationException("There is no workpiece at the origin.");

			if (destination != null)
				throw new InvalidOperationException("There already is a workpiece at the destination.");

			destination = origin;
			origin = null;
		}

		public override string ToString()
		{
			if (IsDamaged)
				return $"{Name}: IsDamaged";

			if (IsComplete)
				return $"{Name}: IsComplete";

			return $"{Name}: {_productionStep}/{_productionActions.Length}";
		}

		[FaultEffect(Fault = nameof(IncorrectlyPositionedFault)), Priority(1)]
		public class IncorrectlyPositionedEffect : Workpiece
		{
			public override void Apply(ProductionAction action)
			{
				IsDamaged = true;
			}
		}

		[FaultEffect(Fault = nameof(ToolApplicationFailed)), Priority(2)]
		public class ToolApplicationFailedEffect : Workpiece
		{
			public override void Apply(ProductionAction action)
			{
			}
		}
	}
}