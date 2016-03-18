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

namespace Tests.Execution.ModelCopy
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class Mixed : TestObject
	{
		protected override void Check()
		{
			var m1 = new M();
			var s = new Simulator(m1, m1.Formula);
			var m2 = (M)s.Model;

			m1.A.I.ShouldBe(m2.A.I);
			m1.B.I.ShouldBe(m2.B.I);
			m1.C.I.ShouldBe(m2.C.I);
			m1.D().I.ShouldBe(m2.D().I);

			m1.E.I.ShouldBe(m2.E.I);
			m1.F.I.ShouldBe(m2.F.I);
			m1.G.I.ShouldBe(m2.G.I);
			m1.H().I.ShouldBe(m2.H().I);

			m2.Formula.Evaluate().ShouldBe(false);

			m2.Roots.ShouldBe(new IComponent[] { m2.A, m2.B, m2.C, m2.D() }, ignoreOrder: true);
			m2.Components.ShouldBe(new IComponent[] { m2.A, m2.B, m2.C, m2.D() }, ignoreOrder: true);
			m2.Faults.ShouldBeEmpty();
		}

		private class M : ModelBase
		{
			[Root(Role.SystemContext)]
			public readonly C A = new C();

			public readonly C E = new C();

			public M()
			{
				F = A;
			}

			[Root(Role.SystemContext)]
			public C B { get; } = new C();

			[Root(Role.SystemContext)]
			public C C { get; } = new C();

			public C F { get; }

			public C G { get; } = new C();

			public Formula Formula => D().I == E.I;

			[Root(Role.SystemContext)]
			public C D() => G;

			public C H() => G;
		}

		private class C : Component
		{
			private static int _count;
			public readonly int I = _count++;
		}
	}
}