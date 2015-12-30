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

namespace Tests.Serialization.Compaction
{
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Mixed1 : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { A = 3, B = -17, D = 99, E = 77, F = 9234, G = 48483 };
			var a = new byte[] { 1, 2 };

			GenerateCode(SerializationMode.Optimized, a, c);
			StateSlotCount.ShouldBe(5);

			Serialize();
			c.A = 1;
			c.B = 2;
			c.D = 1;
			c.E = 9;
			c.F = 4;
			c.G = 9;
			a[0] = 0;
			a[1] = 0;

			Deserialize();
			c.A.ShouldBe((sbyte)3);
			c.B.ShouldBe((sbyte)-17);
			c.D.ShouldBe((short)99);
			c.E.ShouldBe((short)77);
			c.F.ShouldBe(9234);
			c.G.ShouldBe(48483);
			a.ShouldBe(new byte[] { 1, 2 });
		}

		private class C
		{
			public sbyte A;
			public sbyte B;
			public short D;
			public short E;
			public int F;
			public long G;
		}
	}
}