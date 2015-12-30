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

namespace Tests.Execution.Bindings
{
	using System;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal abstract class Y1 : TestComponent
	{
		protected extern int N();
		public extern int N(int i);
	}

	internal class X16 : Y1
	{
		public X16()
		{
			Bind<Func<int>>(nameof(N), nameof(M));
		}

		private int M()
		{
			return 17;
		}

		private int M(int i)
		{
			return i * 2;
		}

		protected override void Check()
		{
			N().ShouldBe(17);
			Should.Throw<UnboundPortException>(() => N(1));
		}
	}

	internal class X17 : Y1
	{
		public X17()
		{
			Bind<Func<int, int>>(nameof(N), nameof(M));
		}

		private int M()
		{
			return 17;
		}

		private int M(int i)
		{
			return i * 2;
		}

		protected override void Check()
		{
			N(1).ShouldBe(2);
			Should.Throw<UnboundPortException>(() => N());
		}
	}

	internal delegate void D1(ref int i);

	internal abstract class Y3 : TestComponent
	{
		protected extern void N();
		public extern void N(ref int i);
	}

	internal class X18 : Y3
	{
		public X18()
		{
			Bind<D1>(nameof(N), nameof(M));
		}

		private void M(ref int i)
		{
			++i;
		}

		private void M()
		{
		}

		protected override void Check()
		{
			var i = 3;
			N(ref i);

			i.ShouldBe(4);
			Should.Throw<UnboundPortException>(() => N());
		}
	}
}