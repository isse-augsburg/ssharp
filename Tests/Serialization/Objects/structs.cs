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

	internal class Struct : SerializationObject
	{
		protected override void Check()
		{
			var o = new object();
			var e = new[] { new object() };

			var c = new C
			{
				X =
				{
					A = 3,
					B = 6,
					C = { A = 1, B = 2 },
					D = o,
					E = e,
					F = new[] { new Y { A = 11, B = 12, C = false }, new Y { A = 13, B = 14, C = true } }
				},
				Y =
				{
					A = 17,
					B = 44,
					C = true
				}
			};

			c.Z = new Z { A = 99, B = c.X };

			GenerateCode(SerializationMode.Optimized, c, e, o, c.X.F);
			StateSlotCount.ShouldBe(12);

			Serialize();
			c.X.F[0] = new Y();
			c.X.F[1] = new Y();
			c.X = new X();
			c.Y = new Y();
			c.Z = new Z();

			Deserialize();
			c.X.A.ShouldBe(3);
			c.X.B.ShouldBe((ushort)6);
			c.X.C.A.ShouldBe((byte)1);
			c.X.C.B.ShouldBe((ushort)2);
			c.X.D.ShouldBe(o);
			c.X.E.ShouldBe(e);
			c.X.F[0].A.ShouldBe((byte)11);
			c.X.F[0].B.ShouldBe((ushort)12);
			c.X.F[0].C.ShouldBe(false);
			c.X.F[1].A.ShouldBe((byte)13);
			c.X.F[1].B.ShouldBe((ushort)14);
			c.X.F[1].C.ShouldBe(true);
			c.Y.A.ShouldBe((byte)17);
			c.Y.B.ShouldBe((ushort)44);
			c.Y.C.ShouldBe(true);
			c.Z.A.ShouldBe(99);
			c.Z.B.A.ShouldBe(3);
			c.Z.B.B.ShouldBe((ushort)6);
			c.Z.B.C.A.ShouldBe((byte)1);
			c.Z.B.C.B.ShouldBe((ushort)2);
			c.Z.B.D.ShouldBe(o);
			c.Z.B.E.ShouldBe(e);
			c.Z.B.F[0].A.ShouldBe((byte)11);
			c.Z.B.F[0].B.ShouldBe((ushort)12);
			c.Z.B.F[0].C.ShouldBe(false);
			c.Z.B.F[1].A.ShouldBe((byte)13);
			c.Z.B.F[1].B.ShouldBe((ushort)14);
			c.Z.B.F[1].C.ShouldBe(true);

			c.Y.C = false;
			Serialize();

			c.Y.C = true;
			Deserialize();
			c.Y.C.ShouldBe(false);
		}

		private struct X
		{
			public int A;
			public ushort B;
			public Y C;
			public object D;
			public object[] E;
			public Y[] F;
		}

		private struct Y
		{
			public byte A;
			public ushort B;
			public bool C;
		}

		private struct Z
		{
			public int A;
			public X B;
		}

		private class C : Component
		{
			public X X;
			public Y Y;
			public Z Z;
		}
	}
}