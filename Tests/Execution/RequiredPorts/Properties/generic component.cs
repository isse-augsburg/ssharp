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

namespace Tests.Execution.RequiredPorts.Properties
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X6<T> : Component
	{
		protected X6(T t)
		{
			P = t;
			Bind(nameof(R), nameof(P));
		}

		public extern T R { get; }
		public T P { get; }
	}

	internal class X7 : TestObject
	{
		protected override void Check()
		{
			var c1 = new C(17);
			c1.R.ShouldBe(17);

			var c2 = new C(44);
			c2.R.ShouldBe(44);
		}

		private class C : X6<int>
		{
			public C(int t)
				: base(t)
			{
			}
		}
	}

	internal class X8 : TestObject
	{
		protected override void Check()
		{
			var c1 = new C(true);
			c1.R.ShouldBe(true);

			var c2 = new C(false);
			c2.R.ShouldBe(false);
		}

		private class C : X6<bool>
		{
			public C(bool t)
				: base(t)
			{
			}
		}
	}
}