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

	internal class BooleanOneBytes : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { A = true, B = false, D = true, E = false, F = true, G = false, H = true, I = true, X = 99 };

			GenerateCode(SerializationMode.Optimized, c);
			StateSlotCount.ShouldBe(2);

			Serialize();
			c.A = false;
			c.B = false;
			c.D = false;
			c.E = false;
			c.F = false;
			c.G = false;
			c.H = false;
			c.I = false;
			c.X = 0;

			Deserialize();
			c.A.ShouldBe(true);
			c.B.ShouldBe(false);
			c.D.ShouldBe(true);
			c.E.ShouldBe(false);
			c.F.ShouldBe(true);
			c.G.ShouldBe(false);
			c.H.ShouldBe(true);
			c.I.ShouldBe(true);
			c.X.ShouldBe(99);
		}

		private class C
		{
			public bool A;
			public bool B;
			public bool D;
			public bool E;
			public bool F;
			public bool G;
			public bool H;
			public bool I;
			public int X;
		}
	}
}