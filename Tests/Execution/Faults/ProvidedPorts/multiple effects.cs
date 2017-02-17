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

	internal class MultipleEffects : TestModel
	{
		protected sealed override void Check()
		{
			var c = new C();
			var d = new D();

			c.F.AddEffect<C.F1>(c);
			c.F.AddEffect<C.F2>(c);
			c.F.AddEffect<D.F>(d);

			Create(c, d);
			c = (C)RootComponents[0];
			d = (D)RootComponents[1];

			c.F.Activation = Activation.Forced;
			c.M().ShouldBe(9);
			c.N().ShouldBe(17);
			d.M().ShouldBe(91);

			c.F.Activation = Activation.Suppressed;
			c.M().ShouldBe(1);
			c.N().ShouldBe(2);
			d.M().ShouldBe(-1);
		}

		private class C : Component
		{
			public readonly TransientFault F = new TransientFault();

			public virtual int M() => 1;

			public virtual int N() => 2;

			[FaultEffect]
			public class F1 : C
			{
				public override int M() => 9;
			}

			[FaultEffect]
			public class F2 : C
			{
				public override int N() => 17;
			}
		}

		private class D : Component
		{
			public virtual int M() => -1;

			[FaultEffect]
			public class F : D
			{
				public override int M() => 91;
			}
		}
	}
}