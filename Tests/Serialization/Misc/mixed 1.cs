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

namespace Tests.Serialization.Misc
{
	using System;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Mixed1 : SerializationObject
	{
		public enum E : long
		{
			A,
			B = Int64.MaxValue,
			C = 5
		}

		protected override void Check()
		{
			var c = new C { F = true, G = E.B, H = -1247 };

			GenerateCode(SerializationMode.Full, c);
			StateSlotCount.ShouldBe(4);

			Serialize();
			c.F = false;
			c.G = E.C;
			c.H = 3;
			Deserialize();
			c.F.ShouldBe(true);
			c.G.ShouldBe(E.B);
			c.H.ShouldBe(-1247);
		}

		internal class C
		{
			public bool F;
			public E G;
			public int H;
		}
	}
}