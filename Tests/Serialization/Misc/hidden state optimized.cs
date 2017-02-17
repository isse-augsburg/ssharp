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

	internal class HiddenOptimized : SerializationObject
	{
		public enum E : long
		{
			A,
			B = Int64.MaxValue,
			C = 5
		}

		protected override void Check()
		{
			var c = new C { F = true, G = -1247, H = E.B, I = 33, D = new D { T = 77 }, T = new F { T = 12 } };

			GenerateCode(SerializationMode.Optimized, c);
			StateSlotCount.ShouldBe(1);

			Serialize();
			c.F = false;
			c.G = 3;
			c.H = E.C;
			c.I = 88;
			c.D.T = 0;
			c.T.T = 0;
			Deserialize();
			c.F.ShouldBe(false);
			c.G.ShouldBe(-1247);
			c.H.ShouldBe(E.C);
			c.I.ShouldBe(88);
			c.J.ShouldBe(333);
			c.K.ShouldBe(11);
			c.D.T.ShouldBe(0);
			c.T.ShouldNotBeNull();
		}

		internal class C
		{
			[NonSerializable]
			public readonly int J = 333;

			[Hidden]
			public readonly int K = 11;

			public D D;

			[Hidden]
			public bool F;

			public int G;

			[Hidden]
			public E H;

			[NonSerializable]
			public int I;

			public F T;
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