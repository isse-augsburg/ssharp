// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace Tests.Serialization.Misc
{
	using System;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class HiddenStateStruct : SerializationObject
	{
		public enum E : long
		{
			A,
			B = Int64.MaxValue,
			C = 5
		}

		protected override void Check()
		{
			var c = new C(1) { F = true, G = -1247, H = E.B, I = 33, D = new D { T = 77 }, T = new F { T = 12 } };
			var x = new X { C = c, H = c };

			GenerateCode(SerializationMode.Optimized, x);
			StateSlotCount.ShouldBe(1);

			Serialize();
			x.C.F = false;
			x.C.G = 3;
			x.C.H = E.C;
			x.C.I = 88;
			x.C.D.T = 0;
			x.C.T.T = 0;
			x.H.F = false;
			x.H.G = 3;
			x.H.H = E.C;
			x.H.I = 88;
			x.H.D.T = 0;
			x.H.T.T = 0;

			Deserialize();
			x.C.F.ShouldBe(false);
			x.C.G.ShouldBe(-1247);
			x.C.H.ShouldBe(E.C);
			x.C.I.ShouldBe(88);
			x.C.J.ShouldBe(333);
			x.C.K.ShouldBe(11);
			x.C.D.T.ShouldBe(0);
			x.C.T.T.ShouldBe(0);
			x.H.F.ShouldBe(false);
			x.H.G.ShouldBe(3);
			x.H.H.ShouldBe(E.C);
			x.H.I.ShouldBe(88);
			x.H.J.ShouldBe(333);
			x.H.K.ShouldBe(11);
			x.H.D.T.ShouldBe(0);
			x.H.T.T.ShouldBe(0);
		}

		internal struct C
		{
			[NonSerializable]
			public readonly int J;

			[Hidden]
			public readonly int K;

			public D D;

			[Hidden]
			public bool F;

			public int G;

			[Hidden]
			public E H;

			[NonSerializable]
			public int I;

			public F T;

			public C(int i)
				: this()
			{
				J = 333;
				K = 11;
			}
		}

		internal class X
		{
			public C C;

			[Hidden]
			public C H;
		}

		[Hidden]
		internal class D
		{
			public int T;
		}

		[NonSerializable]
		internal class F
		{
			public int T;
		}
	}
}