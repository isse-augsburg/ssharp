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

namespace Tests.Serialization.Objects
{
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class NullableStruct : SerializationObject
	{
		protected override void Check()
		{
			var c = new C
			{
				X = new X
				{
					A = 3,
					B = 6,
					C = new Y { A = 1, B = 2, C = true },
				},
				Y = new Y
				{
					A = 17,
					B = 44,
					C = true
				}
			};

			GenerateCode(SerializationMode.Optimized, c);
			StateSlotCount.ShouldBe(4);

			Serialize();
			c.X = new X();
			c.Y = new Y();

			Deserialize();
			c.X.ShouldNotBeNull();
			c.Y.ShouldNotBeNull();
			c.X.Value.C.ShouldNotBeNull();
			c.X.Value.A.ShouldBe(3);
			c.X.Value.B.ShouldBe((ushort)6);
			c.X.Value.C.Value.A.ShouldBe((byte)1);
			c.X.Value.C.Value.B.ShouldBe((ushort)2);
			c.Y.Value.A.ShouldBe((byte)17);
			c.Y.Value.B.ShouldBe((ushort)44);
			c.Y.Value.C.ShouldBe(true);

			c.X = new X();
			c.Y = new Y();

			Serialize();
			Deserialize();

			c.X.ShouldNotBeNull();
			c.Y.ShouldNotBeNull();
			c.X.Value.A.ShouldBeNull();
			c.X.Value.B.ShouldBeNull();
			c.X.Value.C.ShouldBeNull();
			c.Y.Value.A.ShouldBeNull();
			c.Y.Value.B.ShouldBeNull();
			c.Y.Value.C.ShouldBeNull();
		}

		private struct X
		{
			public int? A;
			public ushort? B;
			public Y? C;
		}

		private struct Y
		{
			public byte? A;
			public ushort? B;
			public bool? C;
		}

		private class C : Component
		{
			public X? X;
			public Y? Y;
		}
	}
}