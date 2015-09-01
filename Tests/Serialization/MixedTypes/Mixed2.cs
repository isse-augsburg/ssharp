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

namespace Tests.Serialization.MixedTypes
{
	using System;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Mixed2 : SerializationObject
	{
		public enum E : long
		{
			A,
			B = Int64.MaxValue,
			C = 5
		}

		protected override void Check()
		{
			var o = new object();
			var c = new C { D = 17, E = o,F = true, G = E.B, H = -1247 };

			GenerateCode(SerializationMode.Full, c, o);
			_stateSlotCount.ShouldBe(6);

			Serialize();
			c.D = 0;
			c.E = null;
			c.F = false;
			c.G = E.C;
			c.H = 3;
			Deserialize();
			c.D.ShouldBe((ushort)17);
			c.E.ShouldBe(o);
			c.F.ShouldBe(true);
			c.G.ShouldBe(E.B);
			c.H.ShouldBe(-1247);

			c.E = null;
			Serialize();
			c.E = o;
			Deserialize();
			c.D.ShouldBe((ushort)17);
			c.E.ShouldBe(null);
			c.F.ShouldBe(true);
			c.G.ShouldBe(E.B);
			c.H.ShouldBe(-1247);
		}

		internal class C
		{
			public ushort D;
			public object E;
			public bool F;
			public E G;
			public int H;
		}
	}
}