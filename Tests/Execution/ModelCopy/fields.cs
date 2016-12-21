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
	using System.Collections.Generic;
	using System.Linq;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class Fields : TestObject
	{
		protected override void Check()
		{
			var m1 = new M();
			var s = new SafetySharpSimulator(m1);
			var m2 = (M)s.Model;

			var c1 = m1.GetRoots().OfType<C>().ToArray();
			var c2 = m2.GetRoots().OfType<C>().ToArray();

			for (var i = 0; i < c1.Length; ++i)
				c2[i].I.ShouldBe(c1[i].I);

			m2.E.C.I.ShouldBe(m1.E.C.I);

			m2.Roots.ShouldBe(m2.GetRoots(), ignoreOrder: true);
			m2.Components.ShouldBe(m2.GetRoots().Concat(new [] { m2.E.C }), ignoreOrder: true);
			m2.Faults.ShouldBe(new[] { m2.E.F });
		}

		private class M : ModelBase
		{
			[Root(RootKind.Plant)]
			private readonly C _c = new C();

			[Root(RootKind.Plant)]
			protected readonly C _d = new C();

			[Root(RootKind.Plant)]
			internal readonly D E = new D();

			[Root(RootKind.Plant)]
			public readonly C F = new C();

			[Root(RootKind.Plant)]
			public readonly C[] G = { new C(), new C() };

			public IEnumerable<IComponent> GetRoots()
			{
				yield return _c;
				yield return _d;
				yield return E;
				yield return F;
				yield return G[0];
				yield return G[1];
			}
		}

		private class C : Component
		{
			private static int _count;
			public readonly int I = _count++;
		}

		private class D : Component
		{
			public readonly C C = new C();
			public readonly Fault F = new TransientFault();

			[FaultEffect(Fault = nameof(F))]
			public class E : D
			{
			}
		}
	}
}