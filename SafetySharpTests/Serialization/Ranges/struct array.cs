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

namespace Tests.Serialization.Ranges
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class StructArray : SerializationObject
	{
		protected override void Check()
		{
			var d = new D { S = new[] { new S { F = 2, G = 3, H = 4 }, new S { F = 6, G = 7, H = 8 } } };
			GenerateCode(SerializationMode.Optimized, d);
			StateVectorSize.ShouldBe(8);

			Serialize();
			d.S[0].F = 33;
			d.S[0].G = 0;
			d.S[0].H = 0;

			Deserialize();
			d.S[0].F.ShouldBe(2);
			d.S[0].G.ShouldBe(3);
			d.S[0].H.ShouldBe(4);
			d.S[1].F.ShouldBe(6);
			d.S[1].G.ShouldBe(7);
			d.S[1].H.ShouldBe(8);

			d.S[0].G = -1;
			d.S[1].G = -1;
			Serialize();
			Deserialize();

			d.S[0].F.ShouldBe(2);
			d.S[0].G.ShouldBe(0);
			d.S[0].H.ShouldBe(4);
			d.S[1].F.ShouldBe(6);
			d.S[1].G.ShouldBe(0);
			d.S[1].H.ShouldBe(8);

			d.S[0].G = 121;
			d.S[1].G = 121;
			Serialize();
			Deserialize();

			d.S[0].F.ShouldBe(2);
			d.S[0].G.ShouldBe(10);
			d.S[0].H.ShouldBe(4);
			d.S[1].F.ShouldBe(6);
			d.S[1].G.ShouldBe(10);
			d.S[1].H.ShouldBe(8);

			d.S[0].G = 3;
			d.S[1].G = 7;
			d.S[0].H = -1;
			d.S[1].H = -1;
			Serialize();
			Deserialize();

			d.S[0].F.ShouldBe(2);
			d.S[0].G.ShouldBe(3);
			d.S[0].H.ShouldBe(10);
			d.S[1].F.ShouldBe(6);
			d.S[1].G.ShouldBe(7);
			d.S[1].H.ShouldBe(10);

			d.S[0].H = 111;
			d.S[1].H = 111;
			Serialize();
			Deserialize();

			d.S[0].F.ShouldBe(2);
			d.S[0].G.ShouldBe(3);
			d.S[0].H.ShouldBe(0);
			d.S[1].F.ShouldBe(6);
			d.S[1].G.ShouldBe(7);
			d.S[1].H.ShouldBe(0);

			d.S[0].F = -1;
			Should.Throw<RangeViolationException>(() => Serialize());

			d.S[0].F = 2;
			d.S[1].F = -1;
			Should.Throw<RangeViolationException>(() => Serialize());
		}

		internal struct S
		{
			[Range(0, 10, OverflowBehavior.Error)]
			public int F;

			[Range(0, 10, OverflowBehavior.Clamp)]
			public int G;

			[Range(0, 10, OverflowBehavior.WrapClamp)]
			public int H;
		}

		internal class D : Component
		{
			public S[] S;
		}
	}
}