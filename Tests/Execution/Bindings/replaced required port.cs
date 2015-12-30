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
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal abstract class X33 : TestComponent
	{
		public extern int M1();
		public extern int M2();
	}

	internal class X34 : X33
	{
		int x;
		public X34()
		{
			Bind(nameof(M1), nameof(N));
			Bind(nameof(base.M2), nameof(N));
		}

		private int N()
		{
			return ++x;
		}

		public new extern int M1();
		public new extern int M2();

		protected override void Check()
		{
			M1().ShouldBe(1);
			base.M2().ShouldBe(2);

			Should.Throw<UnboundPortException>(() => base.M1());
			Should.Throw<UnboundPortException>(() => M2());
		}
	}
}