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

namespace Tests.Analysis.Invariants.NotViolated
{
	using SafetySharp.Modeling;
	using ISSE.SafetyChecking.Formula;
	using Shouldly;

	internal class MultipleSubformulas : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C { F = 3 };
			var d = new D { C = c };

			Formula f1 = c.F == 3;
			Formula f2 = c.F == 5;
			CheckInvariant(f1 || f2, c, d).ShouldBe(true);

			Formula f3 = c.F != 0;
			CheckInvariant(f3 && c.X, d).ShouldBe(true);
		}

		private class C : Component
		{
			public int F;
			public readonly Formula X;

			public C()
			{
				X = F == 3 || F == 5;
			}
		}

		private class D : Component
		{
			public C C;

			public override void Update()
			{
				if (Choose(true, false))
					C.F = 3;
				else
					C.F = 5;
			}
		}
	}
}