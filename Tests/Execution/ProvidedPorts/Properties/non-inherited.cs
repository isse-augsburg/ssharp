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

	internal class X1 : TestComponent
	{
		private int _x;
		private int P1 => _x * 2;
		private int P2 { get; } = 77;

		private int P3
		{
			get { return _x / 2; }
		}

		private int P4 { get; set; }

		private int P5
		{
			set { _x = value; }
		}

		private int P6
		{
			get { return _x * 2; }
			set { _x = value / 2; }
		}

		protected override void Check()
		{
			_x = 2;
			P1.ShouldBe(4);
			P3.ShouldBe(1);

			_x = 10;
			P1.ShouldBe(20);
			P3.ShouldBe(5);

			P2.ShouldBe(77);

			var c1 = new C(7);
			c1.P.ShouldBe(7);

			var c2 = new C(77);
			c2.P.ShouldBe(77);

			P4 = 9;
			P4.ShouldBe(9);

			P4 = 11;
			P4.ShouldBe(11);

			P5 = 13;
			_x.ShouldBe(13);

			P5 = 131;
			_x.ShouldBe(131);

			P6 = 16;
			P6.ShouldBe(16);

			P6 = 8;
			P6.ShouldBe(8);
		}

		private class C : Component
		{
			public C(int p)
			{
				P = p;
			}

			public int P { get; }
		}
	}
}