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

namespace Tests.Serialization.Compaction
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Int16Range : SerializationObject
	{
		protected override void Check()
		{
			var x = new X { A = 1, B = 2, C = 3, D = 1001 };

			GenerateCode(SerializationMode.Optimized, x);
			StateSlotCount.ShouldBe(2);

			Serialize();
			x.A = 0;
			x.B = 0;
			x.C = 0;
			x.D = 0;

			Deserialize();
			x.A.ShouldBe((short)1);
			x.B.ShouldBe((ushort)2);
			x.C.ShouldBe((short)3);
			x.D.ShouldBe((ushort)1001);
		}

		private class X
		{
			[Range(-30, 30, OverflowBehavior.Error)]
			public short A;

			[Range(0, 30, OverflowBehavior.Error)]
			public ushort B;

			[Range(-1000, 1000, OverflowBehavior.Error)]
			public short C;

			[Range(1000, 1100, OverflowBehavior.Error)]
			public ushort D;
		}
	}
}