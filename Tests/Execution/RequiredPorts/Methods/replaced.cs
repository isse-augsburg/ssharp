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

namespace Tests.Execution.RequiredPorts.Methods
{
	using Shouldly;
	using Utilities;

	internal abstract class X4 : TestComponent
	{
		public int M(int i)
		{
			return i * 2;
		}

		public extern int N(int i);
	}

	internal class X5 : X4
	{
		public X5()
		{
			Bind(nameof(N), nameof(base.M));
			Bind(nameof(base.N), nameof(M));
		}

		private new int M(int i)
		{
			return i * i;
		}

		private new extern int N(int i);

		protected override void Check()
		{
			N(3).ShouldBe(6);
			N(10).ShouldBe(20);

			base.N(3).ShouldBe(9);
			base.N(10).ShouldBe(100);

			((X4)this).N(3).ShouldBe(9);
			((X4)this).N(10).ShouldBe(100);
		}
	}
}