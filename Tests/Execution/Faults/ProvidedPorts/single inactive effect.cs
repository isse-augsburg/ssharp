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
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class InactiveEffect : TestModel
	{
		protected sealed override void Check()
		{
			Create(new C());
			var c = (C)RootComponents[0];

			c.M().ShouldBe(1);

			int r;
			c.M(out r);
			r.ShouldBe(2);
		}

		private class C : Component
		{
			public virtual int M() => 1;

			public virtual void M(out int r) => r = 2;

			[FaultEffect]
			private class F : C
			{
				public override int M() => 9;

				public override void M(out int r)
				{
					base.M(out r);
					r += 2;
				}
			}
		}
	}
}