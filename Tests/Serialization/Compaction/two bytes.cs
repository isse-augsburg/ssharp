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

	internal class TwoBytes : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { A = 3, B = -17 };
			var a = new short[] { 1, 2 };

			GenerateCode(SerializationMode.Optimized, a, c);
			StateSlotCount.ShouldBe(3);

			Serialize();
			c.B = 2;
			c.A = 1;
			a[0] = 0;
			a[1] = 0;

			Deserialize();
			c.B.ShouldBe((short)-17);
			c.A.ShouldBe((ushort)3);
			a.ShouldBe(new short[] { 1, 2 });
		}

		private class C
		{
			public ushort A;
			public short B;
			public short D;
		}
	}
}