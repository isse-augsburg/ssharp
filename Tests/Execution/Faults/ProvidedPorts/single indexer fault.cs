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

namespace Tests.Execution.Faults.ProvidedPorts
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class Indexer : TestModel
	{
		protected sealed override void Check()
		{
			Create(new C());
			var c = (C)RootComponents[0];

			c._f.Activation = Activation.Forced;
			c[3] = 2;
			c.x.ShouldBe(7);
			c[12L].ShouldBe(24L);
			c[2, 4] = 3;
			c.y.ShouldBe((byte)12);
			c[2, 4].ShouldBe((byte)36);

			c._f.Activation = Activation.Suppressed;
			c[3] = 2;
			c.x.ShouldBe(5);
			c[12L].ShouldBe(12L);
			c[2, 4] = 3;
			c.y.ShouldBe((byte)9);
			c[2, 4].ShouldBe((byte)15);
		}

		private class C : Component
		{
			public readonly TransientFault _f = new TransientFault();

			public int x;
			public byte y;

			public virtual int this[int i]
			{
				set { x = value + i; }
			}

			public virtual long this[long d] => d;

			public virtual byte this[byte b, byte c]
			{
				get { return (byte)(y + b + c); }
				set { y = (byte)(value + b + c); }
			}

			[FaultEffect(Fault = nameof(_f))]
			private class F : C
			{
				public override int this[int x]
				{
					set { base[x] = value * 2; }
				}

				public override long this[long d] => base[d] * 2;

				public override byte this[byte b, byte c]
				{
					get { return (byte)(base[b, c] * 2); }
					set { base[b, c] = (byte)(value * 2); }
				}
			}
		}
	}
}