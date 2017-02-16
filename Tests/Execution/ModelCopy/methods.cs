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
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class Methods : TestObject
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

			m2.E().C.I.ShouldBe(m1.E().C.I);

			m2.Roots.ShouldBe(m2.GetRoots(), ignoreOrder: true);
			m2.Components.ShouldBe(m2.GetRoots().Concat(new[] { m2.E().C }), ignoreOrder: true);
			m2.Faults.ShouldBe(new[] { m2.E().F });
		}

		private class M : ModelBase
		{
			private readonly C[] _c = { new C(), new C(), new C() };
			private readonly D _d = new D();

			private readonly List<C> _l = new List<C>
			{
				new C(),
				new C()
			};

			[Root(RootKind.Plant)]
			private C C() => _c[0];

			[Root(RootKind.Plant)]
			protected C D() => _c[1];

			[Root(RootKind.Plant)]
			internal D E() => _d;

			[Root(RootKind.Plant)]
			public C F() => _c[2];

			[Root(RootKind.Plant)]
			public List<C> G() => _l;

			public IEnumerable<IComponent> GetRoots()
			{
				yield return C();
				yield return D();
				yield return E();
				yield return F();

				foreach (var c in G())
					yield return c;
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