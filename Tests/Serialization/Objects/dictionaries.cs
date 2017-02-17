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

namespace Tests.Serialization.Objects
{
	using System.Collections.Generic;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Dictionaries : SerializationObject
	{
		protected override void Check()
		{
			var o1 = new object();
			var o2 = new object();
			var c = new C
			{
				D = new Dictionary<int, int> { [1] = 3, [5] = 8, [-1] = -1 },
				E = new Dictionary<object, object> { [o1] = o2, [o2] = o1 },
				F = new Dictionary<int, X> { [0] = new X { A = 2, O = o1 } }
			};

			GenerateCode(SerializationMode.Optimized, c, o1, o2);
			Serialize();

			c.D.Clear();
			c.E.Clear();

			c.D[344] = 32;
			c.E[new object()] = new object();
			c.F[1] = new X();
			c.G[4] = 6;

			Deserialize();

			c.D.Count.ShouldBe(3);
			c.D[1].ShouldBe(3);
			c.D[5].ShouldBe(8);
			c.D[-1].ShouldBe(-1);

			c.E.Count.ShouldBe(2);
			c.E[o1].ShouldBe(o2);
			c.E[o2].ShouldBe(o1);

			c.F.Count.ShouldBe(1);
			c.F[0].A.ShouldBe(2);
			c.F[0].O.ShouldBe(o1);

			c.G.Count.ShouldBe(0);

			c.D.Keys.ShouldBe(new[] { 1, 5, -1 }, ignoreOrder: true);
			c.E.Keys.ShouldBe(new[] { o1, o2 }, ignoreOrder: true);
			c.F.Keys.ShouldBe(new[] { 0 });

			c.D.Values.ShouldBe(new[] { 3, 8, -1 }, ignoreOrder: true);
			c.E.Values.ShouldBe(new[] { o1, o2 }, ignoreOrder: true);
			c.F.Values.ShouldBe(new[] { new X { A = 2, O = o1 } });

			c.D[1] = 77;
			c.F.Clear();
			c.F[3] = new X { A = 7, O = o2 };
			Serialize();

			c.D.Clear();
			c.E.Clear();

			c.D[344] = 32;
			c.E[new object()] = new object();
			c.F[1] = new X();

			Deserialize();

			c.D.Count.ShouldBe(3);
			c.D[1].ShouldBe(77);
			c.F.Count.ShouldBe(1);
			c.F[3].A.ShouldBe(7);
			c.F[3].O.ShouldBe(o2);
		}

		private class C : Component
		{
			public Dictionary<int, int> D = new Dictionary<int, int>();
			public Dictionary<object, object> E = new Dictionary<object, object>();
			public Dictionary<int, X> F = new Dictionary<int, X>();
			public Dictionary<int, int> G = new Dictionary<int, int>();
		}

		private struct X
		{
			public int A;
			public object O;
		}
	}
}