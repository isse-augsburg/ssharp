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

namespace Tests.Execution.Scheduling
{
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	public class BlockKindOrder : TestObject
	{
		protected override void Check()
		{
			var m = new S();
			var r = m.ToRuntimeModel();

			r.RootComponents.Length.ShouldBe(4);
			r.RootComponents[0].ShouldBeOfType<D>();
			r.RootComponents[1].ShouldBeOfType<F>();
			r.RootComponents[2].ShouldBeOfType<C>();
			r.RootComponents[3].ShouldBeOfType<E>();
		}

		private class S : ModelBase
		{
			[Root(Role.SystemOfInterest)]
			public C C = new C();

			// Should be ignored
			public C C1 = new C();

			[Root(Role.SystemContext)]
			public D D = new D();

			[Root(Role.SystemOfInterest)]
			public E E = new E();

			[Root(Role.SystemContext)]
			public F F = new F();
		}

		private class C : Component
		{
		}

		private class D : Component
		{
		}

		private class E : Component
		{
		}

		private class F : Component
		{
		}
	}
}