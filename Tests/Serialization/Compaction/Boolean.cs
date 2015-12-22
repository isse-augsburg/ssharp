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

	internal unsafe class Boolean : SerializationObject
	{
		protected override void Check()
		{
			var c1 = new C { A = true, B = false, D = true };
			var a1 = new[] { true, false, true };
			var c2 = new C { A = false, B = true, D = false };
			var a2 = new[] { true, false };

			GenerateCode(SerializationMode.Optimized, c1, a1, c2, a2);
			StateSlotCount.ShouldBe(1);

			Serialize();
			c1.B = false;
			c1.A = false;
			c2.B = false;
			c2.A = false;
			a1[0] = false;
			a1[1] = false;
			a1[2] = false;
			a2[0] = false;
			a2[1] = false;

			Deserialize();
			SerializedState[2].ShouldBe((byte)0);
			SerializedState[3].ShouldBe((byte)0);
			c1.A.ShouldBe(true);
			c1.B.ShouldBe(false);
			c1.D.ShouldBe(true);
			a1.ShouldBe(new[] { true, false, true });
			c2.A.ShouldBe(false);
			c2.B.ShouldBe(true);
			c2.D.ShouldBe(false);
			a2.ShouldBe(new[] { true, false });
		}

		private class C
		{
			public bool A;
			public bool B;
			public bool D;
		}
	}
}