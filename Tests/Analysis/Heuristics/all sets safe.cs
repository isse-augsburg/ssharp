namespace Tests.Analysis.Heuristics
{
	using System;
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X4 : AnalysisTestObject
	{
		protected override void Check()
		{
			var components = new[] { new C(), new C() };
			var model = TestModel.InitializeModel(components);

			Heuristics = new[] { new SubsumptionHeuristic(model) };

			var result = Dcca(model, false);
			result.Exceptions.ShouldBeEmpty();
			result.MinimalCriticalSets.ShouldBeEmpty();
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new PermanentFault();
			public readonly Fault F3 = new PermanentFault();
			public int X;

			public C()
			{
				F1.Subsumes(F2);
				F3.Subsumes(F2);
			}

			public override void Update()
			{
				X = Math.Min(X + 1, 5);
			}

			[FaultEffect(Fault = nameof(F1))]
			[Priority(1)]
			private class E1 : C
			{
				public override void Update()
				{
				}
			}

			[FaultEffect(Fault = nameof(F2))]
			[Priority(2)]
			private class E2 : C
			{
				public override void Update()
				{
				}
			}

			[FaultEffect(Fault = nameof(F3))]
			[Priority(3)]
			private class E3 : C
			{
				public override void Update()
				{
				}
			}
		}
	}
}
