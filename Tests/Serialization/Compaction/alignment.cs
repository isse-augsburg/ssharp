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

	internal class Alignment : SerializationObject
	{
		protected override void Check()
		{
			var x = new X { A = true, B = false, C = true, D = true, E = 129, F = 3293, G = 49493 };

			GenerateCode(SerializationMode.Optimized, x);
			StateSlotCount.ShouldBe(2);

			Serialize();
			x.A = false;
			x.B = false;
			x.C = false;
			x.D = false;
			x.E = 0;
			x.F = 0;
			x.G = 0;

			Deserialize();
			x.A.ShouldBe(true);
			x.B.ShouldBe(false);
			x.C.ShouldBe(true);
			x.D.ShouldBe(true);
			x.E.ShouldBe((byte)129);
			x.F.ShouldBe((short)3293);
			x.G.ShouldBe(49493);
		}

		private class X
		{
			public bool A, B, C, D;
			public byte E;
			public short F;
			public int G;
		}
	}
}