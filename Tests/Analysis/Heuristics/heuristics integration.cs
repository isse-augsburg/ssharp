namespace Tests.Analysis.Heuristics
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class X6 : AnalysisTestObject
	{
		const int componentCount = 4;

		protected override void Check()
		{
			var components = Enumerable.Range(0, componentCount).Select(i => new C()).ToArray();
			var model = TestModel.InitializeModel(components);

			// heuristics are called appropriately
			var counter = new CountingHeuristic();
			Heuristics = new[] { counter };

			var result = Dcca(model, false);
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
			((ulong)result.CheckedSets.Count).ShouldBeLessThan(counter.setCounter);

			// subsumption heuristic is effective
			Heuristics = new[] { new SubsumptionHeuristic(model) };

			foreach (var c in components)
				c.F1.Subsumes(c.F2);

			result = Dcca(model, false);
			((ulong)result.CheckedSets.Count).ShouldBeLessThan(counter.setCounter); // heuristic has effect

			// heuristics are called appropriately when combined
			counter = new CountingHeuristic();
			Heuristics = new IFaultSetHeuristic[] { counter, new SubsumptionHeuristic(model) };

			result = Dcca(model, false);
			counter.cardinalityCounter.ShouldBe(model.Faults.Length + 1);
			result.CheckedSets.Count.ShouldBeLessThan(1 << model.Faults.Length);
			counter.setCounter.ShouldBeGreaterThanOrEqualTo((ulong)result.CheckedSets.Count);
		}

		private class CountingHeuristic : IFaultSetHeuristic
		{
			public int cardinalityCounter = 0;

			public void Augment(List<FaultSet> setsToCheck)
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
