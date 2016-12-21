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

	internal class InheritedModel : TestObject
	{
		protected override void Check()
		{
			var m1 = new M2();
			var s = new SafetySharpSimulator(m1);
			var m2 = (M2)s.Model;

			m2.A.I.ShouldBe(m1.A.I);
			m2.B.I.ShouldBe(m1.B.I);
			m2.C.I.ShouldBe(m1.C.I);
			m2.D().I.ShouldBe(m1.D().I);

			m2.Roots.ShouldBe(new[] { m2.A, m2.B, m2.C, m2.D() }, ignoreOrder: true);
			m2.Components.ShouldBe(new[] { m2.A, m2.B, m2.C, m2.D() }, ignoreOrder: true);
			m2.Faults.ShouldBeEmpty();
		}

		private class M : ModelBase
		{
			private readonly C _c2 = new C();

			public readonly C E = new C();

			[Root(RootKind.Plant)]
			public C C { get; } = new C();

			public C F => new C();

			[Root(RootKind.Plant)]
			public C D() => _c2;
		}

		private class M2 : M
		{
			[Root(RootKind.Plant)]
			public readonly C A = new C();

			[Root(RootKind.Plant)]
			public C B { get; } = new C();

			public C G { get; } = new C();

			public C H() => new C();
		}

		private class C : Component
		{
			private static int _count;
			public readonly int I = _count++;
		}
	}
}