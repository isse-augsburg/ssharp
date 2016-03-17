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

namespace Tests.Serialization.StateLabels
{
	using SafetySharp.Modeling;
	using SafetySharp.Analysis;
	using Shouldly;
	using Utilities;

	internal class InvariantsWithClosure : TestModel
	{
		protected override void Check()
		{
			var x = 3;
			var c = new C { F = 3 };
			var m = TestModel.New(c);
			Formula f1 = c.F == 3;
			Formula f2 = c.F == 7;
			Formula f3 = c.F == x;

			Create(m, f1, f2, f3);

			StateFormulas.Length.ShouldBe(3);
			Formulas.Length.ShouldBe(3);
			RootComponents.Length.ShouldBe(1);

			var root = RootComponents[0];
			root.ShouldBeOfType<C>();

			StateFormulas[0].Expression().ShouldBe(true);
			StateFormulas[1].Expression().ShouldBe(false);
			StateFormulas[2].Expression().ShouldBe(true);

			((C)root).F = 7;
			StateFormulas[0].Expression().ShouldBe(false);
			StateFormulas[1].Expression().ShouldBe(true);
			StateFormulas[2].Expression().ShouldBe(false);
		}

		private class C : Component
		{
			public int F;
		}
	}
}