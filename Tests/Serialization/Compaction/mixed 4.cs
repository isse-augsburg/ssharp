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

namespace Tests.Serialization.Compaction
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Mixed4 : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { A = true };
			var d1 = new D { A = -1 };
			var d2 = new D { A = 55 };
			var d3 = new F { E = E.B };

			GenerateCode(SerializationMode.Optimized, d1, c, d3, d2);
			StateSlotCount.ShouldBe(1);

			Serialize();
			c.A = false;
			d1.A = 0;
			d2.A = 0;
			d3.E = 0;

			Deserialize();
			c.A.ShouldBe(true);
			d1.A.ShouldBe(0);
			d2.A.ShouldBe(55);
			d3.E.ShouldBe(E.B);

			c.A = false;
			d1.A = 1;
			d2.A = 2;
			d3.E = E.A;

			Serialize();
			c.A = true;
			d1.A = 0;
			d2.A = 0;
			d3.E = 0;

			Deserialize();
			c.A.ShouldBe(false);
			d1.A.ShouldBe(1);
			d2.A.ShouldBe(2);
			d3.E.ShouldBe(E.A);

			c.A = false;
			d1.A = 12;
			d2.A = 22;
			d3.E = E.C;

			Serialize();
			c.A = true;
			d1.A = 0;
			d2.A = 0;
			d3.E = 0;

			Deserialize();
			c.A.ShouldBe(false);
			d1.A.ShouldBe(12);
			d2.A.ShouldBe(22);
			d3.E.ShouldBe(E.C);
		}

		private class C
		{
			public bool A;
		}

		private class D
		{
			[Range(0, 100, OverflowBehavior.Clamp)]
			public int A;
		}

		private class F
		{
			public E E;
		}

		private enum E
		{
			A, B, C
		}
	}
}