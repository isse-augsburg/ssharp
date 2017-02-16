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
	using ISSE.SafetyChecking.Formula;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;

	internal class MultiPartFormula : AnalysisTestObject
	{
		protected override void Check()
		{
			var d = new D();

			Formula f1 = d.F > 40;
			Formula f2 = d.G < 50;

			CheckInvariant(f1.Implies(f2), d).ShouldBe(true);
		}

		private class D : Component
		{
			public int F;
			public int G;

			public override void Update()
			{
				F = Choose(1, 2, 3, 4);
				G = Choose(1, 5);
			}
		}
	}
}