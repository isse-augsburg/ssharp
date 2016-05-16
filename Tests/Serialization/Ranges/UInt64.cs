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

namespace Tests.Serialization.Ranges
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class UInt64 : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { F = 1, G = 2, H = 3 };

			GenerateCode(SerializationMode.Optimized, c);

			Serialize();
			c.F = 31;
			c.G = 99;
			c.H = 77;
			Deserialize();
			c.F.ShouldBe((ulong)1);
			c.G.ShouldBe((ulong)2);
			c.H.ShouldBe((ulong)3);

			c.F = 99;
			Should.Throw<RangeViolationException>(() => Serialize());

			c.F = 1;
			c.G = 0;
			c.H = 0;

			Serialize();
			Deserialize();

			c.F.ShouldBe((ulong)1);
			c.G.ShouldBe((ulong)1);
			c.H.ShouldBe((ulong)6);

			c.G = 1;
			c.H = 2;

			Serialize();
			Deserialize();

			c.G.ShouldBe((ulong)1);
			c.H.ShouldBe((ulong)2);

			c.G = 2;
			c.H = 3;

			Serialize();
			Deserialize();

			c.G.ShouldBe((ulong)2);
			c.H.ShouldBe((ulong)3);

			c.G = 3;
			c.H = 6;

			Serialize();
			Deserialize();

			c.G.ShouldBe((ulong)3);
			c.H.ShouldBe((ulong)6);

			c.G = 4;
			c.H = 7;

			Serialize();
			Deserialize();

			c.G.ShouldBe((ulong)3);
			c.H.ShouldBe((ulong)2);

			c.G = 5;
			c.H = 8;

			Serialize();
			Deserialize();

			c.G.ShouldBe((ulong)3);
			c.H.ShouldBe((ulong)2);

			c.G = System.UInt64.MaxValue;
			c.H = System.UInt64.MaxValue;
			c.Q = System.UInt64.MaxValue;

			Serialize();
			Deserialize();

			c.G.ShouldBe((ulong)3);
			c.H.ShouldBe((ulong)2);
			c.Q.ShouldBe(System.UInt64.MaxValue);

			c.G = System.UInt64.MinValue;
			c.H = System.UInt64.MinValue;
			c.Q = System.UInt64.MinValue;

			Serialize();
			Deserialize();

			c.G.ShouldBe((ulong)1);
			c.H.ShouldBe((ulong)6);
			c.Q.ShouldBe(System.UInt64.MinValue);
		}

		internal class C
		{
			[Range(1, 5, OverflowBehavior.Error)]
			public ulong F;

			public ulong G;
			public ulong H;
			public ulong Q;

			public C()
			{
				Range.Restrict(G, 1u, 3u, OverflowBehavior.Clamp);
				Range.Restrict(H, 2u, 6u, OverflowBehavior.WrapClamp);
			}
		}
	}
}