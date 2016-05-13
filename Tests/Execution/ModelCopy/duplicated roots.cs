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

	internal class DuplicatedRoots : TestObject
	{
		protected override void Check()
		{
			var m1 = new M();
			var s = new Simulator(m1);
			var m2 = (M)s.Model;

			m2.A.I.ShouldBe(m1.A.I);
			m2.B.I.ShouldBe(m1.B.I);

			m2.Roots.ShouldBe(new IComponent[] { m2.A }, ignoreOrder: true);
			m2.Components.ShouldBe(new IComponent[] { m2.A }, ignoreOrder: true);
			m2.Faults.ShouldBeEmpty();
		}

		private class M : ModelBase
		{
			[Root(RootKind.Plant)]
			public readonly C A;

			[Root(RootKind.Plant)]
			public readonly C B;

			public M()
			{
				A = new C();
				B = A;
			}
		}

		private class C : Component
		{
			private static int _count;
			public readonly int I = _count++;
		}
	}
}