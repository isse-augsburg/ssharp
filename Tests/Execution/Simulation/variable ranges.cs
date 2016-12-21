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

namespace Tests.Execution.Simulation
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class VariableRanges : TestObject
	{
		protected override void Check()
		{
			var simulator = new SafetySharpSimulator(TestModel.InitializeModel(new C()));
			var c = (C)simulator.Model.Roots[0];

			c.X.ShouldBe(0);
			c.Y.ShouldBe(0);
			c.Z.ShouldBe(0);

			simulator.SimulateStep();
			c.X.ShouldBe(1);
			c.Y.ShouldBe(1);
			c.Z.ShouldBe(1);

			simulator.SimulateStep();
			c.X.ShouldBe(2);
			c.Y.ShouldBe(0);
			c.Z.ShouldBe(2);

			simulator.SimulateStep();
			c.X.ShouldBe(2);
			c.Y.ShouldBe(1);
			c.Z.ShouldBe(3);

			var exception = Should.Throw<RangeViolationException>(() => simulator.SimulateStep());
			exception.Field.ShouldBe(typeof(C).GetField("Z"));
			exception.Object.ShouldBe(c);
			exception.FieldValue.ShouldBe(4);
			exception.Range.LowerBound.ShouldBe(0);
			exception.Range.UpperBound.ShouldBe(3);
			exception.Range.OverflowBehavior.ShouldBe(OverflowBehavior.Error);

			simulator.Reset();
			c.X.ShouldBe(0);
			c.Y.ShouldBe(0);
			c.Z.ShouldBe(0);
		}

		private class C : Component
		{
			[Range(0, 2, OverflowBehavior.Clamp)]
			public int X;

			[Range(0, 1, OverflowBehavior.WrapClamp)]
			public int Y;

			[Range(0, 3, OverflowBehavior.Error)]
			public int Z;

			protected internal override void Initialize()
			{
				Y = 77;
			}

			public override void Update()
			{
				++X;
				++Y;
				++Z;
			}
		}
	}
}