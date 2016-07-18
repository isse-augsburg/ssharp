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
	using System;
	using System.Collections.Generic;
	using SafetySharp.Analysis;
	using SafetySharp.Analysis.Heuristics;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X2 : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C();
			var model = TestModel.InitializeModel(c);

			// no subsumption declared
			IFaultSetHeuristic heuristic = new SubsumptionHeuristic(model);
			var setsToCheck = new LinkedList<FaultSet>(new[] { new FaultSet(c.F4), new FaultSet(c.F5) });
			heuristic.Augment(0, setsToCheck);
			setsToCheck.Count.ShouldBe(2);

			// simple subsumption
			heuristic = new SubsumptionHeuristic(model);
			setsToCheck = new LinkedList<FaultSet>(new[] { new FaultSet(c.F3), new FaultSet(c.F5) });
			heuristic.Augment(0, setsToCheck);
			setsToCheck.ShouldBe(new[] { new FaultSet(c.F3, c.F4), new FaultSet(c.F3), new FaultSet(c.F5) });

			// transitive subsumption
			heuristic = new SubsumptionHeuristic(model);
			setsToCheck = new LinkedList<FaultSet>(new[] { new FaultSet(c.F2), new FaultSet(c.F3) });
			heuristic.Augment(0, setsToCheck);
			setsToCheck.ShouldBe(new[] { new FaultSet(c.F2, c.F3, c.F4), new FaultSet(c.F2),
				new FaultSet(c.F3, c.F4), new FaultSet(c.F3) });

			// reflexive subsumption (nonsensical, but should be handled without endless loop)
			heuristic = new SubsumptionHeuristic(model);
			setsToCheck = new LinkedList<FaultSet>(new[] { new FaultSet(c.F1) });
			heuristic.Augment(0, setsToCheck);
			setsToCheck.ShouldBe(new[] { new FaultSet(c.F1) });

			// circular subsumption
			heuristic = new SubsumptionHeuristic(model);
			setsToCheck = new LinkedList<FaultSet>(new[] { new FaultSet(c.F6) });
			heuristic.Augment(0, setsToCheck);
			setsToCheck.ShouldBe(new[] { new FaultSet(c.F6, c.F7, c.F8), new FaultSet(c.F6) });

			// empty set subsumes nothing
			heuristic = new SubsumptionHeuristic(model);
			setsToCheck = new LinkedList<FaultSet>(new[] { new FaultSet() });
			heuristic.Augment(0, setsToCheck);
			setsToCheck.Count.ShouldBe(1);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new PermanentFault();
			public readonly Fault F3 = new PermanentFault();
			public readonly Fault F4 = new PermanentFault();
			public readonly Fault F5 = new PermanentFault();
			public readonly Fault F6 = new PermanentFault();
			public readonly Fault F7 = new PermanentFault();
			public readonly Fault F8 = new PermanentFault();
			public int X;

			public C()
			{
				F1.Subsumes(F1);

				F2.Subsumes(F3);
				F3.Subsumes(F4);

				F6.Subsumes(F7);
				F7.Subsumes(F8);
				F8.Subsumes(F6);
			}

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

			[FaultEffect(Fault = nameof(F4))]
			private class E4 : C
			{
				public override void Update()
				{
				}
			}

			[FaultEffect(Fault = nameof(F5))]
			private class E5 : C
			{
				public override void Update()
				{
				}
			}

			[FaultEffect(Fault = nameof(F6))]
			private class E6 : C
			{
				public override void Update()
				{
				}
			}

			[FaultEffect(Fault = nameof(F7))]
			private class E7 : C
			{
				public override void Update()
				{
				}
			}

			[FaultEffect(Fault = nameof(F8))]
			private class E8 : C
			{
				public override void Update()
				{
				}
			}
		}
	}
}
