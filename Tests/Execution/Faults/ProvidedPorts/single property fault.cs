// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace Tests.Execution.Faults.ProvidedPorts
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X2 : TestModel
	{
		protected sealed override void Check()
		{
			Create(new C());
			var c = (C)RootComponents[0];

			c._f.Activation = Activation.Forced;
			c.M.ShouldBe(9);
			c.N = 4;
			c.x.ShouldBe(16);
			c.O = 19;
			c.O.ShouldBe(17);

			c._f.Activation = Activation.Suppressed;
			c.M.ShouldBe(1);
			c.N = 8;
			c.x.ShouldBe(8);
			c.O = 22;
			c.O.ShouldBe(24);
		}

		private class C : Component
		{
			public readonly TransientFault _f = new TransientFault();

			public int x;

			public virtual int N
			{
				set { x = value; }
			}

			public virtual int M => 1;

			public virtual int O
			{
				get { return x + 1; }
				set { x = value + 1; }
			}

			[FaultEffect(Fault = nameof(_f))]
			private class F : C
			{
				public override int M => 9;

				public override int N
				{
					set { x = value * value; }
				}

				public override int O
				{
					get { return x - 1; }
					set { x = value - 1; }
				}
			}
		}
	}
}