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

	internal class X3 : AnalysisTestObject
	{
		protected override void Check()
		{
			var components = Enumerable.Range(0, 5).Select(i => new C()).ToArray();
			var model = TestModel.InitializeModel(components);

			IFaultSetHeuristic heuristic = new MinimalRedundancyHeuristic(model,
				components.Select(c => c.F1),
				components.Select(c => c.F2),
				components.Select(c => c.F3)
			);

			var setsToCheck = new List<FaultSet>();
			heuristic.Augment(setsToCheck);

			setsToCheck.Count.ShouldBe(125);
			setsToCheck.All(set => set.Cardinality == 12).ShouldBe(true);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new PermanentFault();
			public readonly Fault F3 = new PermanentFault();
			public int X;

			public override void Update()
			{
				X = Math.Min(X + 1, 5);
			}

			[FaultEffect(Fault = nameof(F1))]
			private class E1 : C
			{
				public override void Update()
				{
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
