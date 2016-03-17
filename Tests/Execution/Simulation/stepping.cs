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

	internal class Stepping : TestObject
	{
		protected override void Check()
		{
			var simulator = new Simulator(TestModel.New(new C { X = 44 }));
			var c = (C)simulator.Model.RootComponents[0];
			c.F.Activation = Activation.Suppressed;

			c.X.ShouldBe(44);

			for (var i = 0; i < 10; ++i)
			{
				simulator.FastForward(10);
				c.X.ShouldBe(44 + (i + 1) * 10);
			}

			for (var i = 0; i < 12; ++i)
			{
				simulator.Rewind(10);
				if (i < 10)
					c.X.ShouldBe(44 + (10 - i - 1) * 10);
				else
					c.X.ShouldBe(44);
			}

			for (var i = 0; i < 12; ++i)
			{
				simulator.FastForward(10);
				c.X.ShouldBe(44 + (i + 1) * 10);
			}

			simulator.Rewind(61);
			c.X.ShouldBe(103);

			c.F.Activation = Activation.Forced;
			simulator.SimulateStep();
			c.X.ShouldBe(105);

			simulator.Rewind(2);
			c.X.ShouldBe(102);

			c.F.Activation = Activation.Suppressed;
			simulator.Prune();
			c.X.ShouldBe(102);

			simulator.SimulateStep();
			c.X.ShouldBe(103);

			simulator.FastForward(10);
			c.X.ShouldBe(113);
		}

		private class C : Component
		{
			public int X;
			public readonly Fault F = new TransientFault();

			public override void Update()
			{
				X += Y;
			}

			public virtual int Y => 1;

			[FaultEffect(Fault = nameof(F))]
			public class E : C
			{
				public override int Y => 2;
			}
		}
	}
}