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

	internal class GenericModel : TestObject
	{
		protected override void Check()
		{
			var m1 = new M<C>();
			var s = new SafetySharpSimulator(m1);
			var m2 = (M<C>)s.Model;

			m2.A.I.ShouldBe(m1.A.I);
			((C)m2.B).I.ShouldBe(((C)m1.B).I);
			((C)m2.C).I.ShouldBe(((C)m1.C).I);
			m2.D().I.ShouldBe(m1.D().I);

			m2.Roots.ShouldBe(new[] { m2.A, m2.B, m2.C, m2.D() }, ignoreOrder: true);
			m2.Components.ShouldBe(new[] { m2.A, m2.B, m2.C, m2.D() }, ignoreOrder: true);
			m2.Faults.ShouldBeEmpty();
		}

		private class M<T> : ModelBase
			where T : Component, new()
		{
			[Root(RootKind.Plant)]
			public readonly T A = new T();

			public readonly T E = new T();
			private readonly T _c2 = new T();

			[Root(RootKind.Plant)]
			public Component B { get; } = new T();

			[Root(RootKind.Plant)]
			public IComponent C { get; } = new T();

			public T F => new T();

			public T G { get; } = new T();

			[Root(RootKind.Plant)]
			public T D() => _c2;

			public T H() => new T();
		}

		private class C : Component
		{
			private static int _count;
			public readonly int I = _count++;
		}
	}
}