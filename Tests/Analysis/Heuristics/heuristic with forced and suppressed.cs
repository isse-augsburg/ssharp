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

	internal class X5 : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			var m = TestModel.InitializeModel(c);

			Heuristics = new[] { new Heuristic() { C = c } };

			c.F1.SuppressActivation();

			var result = Dcca(m, c.Defect);
			result.MinimalCriticalSets.ShouldBeEmpty();
			result.CheckedSets.Count.ShouldBe(2);

			c.F1.ForceActivation();

			result = Dcca(m, c.Defect);
			result.MinimalCriticalSets.Single().ShouldBe(new HashSet<Fault>(new[] { c.F1 }));
			result.CheckedSets.Count.ShouldBe(1);
		}

		private class Heuristic : IFaultSetHeuristic
		{
			public C C { get; set; }

			public void Augment(List<FaultSet> setsToCheck)
			{
				setsToCheck.Add(new FaultSet());
				setsToCheck.Add(new FaultSet(C.F1, C.F2));
				setsToCheck.Add(new FaultSet(C.F1));
			}

			public void Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
			{
			}
		}

		private class C : Component
		{
			public virtual bool Defect => false;

			public readonly Fault F1 = new PermanentFault();
			public readonly Fault F2 = new PermanentFault();

			[FaultEffect(Fault = nameof(F1))]
			private class E1 : C
			{
				public override bool Defect => true;
			}

			[FaultEffect(Fault = nameof(F2))]
			private class E2 : C
			{
			}
		}
	}
}
