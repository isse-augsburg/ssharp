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

namespace Tests.Serialization.Objects
{
	using System;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal unsafe class Arrays : SerializationObject
	{
		protected override void Check()
		{
			var o1 = new object();
			var o2 = new object();
			var c = new C
			{
				I = new[] { -17, 2, 12 },
				D = new[] { Int64.MaxValue, Int64.MinValue },
				B = new[] { true, false, true },
				P = new[] { (int*)17, (int*)19 },
				O = new[] { o1, o2 },
				E = new[] { E.A, E.C },
				S = new short[] { 33, 77, 29292, -22923 }
			};

			GenerateCode(SerializationMode.Optimized, c, c.I, o1, o2, c.D, c.B, c.P, c.O, c.E, c.S);
			StateSlotCount.ShouldBe(29);

			Serialize();
			c.I[1] = 33;
			c.D[0] = Int64.MinValue;
			c.B[2] = false;
			c.P[0] = (int*)-1;
			c.O[1] = null;
			c.E[1] = E.C;
			c.S[0] = -3595;
			c.S[3] = 9923;
			c.I = null;
			c.D = null;
			c.B = null;
			c.P = null;
			c.O = null;
			c.E = null;
			c.S = null;
			Deserialize();
			c.I.ShouldBe(new[] { -17, 2, 12 });
			c.D.ShouldBe(new[] { Int64.MaxValue, Int64.MinValue });
			c.B.ShouldBe(new[] { true, false, true });
			c.O.ShouldBe(new[] { o1, o2 });
			c.E.ShouldBe(new[] { E.A, E.C });
			c.S.ShouldBe(new short[] { 33, 77, 29292, -22923 });

			c.P.Length.ShouldBe(2);
			((ulong)c.P[0]).ShouldBe((ulong)17);
			((ulong)c.P[1]).ShouldBe((ulong)19);
		}

		private class C : Component
		{
			public bool[] B;
			public long[] D;
			public E[] E;
			public int[] I;
			public object[] O;
			public int*[] P;
			public short[] S;
		}

		private enum E
		{
			A,
			B,
			C
		}
	}
}