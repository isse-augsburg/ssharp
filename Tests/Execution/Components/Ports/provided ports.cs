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

namespace Tests.Reflection.Components.Ports
{
	using System.Linq;
	using System.Reflection;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class ProvidedPorts : TestObject
	{
		protected override void Check()
		{
			new C().GetProvidedPorts().OrderBy(m => m.Name).ShouldBe(new[]
			{
				typeof(C).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
				typeof(C).GetMethod("R", BindingFlags.Instance | BindingFlags.NonPublic),
				typeof(C).GetProperty("X", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(C).GetProperty("Y", BindingFlags.Instance | BindingFlags.NonPublic).SetMethod,
				typeof(C).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(C).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).SetMethod
			}.OrderBy(m => m.Name));

			new F().GetProvidedPorts().OrderBy(m => m.Name).ShouldBe(new[]
			{
				typeof(E).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
				typeof(E).GetProperty("X", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(E).GetProperty("Y", BindingFlags.Instance | BindingFlags.NonPublic).SetMethod,
				typeof(E).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(E).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).SetMethod,
				typeof(F).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
				typeof(F).GetProperty("X", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(F).GetProperty("Y", BindingFlags.Instance | BindingFlags.NonPublic).SetMethod,
				typeof(F).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(F).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).SetMethod
			}.OrderBy(m => m.Name));

			new D<int>().GetProvidedPorts().ShouldBe(new[]
			{
				typeof(D<int>).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
			});

			new D<C>().GetProvidedPorts().ShouldBe(new[]
			{
				typeof(D<C>).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
			});

			new C2().GetProvidedPorts().ShouldBeEmpty();
			new D2<int>().GetProvidedPorts().ShouldBeEmpty();
			new D2<C2>().GetProvidedPorts().ShouldBeEmpty();
		}

		private class C : Component
		{
			public int X { get; }

			private int Y
			{
				set { }
			}

			public int Z { get; set; }

			public virtual int M(int i)
			{
				return i;
			}

			private void R()
			{
			}
		}

		private abstract class E : Component
		{
			public abstract int X { get; }
			protected abstract int Y { set; }
			public abstract int Z { get; set; }
			public abstract int M(int i);
		}

		private class F : E
		{
			public override int X { get; }

			protected override int Y
			{
				set { throw new System.NotImplementedException(); }
			}

			public override int Z { get; set; }
			public override int M(int i)
			{
				throw new System.NotImplementedException();
			}
		}

		private class D<T> : Component
		{
			public T M(T t)
			{
				return t;
			}
		}

		private class C2 : Component
		{
			public extern int X { get; }
			private extern int Y { set; }
			public extern int Z { get; set; }
			public virtual extern int M(int i);
			private extern void R();
		}

		private class D2<T> : Component
		{
			public extern T M(T t);
		}
	}
}