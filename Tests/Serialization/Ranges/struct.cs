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
	using System;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Struct : SerializationObject
	{
		protected override void Check()
		{
			var d = new D { S = { F = 3, G = 4, H = 2 } };
			GenerateCode(SerializationMode.Optimized, d);
			StateVectorSize.ShouldBe(4);

			Serialize();
			d.S.F = 33;
			d.S.G = 0;
			d.S.H = 0;

			Deserialize();
			d.S.F.ShouldBe(3);
			d.S.G.ShouldBe(4);
			d.S.H.ShouldBe(2);

			d.S.G = -1;
			Serialize();
			Deserialize();

			d.S.F.ShouldBe(3);
			d.S.G.ShouldBe(0);
			d.S.H.ShouldBe(2);

			d.S.G = 121;
			Serialize();
			Deserialize();

			d.S.F.ShouldBe(3);
			d.S.G.ShouldBe(5);
			d.S.H.ShouldBe(2);

			d.S.H = -1;
			Serialize();
			Deserialize();

			d.S.F.ShouldBe(3);
			d.S.G.ShouldBe(5);
			d.S.H.ShouldBe(5);

			d.S.H = 111;
			Serialize();
			Deserialize();

			d.S.F.ShouldBe(3);
			d.S.G.ShouldBe(5);
			d.S.H.ShouldBe(0);

			d.S.F = -1;
			Should.Throw<RangeViolationException>(() => Serialize());
		}

		internal struct S
		{
			[Range(0, 5, OverflowBehavior.Error)]
			public int F;

			[Range(0, 5, OverflowBehavior.Clamp)]
			public int G;

			[Range(0, 5, OverflowBehavior.WrapClamp)]
			public int H;
		}

		internal class D : Component
		{
			public S S;

			public D()
			{
				Should.Throw<ArgumentException>(() => Restrict());
			}

			private void Restrict()
			{
				Range.Restrict(S.F, 0, 3, OverflowBehavior.Clamp);
			}
		}
	}
}