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
	using ISSE.SafetyChecking.Formula;
	using SafetySharp.Modeling;
	using SafetySharp.Analysis;
	using Shouldly;
	using Utilities;
	using static SafetySharp.Analysis.Operators;

	internal class CtlFormula : TestModel
	{
		protected override void Check()
		{
			var x = 3;
			var c = new C { F = 3 };
			var m = InitializeModel(c);
			Formula f = AX(x == c.M() && F(c.F == 7 && EU(c.F < 0, c.F > 0)));

			Create(m, f);

			ExecutableStateFormulas.Length.ShouldBe(4);
			Formulas.Length.ShouldBe(1);
			RootComponents.Length.ShouldBe(1);

			var root = RootComponents[0];
			root.ShouldBeOfType<C>();

			((C)root).F = 0;
			ExecutableStateFormulas[0].Expression().ShouldBe(true);
			ExecutableStateFormulas[1].Expression().ShouldBe(false);
			ExecutableStateFormulas[2].Expression().ShouldBe(false);
			ExecutableStateFormulas[3].Expression().ShouldBe(false);

			((C)root).F = 3;
			ExecutableStateFormulas[0].Expression().ShouldBe(false);
			ExecutableStateFormulas[1].Expression().ShouldBe(false);
			ExecutableStateFormulas[2].Expression().ShouldBe(false);
			ExecutableStateFormulas[3].Expression().ShouldBe(true);

			((C)root).F = -3;
			ExecutableStateFormulas[0].Expression().ShouldBe(false);
			ExecutableStateFormulas[1].Expression().ShouldBe(false);
			ExecutableStateFormulas[2].Expression().ShouldBe(true);
			ExecutableStateFormulas[3].Expression().ShouldBe(false);

			((C)root).F = 7;
			ExecutableStateFormulas[0].Expression().ShouldBe(false);
			ExecutableStateFormulas[1].Expression().ShouldBe(true);
			ExecutableStateFormulas[2].Expression().ShouldBe(false);
			ExecutableStateFormulas[3].Expression().ShouldBe(true);
		}

		private class C : Component
		{
			public int F;

			public int M() => F + 3;
		}
	}
}