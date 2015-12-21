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

	internal class OneByte : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { A = 3, B = -17 };
			var a = new [] { E.A, E.B };

			GenerateCode(SerializationMode.Optimized, a, c);
			StateSlotCount.ShouldBe(1);

			Serialize();
			c.B = 2;
			c.A = 1;
			a[0] = 0;
			a[1] = 0;

			Deserialize();
			c.B.ShouldBe((sbyte)-17);
			c.A.ShouldBe((byte)3);
			a.ShouldBe(new [] { E.A, E.B });
		}

		private class C
		{
			public byte A;
			public sbyte B;
		}

		private enum E : byte
		{
			A, B
		}
	}
}