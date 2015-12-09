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

namespace Tests.Execution.Scheduling
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X4 : TestModel
	{
		protected override void Check()
		{
			Create(new C());
			var c = (C)RootComponents[0];

			c.Update();
			c.X.ShouldBe(3);
		}

		private class C : Component
		{
			public readonly D D1 = new D();
			public readonly D D2 = new D();
			public readonly D D3 = new D();
			public int X;

			public C()
			{
				Bind(nameof(D1.M), nameof(D1Updated));
				Bind(nameof(D2.M), nameof(D2Updated));
				Bind(nameof(D3.M), nameof(D3Updated));
			}

			private void D1Updated()
			{
				X.ShouldBe(0);
				X += 1;
			}

			private void D2Updated()
			{
				X.ShouldBe(2);
				X += 1;
			}

			private void D3Updated()
			{
				X.ShouldBe(1);
				X += 1;
			}

			public override void Update()
			{
				Update(D1, D3, D2);
			}
		}

		private class D : Component
		{
			public extern void M();

			public override void Update()
			{
				M();
			}
		}
	}
}