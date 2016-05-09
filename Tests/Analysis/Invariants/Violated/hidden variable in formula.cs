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

namespace Tests.Analysis.Invariants.Violated
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;

	internal class HiddenVariableInFormula : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			Formula fUnequalZero = c.F != 0;

			CheckInvariant(fUnequalZero, c).ShouldBe(false);
		}

		private class C : Component
		{
			[Hidden]
			public int F;

			private int G;

			public override void Update()
			{
				// First the paths with 1 get selected and the complete state space is revealed.
				// When 2 is chosen no new state is detected and fUnequalZero is never true
				F = Choose(0, 1);
				G = (G + F) % 5;
			}
		}
	}
}