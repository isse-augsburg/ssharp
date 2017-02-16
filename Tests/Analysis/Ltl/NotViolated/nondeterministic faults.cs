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

namespace Tests.Analysis.Ltl.NotViolated
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using Shouldly;
	using static SafetySharp.Analysis.Operators;

	internal class NonDeterministicFaults : AnalysisTestObject
	{
		protected override void Check()
		{
			var c = new C { X = 3 };

			Check(G(!c.F1.IsActivated && !c.F2.IsActivated && !c.F3.IsActivated).Implies(G(c.X == 3)), c).ShouldBe(true);

			Check(G(!c.F2.IsActivated && !c.F3.IsActivated).Implies(G(!c.F1.IsActivated || c.X == 13)), c).ShouldBe(true);
			Check(G(!c.F1.IsActivated && !c.F3.IsActivated).Implies(G(!c.F2.IsActivated || c.X == 103)), c).ShouldBe(true);
			Check(G(!c.F1.IsActivated && !c.F2.IsActivated).Implies(G(!c.F3.IsActivated || c.X == 1003)), c).ShouldBe(true);

			Check(G(!c.F3.IsActivated).Implies(G(!c.F1.IsActivated || !c.F2.IsActivated || c.X == 103 || c.X == 13)), c).ShouldBe(true);
			Check(G(!c.F1.IsActivated).Implies(G(!c.F3.IsActivated || !c.F2.IsActivated || c.X == 1003 || c.X == 103)), c).ShouldBe(true);
			Check(G(!c.F2.IsActivated).Implies(G(!c.F1.IsActivated || !c.F3.IsActivated || c.X == 1003 || c.X == 13)), c).ShouldBe(true);

			Check(G(!c.F1.IsActivated || !c.F2.IsActivated || !c.F3.IsActivated || c.X == 1003 || c.X == 103 || c.X == 13), c).ShouldBe(true);
		}

		private class C : Component
		{
			public readonly Fault F1 = new TransientFault();
			public readonly Fault F2 = new TransientFault();
			public readonly Fault F3 = new TransientFault();
			public readonly Fault F4 = new TransientFault();
			public readonly Fault F5 = new TransientFault();
			public int X;

			public override void Update()
			{
				X = 3;
			}

			[FaultEffect(Fault = nameof(F1))]
			[Priority(3)]
			internal class E1 : C
			{
				public override void Update()
				{
					base.Update();
					X += 10;
				}
			}

			[FaultEffect(Fault = nameof(F2))]
			[Priority(3)]
			internal class E2 : C
			{
				public override void Update()
				{
					base.Update();
					X += 100;
				}
			}

			[FaultEffect(Fault = nameof(F3))]
			[Priority(3)]
			internal class E3 : C
			{
				public override void Update()
				{
					base.Update();
					X += 1000;
				}
			}

			[FaultEffect(Fault = nameof(F4))]
			[Priority(3)]
			internal class E4 : C
			{
				// Should have no effect on the nondeterministic fault effect choice
			}

			[FaultEffect(Fault = nameof(F4))]
			[Priority(3)]
			internal class E0 : C
			{
				// Should have no effect on the nondeterministic fault effect choice
			}

			[FaultEffect(Fault = nameof(F4))]
			[Priority(3)]
			internal class E1a : C
			{
				// Should have no effect on the nondeterministic fault effect choice
			}

			[FaultEffect(Fault = nameof(F4))]
			[Priority(3)]
			internal class E2a : C
			{
				// Should have no effect on the nondeterministic fault effect choice
			}

			[FaultEffect(Fault = nameof(F5))]
			[Priority(1)]
			internal class E5 : C
			{
				// Should have no effect on the nondeterministic fault effect choice

				public override void Update()
				{
					X = 3;
				}
			}
		}
	}
}