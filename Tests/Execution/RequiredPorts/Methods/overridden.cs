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

namespace Tests.Execution.RequiredPorts.Methods
{
	using Shouldly;
	using Utilities;

	internal abstract class X2 : TestComponent
	{
		protected X2()
		{
			Bind(nameof(A), nameof(M));
		}

		protected virtual int M(int i)
		{
			return i * 2;
		}

		protected abstract bool Q(int i);

		protected virtual int N(int i)
		{
			return i / 2;
		}

		protected extern int A(int i);

		protected virtual extern int R(int i);
	}

	internal class X3 : X2
	{
		public X3()
		{
			Bind(nameof(B), nameof(base.M));
			Bind(nameof(C), nameof(Q));
			Bind(nameof(D), nameof(N));
			Bind(nameof(E), nameof(base.N));
			Bind(nameof(base.R), nameof(M));
			Bind(nameof(R), nameof(N));
		}

		protected override int M(int i)
		{
			return i * i;
		}

		protected override bool Q(int i)
		{
			return i % 2 == 0;
		}

		protected override int N(int i)
		{
			return base.N(i) + 2;
		}

		protected override extern int R(int i);

		private extern int B(int i);
		private extern bool C(int i);
		private extern int D(int i);
		private extern int E(int i);

		protected override void Check()
		{
			A(3).ShouldBe(9);
			A(10).ShouldBe(100);

			B(3).ShouldBe(6);
			B(10).ShouldBe(20);

			C(2).ShouldBe(true);
			C(3).ShouldBe(false);

			D(10).ShouldBe(7);
			D(100).ShouldBe(52);

			E(10).ShouldBe(5);
			E(100).ShouldBe(50);

			base.R(4).ShouldBe(16);
			base.R(20).ShouldBe(400);

			R(8).ShouldBe(6);
			R(20).ShouldBe(12);
		}
	}
}