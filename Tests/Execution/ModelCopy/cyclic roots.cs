// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

	internal class CyclicRoots : TestObject
	{
		protected override void Check()
		{
			var m1 = new M();
			var s = new SafetySharpSimulator(m1);
			var m2 = (M)s.Model;

			m2.F.I.ShouldBe(m1.F.I);
			m2.E.C.I.ShouldBe(m1.E.C.I);

			m2.Roots.ShouldBe(new IComponent[] { m2.F, m2.E }, ignoreOrder: true);
			m2.Components.ShouldBe(new IComponent[] { m2.F, m2.E }, ignoreOrder: true);
			m2.Faults.ShouldBeEmpty();
		}

		private class M : ModelBase
		{
			[Root(RootKind.Plant)]
			internal readonly D E;

			[Root(RootKind.Plant)]
			public readonly C F;

			public M()
			{
				E = new D { C = new C() };
				F = E.C;
			}
		}

		private class C : Component
		{
			private static int _count;
			public readonly int I = _count++;
		}

		private class D : Component
		{
			public C C;
		}
	}
}