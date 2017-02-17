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

namespace Tests.Execution.Components.Ports
{
	using System.Linq;
	using System.Reflection;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class RequiredPorts : TestObject
	{
		protected override void Check()
		{
			new C().GetRequiredPorts().OrderBy(m => m.Name).ShouldBe(new[]
			{
				typeof(C).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
				typeof(C).GetMethod("R", BindingFlags.Instance | BindingFlags.NonPublic),
				typeof(C).GetProperty("X", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(C).GetProperty("Y", BindingFlags.Instance | BindingFlags.NonPublic).SetMethod,
				typeof(C).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(C).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).SetMethod
			}.OrderBy(m => m.Name));

			new D().GetRequiredPorts().OrderBy(m => m.Name).ShouldBe(new[]
			{
				typeof(C).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
				typeof(D).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
				typeof(C).GetMethod("R", BindingFlags.Instance | BindingFlags.NonPublic),
				typeof(C).GetProperty("X", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(C).GetProperty("Y", BindingFlags.Instance | BindingFlags.NonPublic).SetMethod,
				typeof(C).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).GetMethod,
				typeof(C).GetProperty("Z", BindingFlags.Instance | BindingFlags.Public).SetMethod
			}.OrderBy(m => m.Name));

			new D<int>().GetRequiredPorts().ShouldBe(new[]
			{
				typeof(D<int>).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
			});

			new D<C>().GetRequiredPorts().ShouldBe(new[]
			{
				typeof(D<C>).GetMethod("M", BindingFlags.Instance | BindingFlags.Public),
			});

			new C2().GetRequiredPorts().ShouldBeEmpty();
			new D2<int>().GetRequiredPorts().ShouldBeEmpty();
			new D2<C2>().GetRequiredPorts().ShouldBeEmpty();
		}

		private class C : Component
		{
			public extern int X { get; }
			private extern int Y { set; }
			public extern int Z { get; set; }
			public virtual extern int M(int i);
			private extern void R();
		}

		private class D : C
		{
			public override extern int M(int i);
		}

		private class D<T> : Component
		{
			public extern T M(T t);
		}

		private class C2 : Component
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

		private class D2<T> : Component
		{
			public T M(T t)
			{
				return t;
			}
		}
	}
}