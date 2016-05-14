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

namespace Tests.Analysis.Dcca
{
	using System.Linq;
	using SafetySharp.Modeling;
	using Shouldly;

	internal class X2 : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			var result = Dcca(c.X > 4, c);

			result.CheckedSets.Count.ShouldBe(5);
			result.MinimalCriticalSets.Count.ShouldBe(1);
			result.Exceptions.ShouldBeEmpty();
			result.IsComplete.ShouldBe(true);
			result.SuppressedFaults.ShouldBeEmpty();
			result.ForcedFaults.ShouldBeEmpty();

			ShouldContain(result.CheckedSets);
			ShouldContain(result.CheckedSets, c.F1);
			ShouldContain(result.CheckedSets, c.F2);
			ShouldContain(result.CheckedSets, c.F3);
			ShouldContain(result.CheckedSets, c.F2, c.F3);

			ShouldContain(result.MinimalCriticalSets, c.F1);

			result.CounterExamples.Count.ShouldBe(1);
			foreach (var set in result.MinimalCriticalSets)
				result.CounterExamples.ContainsKey(set).ShouldBe(true);

			SimulateCounterExample(result.CounterExamples[result.MinimalCriticalSets.Single()], simulator =>
			{
				c = (C)simulator.Model.Roots[0];

				c.X.ShouldBe(0);

				while (!simulator.IsCompleted)
					simulator.SimulateStep();

				c.X.ShouldBe(17);
			});
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new PermanentFault();
			public readonly Fault F3 = new PermanentFault();
			public int X;

			public override void Update()
			{
				X = 3;
			}

			[FaultEffect(Fault = nameof(F1))]
			private class E1 : C
			{
				public override void Update()
				{
					X = 17;
				}
			}

			[FaultEffect(Fault = nameof(F2))]
			private class E2 : C
			{
				public override void Update()
				{
				}
			}

			[FaultEffect(Fault = nameof(F3))]
			private class E3 : C
			{
				public override void Update()
				{
				}
			}
		}
	}
}