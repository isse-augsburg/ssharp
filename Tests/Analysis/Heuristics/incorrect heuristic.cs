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
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class X7 : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			var m = TestModel.InitializeModel(c);

			Heuristics = new[] { new WrongHeuristic() { C = c } };

			var result = Dcca(m, c.Defect);
			result.CheckedSets.ShouldBe(new[] {
				new HashSet<Fault>(new[] { c.F1 }),
				new HashSet<Fault>(new[] { c.F2 }),
				new HashSet<Fault>() }
			);
			result.MinimalCriticalSets.ShouldBe(new[] {
				new HashSet<Fault>(new[] { c.F1 }),
				new HashSet<Fault>(new[] { c.F2 })
			});
			result.Exceptions.ShouldBeEmpty();
		}

		private class WrongHeuristic : IFaultSetHeuristic
		{
			public C C { get; set; }

			public void Augment(List<FaultSet> setsToCheck)
			{
				setsToCheck.Add(new FaultSet(C.F1));
			}

			private bool updated = false;
			public void Update(List<FaultSet> setsToCheck, FaultSet checkedSet, bool isSafe)
			{
				if (updated)
					return;
				setsToCheck.Add(new FaultSet(C.F2));
				updated = true;
			}
		}

		private class C : Component
		{
			public virtual bool Defect => X > 7;

			public virtual int X => 3;

			public readonly Fault F1 = new PermanentFault();
			public readonly Fault F2 = new PermanentFault();

			[FaultEffect(Fault = nameof(F1))]
			private class E1 : C
			{
				public override int X => 12;
			}

			[FaultEffect(Fault = nameof(F2))]
			private class E2 : C
			{
				public override int X => 30;
			}
		}
	}
}
