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

	internal class Mixed3 : SerializationObject
	{
		protected override void Check()
		{
			var c1 = new C { A = true };
			var c2 = new C { A = false };
			var d = new D { A = 99, B = 3 };
			 
			GenerateCode(SerializationMode.Optimized, c1, c2, d);
			StateSlotCount.ShouldBe(1);

			Serialize();
			c1.A = false;
			c2.A = false;
			d.A = 292;
			d.B = 9;

			Deserialize();
			c1.A.ShouldBe(true);
			c2.A.ShouldBe(false);
			d.A.ShouldBe((short)99);
			d.B.ShouldBe((byte)3);
		}

		private class C
		{
			public bool A;
		}

		private class D
		{
			public short A;
			public byte B;
		}
	}
}