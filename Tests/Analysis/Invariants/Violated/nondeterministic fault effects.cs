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

namespace Tests.Analysis.Invariants.Violated
{
	using SafetySharp.Modeling;
	using Shouldly;

	internal class NonDeterministicFaultEffects : AnalysisTestObject
	{
		protected override void Check()
		{
			var c1 = new C1();

			CheckInvariant(c1.X != 3, c1).ShouldBe(false);
			CheckInvariant(c1.X != 7, c1).ShouldBe(false); 

			var c2 = new C2.C2b();

			CheckInvariant(c2.X != 3, c2).ShouldBe(false);
			CheckInvariant(c2.X != 7, c2).ShouldBe(false);

			var c3 = new C3.C3b();

			CheckInvariant(c3.X != 3, c3).ShouldBe(false);
			CheckInvariant(c3.X != 7, c3).ShouldBe(false);
		}

		private class C1 : Component
		{
			private readonly Fault _f = new TransientFault();

			public int X;

			[FaultEffect(Fault = nameof(_f))]
			public class E1 : C1
			{
				public override void Update()
				{
					X = 3;
				}
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E2 : C1
			{
				public override void Update()
				{
					X = 7;
				}
			}
		}

		private class C2 : Component
		{
			private readonly Fault _f = new TransientFault();

			public int X;

			public virtual void M()
			{
			}

			public class C2b : C2
			{
				public override void Update()
				{
					M();
				}
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E1 : C2b
			{
				public override void M()
				{
					X = 3;
				}
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E2 : C2b
			{
				public override void M()
				{
					X = 7;
				}
			}
		}

		private class C3 : Component
		{
			private readonly Fault _f = new TransientFault();

			public int X;

			public virtual int M => 0;

			public class C3b : C3
			{
				public override void Update()
				{
					var x = M;
				}
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E1 : C3b
			{
				public override int M => X = 3;
			}

			[FaultEffect(Fault = nameof(_f))]
			public class E2 : C3b
			{
				public override int M => X = 7;
			}
		}
	}
}