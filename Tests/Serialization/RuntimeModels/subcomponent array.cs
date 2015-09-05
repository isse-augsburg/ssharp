// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace Tests.Serialization.RuntimeModels
{
	using System;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Reflection;
	using Shouldly;

	internal class SubcomponentArray : RuntimeModelTest
	{
		private static bool _hasConstructorRun;

		protected override void Check()
		{
			var c1 = new C1<int> { F = 33 };
			var c2 = new C2 { L = Int64.MaxValue };
			var c3 = new C1<bool> { F = true };
			var d = new D { C = new IComponent[] { c1, c2, c3 } };
			var m = new Model(d);

			_hasConstructorRun = false;
			Create(m);

			StateFormulas.ShouldBeEmpty();
			RootComponents.Length.ShouldBe(1);

			var root = RootComponents[0];
			root.ShouldBeOfType<D>();

			((D)root).C[0].ShouldBeOfType<C1<int>>();
			((D)root).C[1].ShouldBeOfType<C2>();
			((D)root).C[2].ShouldBeOfType<C1<bool>>();

			((C1<int>)((D)root).C[0]).F.ShouldBe(33);
			((C2)((D)root).C[1]).L.ShouldBe(Int64.MaxValue);
			((C1<bool>)((D)root).C[2]).F.ShouldBe(true);

			root.GetSubcomponents().ShouldBe(new[] { ((D)root).C[0], ((D)root).C[1], ((D)root).C[2] });
			((Component)((D)root).C[0]).GetSubcomponents().ShouldBeEmpty();
			((Component)((D)root).C[1]).GetSubcomponents().ShouldBeEmpty();
			((Component)((D)root).C[2]).GetSubcomponents().ShouldBeEmpty();

			_hasConstructorRun.ShouldBe(false);
		}

		private class C1<T> : Component
		{
			public T F;

			public C1()
			{
				_hasConstructorRun = true;
			}
		}

		private class C2 : Component
		{
			public long L;

			public C2()
			{
				_hasConstructorRun = true;
			}
		}

		private class D : Component
		{
			public IComponent[] C;

			public D()
			{
				_hasConstructorRun = true;
			}
		}
	}
}