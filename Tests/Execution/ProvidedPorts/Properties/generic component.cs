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

namespace Tests.Execution.ProvidedPorts.Properties
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X6<T> : Component
	{
		public T _p3get;
		public T _p3set;

		public X6(T p1)
		{
			P1 = p1;
		}

		public virtual T P1 { get; }

		public virtual T P2 { get; set; }

		public virtual T P3
		{
			get { return _p3get; }
			set { _p3set = value; }
		}
	}

	internal class X7 : TestObject
	{
		protected override void Check()
		{
			var c = new C(7);
			c.P1.ShouldBe(7);

			c.P2 = 99;
			c.P2.ShouldBe(99);

			c._p3get = 17;
			c.P3.ShouldBe(17);

			c.P3 = 19;
			c._p3set.ShouldBe(19);
		}

		private class C : X6<int>
		{
			public C(int p1)
				: base(p1)
			{
			}
		}
	}

	internal class X8 : TestObject
	{
		protected override void Check()
		{
			var c = new C(7);
			c.P1.ShouldBe(7);

			c.P2 = 99;
			c.P2.ShouldBe(99);

			c._p3get = 17;
			c.P3.ShouldBe(17);

			c.P3 = 19;
			c._p3set.ShouldBe(19);
		}

		private class C : X6<long>
		{
			public C(long p1)
				: base(p1)
			{
			}
		}
	}
}