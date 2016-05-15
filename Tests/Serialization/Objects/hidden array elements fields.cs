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

namespace Tests.Serialization.Objects
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class HiddenArrayElementsFields : SerializationObject
	{
		protected override void Check()
		{
			var d1 = new D { X = 5 };
			var d2 = new D { X = 77 };

			var c = new C
			{
				A = new[] { -17, 2, 12 },
				B = new[] { 9, 3 },
				E = new[] { d1, d2 }
			};

			GenerateCode(SerializationMode.Optimized, c);
			StateSlotCount.ShouldBe(7);

			Serialize();
			c.A[0] = -3331;
			c.B[0] = 9999;
			c.D[0] = 73929;
			d1.X = 72893;
			d2.X = 9932;
			c.F[0].X = 293945;

			Deserialize();
			c.A.ShouldBe(new[] { -3331, 2, 12 });
			c.B.ShouldBe(new[] { 9, 3 });
			c.D.ShouldBe(new[] { 333, 444 });
			d1.X.ShouldBe(5);
			d2.X.ShouldBe(77);
			c.F[0].X.ShouldBe(99);

			GenerateCode(SerializationMode.Full, c);

			Serialize();
			c.A[0] = -332531;
			c.B[0] = 99399;
			c.D[0] = 735929;
			d1.X = 722893;
			d2.X = 93932;
			c.F[0].X = 2935945;

			Deserialize();
			c.A.ShouldBe(new[] { -3331, 2, 12 });
			c.B.ShouldBe(new[] { 9, 3 });
			c.D.ShouldBe(new[] { 333, 444 });
			d1.X.ShouldBe(5);
			d2.X.ShouldBe(77);
			c.F[0].X.ShouldBe(99);
		}

		private class C : Component
		{
			public readonly int[] D = { 333, 444 };

			[Hidden(HideElements = true)]
			public readonly D[] F = { new D { X = 99 } };

			[Hidden(HideElements = true)]
			public int[] A;

			[Hidden]
			public int[] B;

			[Hidden(HideElements = true)]
			public D[] E;
		}

		private class D
		{
			public int X;
		}
	}
}