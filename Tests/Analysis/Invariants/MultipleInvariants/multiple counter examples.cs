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

namespace Tests.Analysis.Invariants.StateGraph
{
	using SafetySharp.Modeling;
	using ISSE.SafetyChecking.Formula;
	using Shouldly;

	internal class Steps : AnalysisTestObject
	{
		protected override void Check()
		{
			const int count = 31;
			const int start = 2;

			var c = new C { X = start };
			var formulas = new Formula[count];

			for (var i = 0; i < count; ++i)
			{
				var j = i;
				formulas[i] = c.X != start + j;
			}

			var results = CheckInvariants(c, formulas);

			for (var i = 0; i < count; ++i)
			{
				results[i].ShouldBe(false);
				CounterExamples[i].ShouldNotBeNull();
				CounterExamples[i].StepCount.ShouldBe(i + 1);

				SimulateCounterExample(CounterExamples[i], simulator =>
				{
					c = (C)simulator.Model.Roots[0];

					c.X.ShouldBe(start);

					for (var j = start + 1; j < start + i; ++j)
					{
						simulator.SimulateStep();
						c.X.ShouldBe(j);
						simulator.IsCompleted.ShouldBe(false);
					}

					simulator.SimulateStep();
					c.X.ShouldBe(start + i);
					simulator.IsCompleted.ShouldBe(true);
				});
			}
		}

		private class C : Component
		{
			[Range(0, 50, OverflowBehavior.Clamp)]
			public int X;

			public override void Update()
			{
				++X;
			}
		}
	}
}