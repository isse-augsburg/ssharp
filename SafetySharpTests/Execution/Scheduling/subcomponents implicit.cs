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

namespace Tests.Execution.Scheduling
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X1 : TestModel
	{
		protected override void Check()
		{
			Create(new C());
			var c = (C)RootComponents[0];

			c.Update();

			c.X.ShouldBe(2);
			c.D1.X.ShouldBe(10);
			c.D1.E.X.ShouldBe(21);
			c.D2.X.ShouldBe(23);
			c.D2.E.X.ShouldBe(21);
		}

		private class C : Component
		{
			public readonly D D1 = new D { X = 4 };
			public readonly D D2 = new D { X = 17 };
			public int X;

			public override void Update()
			{
				Update(D1, D2);
				X += 2;
			}
		}

		private class D : Component
		{
			public readonly E E = new E();
			public int X;

			public override void Update()
			{
				Update(E);
				X += 6;
			}
		}

		private class E : Component
		{
			public int X;

			public override void Update()
			{
				base.Update();
				X += 21;
			}
		}
	}
}