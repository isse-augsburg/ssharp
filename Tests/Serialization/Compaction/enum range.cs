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
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class EnumRange : SerializationObject
	{
		protected override void Check()
		{
			var a = new[] { E8.A, E8.B };
			var b = new[] { E16.A, E16.B };
			var c = new[] { E32.A, E32.B };
			var d = new C { A = E8.A, B = E8.B };

			GenerateCode(SerializationMode.Optimized, a, b, c, d);
			StateSlotCount.ShouldBe(4);

			Serialize();
			a[0] = 0;
			a[1] = 0;
			b[0] = 0;
			b[1] = 0;
			c[0] = 0;
			c[1] = 0;
			d.A = E8.A;
			d.B = E8.B;

			Deserialize();
			a.ShouldBe(new[] { E8.A, E8.B });
			b.ShouldBe(new[] { E16.A, E16.B });
			c.ShouldBe(new[] { E32.A, E32.B });
			d.A.ShouldBe(E8.A);
			d.B.ShouldBe(E8.B);
		}

		private class C
		{
			public E8 A;
			public E8 B;
		}

		private enum E8
		{
			A,
			B
		}

		private enum E16
		{
			A,
			B = 39492
		}

		private enum E32
		{
			A,
			B = 393949395
		}
	}
}