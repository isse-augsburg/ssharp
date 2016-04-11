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
	using System;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Delegates : SerializationObject
	{
		protected override void Check()
		{
			var c = new C();

			GenerateCode(SerializationMode.Full, c);

			Serialize();
			c.O = null;
			c.P = null;
			c.N = () => { };
			c.X = null;
			c.Y = null;
			c.Z = null;
			c.W = null;
			c.V = null;
			c.Multi = null;

			Deserialize();

			c.O(17);
			c.P().ShouldBe(50);
			c.N.ShouldBe(null);

			var b = false;
			short s;
			c.X(11, ref b, out s).ShouldBe(2);

			c.D.X.ShouldBe(39);
			b.ShouldBe(true);
			s.ShouldBe((short)199);

			c.Y(33).ShouldBe(33);
			c.Z(11).ShouldBe(22);
			c.W(17).ShouldBe(56);
			c.V(191).ShouldBe(191);

			c.Multi();
			c.D.X.ShouldBe(41);
		}

		private class C
		{
			public readonly D D = new D();
			public Action<int> O;
			public Func<int> P;
			public Action N;
			public X X;
			public Func<int, int> Y;
			public Func<int, int> Z;
			public Func<int, int> W;
			public Func<int, int> V;
			public Action Multi;

			public C()
			{
				O += v => D.X += v;
				O += v => D.X += 2 * v - 1;
				P = () => D.X;
				X = M;
				Y = R;
				Z = S;
				W = w => w + D.X;
				Multi = () => ++D.X;
				Multi += () => ++D.X;
				V = Q<int>.R;
			}

			private int M(int a, ref bool b, out short c)
			{
				D.X -= a;
				b = !b;
				c = 199;

				return 2;
			}

			private T R<T>(T t)
			{
				return t;
			}

			static int S(int s)
			{
				return s * 2;
			}
		}

		private class D
		{
			private object o = new object();
			public int X;
		}

		private class Q<T>
		{
			public static T R(T t)
			{
				return t;
			}
		}

		private delegate int X(int a, ref bool b, out short c);
	}
}