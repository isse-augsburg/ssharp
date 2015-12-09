// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X3 : TestModel
	{
		protected sealed override void Check()
		{
			Create(new C());
			var c = (C)RootComponents[0];

			c._f1.ActivationMode = ActivationMode.Never;
			c._f2.ActivationMode = ActivationMode.Never;
			c._f3.ActivationMode = ActivationMode.Never;
			c.M(1).ShouldBe(1);

			c._f1.ActivationMode = ActivationMode.Always;
			c._f2.ActivationMode = ActivationMode.Never;
			c._f3.ActivationMode = ActivationMode.Never;
			c.M(1).ShouldBe(101);

			c._f1.ActivationMode = ActivationMode.Never;
			c._f2.ActivationMode = ActivationMode.Always;
			c._f3.ActivationMode = ActivationMode.Never;
			c.M(1).ShouldBe(1001);

			c._f1.ActivationMode = ActivationMode.Never;
			c._f2.ActivationMode = ActivationMode.Never;
			c._f3.ActivationMode = ActivationMode.Always;
			c.M(1).ShouldBe(10001);

			c._f1.ActivationMode = ActivationMode.Always;
			c._f2.ActivationMode = ActivationMode.Always;
			c._f3.ActivationMode = ActivationMode.Never;
			c.M(1).ShouldBe(1101);

			c._f1.ActivationMode = ActivationMode.Always;
			c._f2.ActivationMode = ActivationMode.Never;
			c._f3.ActivationMode = ActivationMode.Always;
			c.M(1).ShouldBe(10101);

			c._f1.ActivationMode = ActivationMode.Never;
			c._f2.ActivationMode = ActivationMode.Always;
			c._f3.ActivationMode = ActivationMode.Always;
			c.M(1).ShouldBe(11001);

			c._f1.ActivationMode = ActivationMode.Always;
			c._f2.ActivationMode = ActivationMode.Always;
			c._f3.ActivationMode = ActivationMode.Always;
			c.M(1).ShouldBe(11101);
		}

		private class C : Component
		{
			public readonly TransientFault _f1 = new TransientFault();
			public readonly TransientFault _f2 = new TransientFault();
			public readonly TransientFault _f3 = new TransientFault();

			public virtual int M(int x) => x;

			[FaultEffect(Fault = nameof(_f1))]
			[Priority(1)]
			private class F1 : C
			{
				public override int M(int x) => base.M(x) + 100;
			}

			[FaultEffect(Fault = nameof(_f2))]
			[Priority(2)]
			private class F2 : C
			{
				public override int M(int x) => base.M(x) + 1000;
			}

			[FaultEffect(Fault = nameof(_f3))]
			[Priority(3)]
			private class F3 : C
			{
				public override int M(int x) => base.M(x) + 10000;
			}
		}
	}
}