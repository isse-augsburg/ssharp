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

namespace Tests.Execution.Bindings
{
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class X2 : TestComponent
	{
		public X2()
		{
			Bind(nameof(N), nameof(M));
		}

		private int M()
		{
			return 1;
		}

		private extern int N();
		private extern int N(int i);

		protected override void Check()
		{
			N().ShouldBe(1);
			Should.Throw<UnboundPortException>(() => N(1));
		}
	}

	internal class X3 : TestComponent
	{
		public X3()
		{
			Bind(nameof(N), nameof(M));
		}

		private int M()
		{
			return 17;
		}

		private int M(int i)
		{
			return i;
		}

		private extern int N();

		protected override void Check()
		{
			N().ShouldBe(17);
		}
	}

	internal class X4 : TestComponent
	{
		public X4()
		{
			Bind(nameof(N), nameof(M));
		}

		private int M(int i)
		{
			return i + 1;
		}

		private extern int N();
		private extern int N(int i);

		protected override void Check()
		{
			N(17).ShouldBe(18);
			Should.Throw<UnboundPortException>(() => N());
		}
	}

	internal class X5 : TestComponent
	{
		public X5()
		{
			Bind(nameof(N), nameof(M));
		}

		private int M()
		{
			return 3;
		}

		private int M(int i)
		{
			return i + 17;
		}

		private extern int N(int i);

		protected override void Check()
		{
			N(3).ShouldBe(20);
		}
	}

	internal class X6 : TestComponent
	{
		public X6()
		{
			Bind(nameof(N), nameof(M));
		}

		private int M()
		{
			return 3;
		}

		private int M(bool b)
		{
			return 0;
		}

		private extern int N();
		private extern int N(int i);

		protected override void Check()
		{
			N().ShouldBe(3);
			Should.Throw<UnboundPortException>(() => N(3));
		}
	}

	internal class X7 : TestComponent
	{
		public X7()
		{
			Bind(nameof(N), nameof(M));
		}

		private int M(int i)
		{
			return 32;
		}

		private int M(bool i)
		{
			return i ? 3 : 2;
		}

		private extern int N();
		private extern int N(bool b);

		protected override void Check()
		{
			N(true).ShouldBe(3);
			N(false).ShouldBe(2);
			Should.Throw<UnboundPortException>(() => N());
		}
	}

	internal class X8 : TestComponent
	{
		public X8()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M()
		{
		}

		private void M(ref bool b)
		{
			b = !b;
		}

		private extern void N(ref bool b);
		private extern void N(int i);

		protected override void Check()
		{
			var b = false;
			N(ref b);
			b.ShouldBe(true);

			Should.Throw<UnboundPortException>(() => N(1));
		}
	}

	internal class X9 : TestComponent
	{
		public X9()
		{
			Bind(nameof(N), nameof(M));
		}

		private void M(ref int i)
		{
			++i;
		}

		private void M(int i)
		{
		}

		private extern void N();
		private extern void N(ref int i);

		protected override void Check()
		{
			var i = 3;
			N(ref i);
			i.ShouldBe(4);
			Should.Throw<UnboundPortException>(() => N());
		}
	}
}