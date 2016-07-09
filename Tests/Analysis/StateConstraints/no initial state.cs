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

namespace Tests.Analysis.StateConstraints
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;

	internal class NoInitialState : AnalysisTestObject
	{
		protected override void Check()
		{
			var exception = Should.Throw<AnalysisException>(() => CheckInvariant(true, new C()));
			exception.CounterExample.StepCount.ShouldBe(0);

			SimulateCounterExample(exception.CounterExample, simulator =>
			{
				var c = (C)simulator.Model.Roots[0];

				c.X.ShouldBe(0);
				simulator.IsCompleted.ShouldBe(true);
			});
		}

		private class C : Component
		{
			[Range(0, 20, OverflowBehavior.Clamp)]
			public int X;

			public C()
			{
				AddStateConstraint(X != 0);
			}

			public override void Update()
			{
				++X;
			}
		}
	}
}