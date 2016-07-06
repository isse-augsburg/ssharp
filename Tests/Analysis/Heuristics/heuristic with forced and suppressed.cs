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
			result.Exceptions.ShouldBeEmpty();
			result.IsComplete.ShouldBe(true);
			result.ForcedFaults.ShouldBe(new[] { c.F1 });
			result.SuppressedFaults.ShouldBeEmpty();
			result.MinimalCriticalSets.ShouldBeEmpty();
			result.CheckedSets.ShouldBe(new[] {
				new HashSet<Fault>(),
				new HashSet<Fault>(new[] { c.F2 })
			});

			c.F1.ForceActivation();

			result = Dcca(m, c.Defect);
			result.Exceptions.ShouldBeEmpty();
			result.IsComplete.ShouldBe(true);
			result.SuppressedFaults.ShouldBe(new[] { c.F1 });
			result.ForcedFaults.ShouldBeEmpty();
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
