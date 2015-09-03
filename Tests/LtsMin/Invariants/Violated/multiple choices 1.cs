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

namespace Tests.LtsMin.Invariants.Violated
{
	using SafetySharp.Modeling;
	using Shouldly;

	internal class MultipleChoices1 : LtsMinTestObject
	{
		protected override void Check()
		{
			var c = new C { F = 3 };
			var d = new D { C = c };
			var m = new Model(d);

			CheckInvariant(m, c.F != 2).ShouldBe(false);
			CheckInvariant(m, c.F != 10).ShouldBe(false);
			CheckInvariant(m, c.F != 20).ShouldBe(false);
			CheckInvariant(m, c.F != 12).ShouldBe(false);
			CheckInvariant(m, c.F != 22).ShouldBe(false);
			CheckInvariant(m, c.F == 2 || c.F == 10 || c.F == 20 || c.F == 12 || c.F == 22).ShouldBe(false);
		}

		private class C : Component
		{
			public int F;
			public bool G;
		}

		private class D : Component
		{
			public C C;

			public override void Update()
			{
				var offset = 0;

				if (Choose(true, false))
					offset += 2;

				if (Choose(true, false))
					offset += 10;
				else if (Choose(true, false))
					offset += 20;

				C.F = offset;
			}
		}
	}
}