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

	internal class OneByte : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { A = 3, B = -17, S = { A = 3 }, T = new[] { new S { A = 99 }, new S { A = 81 } } };
			var a = new[] { E.A, E.B };

			GenerateCode(SerializationMode.Optimized, a, c);
			StateSlotCount.ShouldBe(3);

			Serialize();
			c.B = 2;
			c.A = 1;
			a[0] = 0;
			a[1] = 0;
			c.S = new S();
			c.T[0] = new S();
			c.T[1] = new S();

			Deserialize();
			c.B.ShouldBe((sbyte)-17);
			c.A.ShouldBe((byte)3);
			a.ShouldBe(new[] { E.A, E.B });
			c.S.A.ShouldBe((byte)3);
			c.T[0].A.ShouldBe((byte)99);
			c.T[1].A.ShouldBe((byte)81);
		}

		private class C
		{
			public byte A;
			public sbyte B;
			public S S;
			public S[] T;
		}

		private enum E : byte
		{
			A,
			B
		}

		private struct S
		{
			public byte A;
		}
	}
}