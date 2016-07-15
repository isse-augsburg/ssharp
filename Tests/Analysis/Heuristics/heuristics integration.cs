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

namespace Tests.Analysis.Heuristics
{
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X6 : AnalysisTestObject
	{
		const int componentCount = 8;

		protected override void Check()
		{
			var components = Enumerable.Range(0, componentCount).Select(i => new C()).ToArray();
			var model = TestModel.InitializeModel(components);

			// heuristics are called appropriately
			var counter = new CountingHeuristic();
			Heuristics = new[] { counter };

			var result = Dcca(model, false);
			result.IsComplete.ShouldBe(true);
			result.Exceptions.ShouldBeEmpty();
			result.MinimalCriticalSets.ShouldBeEmpty();
			result.CheckedSets.Count.ShouldBe(1 << model.Faults.Length);
			counter.setCounter.ShouldBe((ulong)result.CheckedSets.Count);
			counter.cardinalityCounter.ShouldBe(model.Faults.Length + 1);

			// redundancy heuristic works properly
			Heuristics = new[] { new MinimalRedundancyHeuristic(
				model,
					components.Select(c => c.F1),
					components.Select(c => c.F2)
				)
			};

			result = Dcca(model, false);
			result.IsComplete.ShouldBe(true);
			result.Exceptions.ShouldBeEmpty();
			result.MinimalCriticalSets.ShouldBeEmpty();
			result.CheckedSets.Count.ShouldBe(575);
			((ulong)result.CheckedSets.Count).ShouldBeLessThan(counter.setCounter);

			// subsumption heuristic is effective
			Heuristics = new[] { new SubsumptionHeuristic(model) };

			foreach (var c in components)
				c.F1.Subsumes(c.F2);

			result = Dcca(model, false);
			result.IsComplete.ShouldBe(true);
			result.Exceptions.ShouldBeEmpty();
			result.MinimalCriticalSets.ShouldBeEmpty();
			result.CheckedSets.Count.ShouldBe(985);
			((ulong)result.CheckedSets.Count).ShouldBeLessThan(counter.setCounter); // heuristic has effect

			// heuristics are called appropriately when combined
			counter = new CountingHeuristic();
			Heuristics = new IFaultSetHeuristic[] { counter, new SubsumptionHeuristic(model) };

			result = Dcca(model, false);
			result.IsComplete.ShouldBe(true);
			result.Exceptions.ShouldBeEmpty();
			result.MinimalCriticalSets.ShouldBeEmpty();
			counter.cardinalityCounter.ShouldBe(model.Faults.Length + 1);
			result.CheckedSets.Count.ShouldBeLessThan(1 << model.Faults.Length);
			counter.setCounter.ShouldBeGreaterThanOrEqualTo((ulong)result.CheckedSets.Count);
		}

		private class CountingHeuristic : IFaultSetHeuristic
		{
			public int cardinalityCounter = 0;

			public void Augment(uint cardinalityLevel, List<FaultSet> setsToCheck)
			{
				cardinalityCounter++;
			}

			public ulong setCounter = 0;

			public void Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
			{
				setCounter++;
			}
		}

		private class C : Component
		{
			public readonly Fault F1 = new PermanentFault();
			public readonly Fault F2 = new PermanentFault();

			[FaultEffect(Fault = nameof(F1))]
			private class E1 : C
			{ }

			[FaultEffect(Fault = nameof(F2))]
			private class E2 : C
			{ }
		}
	}
}
