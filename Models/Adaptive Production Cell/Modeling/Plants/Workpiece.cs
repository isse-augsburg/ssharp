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
	using System;
	using SafetySharp.Modeling;

	internal class Workpiece : Component
	{
		[Hidden(HideElements = true)]
		private readonly ProductionAction[] _productionActions;

		private int _productionStep;

		public Workpiece(params ProductionAction[] productionActions)
		{
			_productionActions = productionActions;
			Range.Restrict(_productionStep, 0, _productionActions.Length, OverflowBehavior.Error);
		}

		public bool IsDamaged { get; private set; }

		public void Apply(ProductionAction action)
		{
			IsDamaged |= _productionActions[_productionStep] != action;
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
	}
}