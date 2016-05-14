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

	internal class PropertyRangesMethods : SerializationObject
	{
		protected override void Check()
		{
			var c = new C { F = 1, G = 2, H = 3 };

			GenerateCode(SerializationMode.Full, c);

			Serialize();
			c.F = 31;
			c.G = 99;
			c.H = 77;
			Deserialize();
			c.F.ShouldBe(1);
			c.G.ShouldBe(2);
			c.H.ShouldBe(3);

			c.F = -17;
			Should.Throw<RangeViolationException>(() => Serialize());

			c.F = 99;
			Should.Throw<RangeViolationException>(() => Serialize());

			c.F = 1;
			c.G = -9;
			c.H = -4;

			Serialize();
			Deserialize();

			c.F.ShouldBe(1);
			c.G.ShouldBe(-1);
			c.H.ShouldBe(6);

			c.G = -1;
			c.H = -2;

			Serialize();
			Deserialize();

			c.G.ShouldBe(-1);
			c.H.ShouldBe(-2);

			c.G = 0;
			c.H = 0;

			Serialize();
			Deserialize();

			c.G.ShouldBe(0);
			c.H.ShouldBe(0);

			c.G = 3;
			c.H = 6;

			Serialize();
			Deserialize();

			c.G.ShouldBe(3);
			c.H.ShouldBe(6);

			c.G = 4;
			c.H = 7;

			Serialize();
			Deserialize();

			c.G.ShouldBe(3);
			c.H.ShouldBe(-2);

			c.G = 40;
			c.H = 8;

			Serialize();
			Deserialize();

			c.G.ShouldBe(3);
			c.H.ShouldBe(-2);

			c.G = System.Int32.MaxValue;
			c.H = System.Int32.MaxValue;
			c.Q = System.Int32.MaxValue;

			Serialize();
			Deserialize();

			c.G.ShouldBe(3);
			c.H.ShouldBe(-2);
			c.Q.ShouldBe(System.Int32.MaxValue);

			c.G = System.Int32.MinValue;
			c.H = System.Int32.MinValue;
			c.Q = System.Int32.MinValue;

			Serialize();
			Deserialize();

			c.G.ShouldBe(-1);
			c.H.ShouldBe(6);
			c.Q.ShouldBe(System.Int32.MinValue);
		}

		internal class C
		{
			public C()
			{
				Range.Restrict(F, -1, 5, OverflowBehavior.Error);
				Range.Restrict(G, -1, 3, OverflowBehavior.Clamp);
				Range.Restrict(H, -2, 6, OverflowBehavior.WrapClamp);
				Range.Restrict(R, -2, 6, OverflowBehavior.WrapClamp);

				Should.Throw<ArgumentException>(() => InvalidRange());
			}

			public int F { get; set; }
			public int G { get; set; }
			public int H { get; set; }
			public int Q { get; set; }
			public int R { get; }
			public int S => 5;

			private void InvalidRange()
			{
				Range.Restrict(S, 0, 2, OverflowBehavior.Clamp);
			}
		}
	}
}