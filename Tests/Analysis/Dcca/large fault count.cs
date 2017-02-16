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
	using ISSE.SafetyChecking.MinimalCriticalSetAnalysis;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using ISSE.SafetyChecking.Modeling;
	using Shouldly;

	internal class LargeFaultCount : AnalysisTestObject
	{
		private const int FaultCount = 60;

		protected override void Check()
		{
			// The test is too large for the state graph backend
			if ((SafetyAnalysisBackend)Arguments[0] == SafetyAnalysisBackend.FaultOptimizedStateGraph)
				return;

			var c = new C();
			var result = Dcca(c.D.Any(d => d.X > 4), c);

			result.CheckedSets.Count.ShouldBe(FaultCount + 1);
			result.MinimalCriticalSets.Count.ShouldBe(FaultCount);
			result.Exceptions.ShouldBeEmpty();
			result.IsComplete.ShouldBe(true);
			result.SuppressedFaults.ShouldBeEmpty();
			result.ForcedFaults.ShouldBeEmpty();

			ShouldContain(result.CheckedSets);

			foreach (var d in c.D)
			{
				ShouldContain(result.CheckedSets, d.F);
				ShouldContain(result.MinimalCriticalSets, d.F);
			}

			result.CounterExamples.Count.ShouldBe(FaultCount);
			foreach (var set in result.MinimalCriticalSets)
				result.CounterExamples.ContainsKey(set).ShouldBe(true);

			foreach (var set in result.MinimalCriticalSets)
			{
				SimulateCounterExample(result.CounterExamples[set], simulator =>
				{
					c = (C)simulator.Model.Roots[0];

					foreach (var d in c.D)
						d.X.ShouldBe((byte)0);

					while (!simulator.IsCompleted)
						simulator.SimulateStep();

					foreach (var d in c.D)
						d.X.ShouldBe(d.F.IsActivated ? (byte)17 : (byte)0);
				});
			}
		}

		private class C : Component
		{
			public readonly D[] D = new D[FaultCount];

			public C()
			{
				for (var i = 0; i < D.Length; ++i)
					D[i] = new D();
			}

			public override void Update()
			{
				Update(D);
			}
		}

		private class D : Component
		{
			public readonly Fault F = new TransientFault();
			public byte X;

			[FaultEffect(Fault = nameof(F))]
			private class E : D
			{
				public override void Update()
				{
					X = 17;
				}
			}
		}
	}
}