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

namespace Tests.Serialization.RuntimeModels
{
	using System.Collections.Generic;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using Shouldly;
	using Utilities;

	internal class ObjectsInStruct : TestModel
	{
		private static bool _hasConstructorRun;

		protected override void Check()
		{
			var o0 = new V();
			var o1 = new V { Y = 1 };
			var o2 = new V { Y = 2 };
			var o3 = new V { Y = 3 };
			var o4 = new V { Y = 4 };
			var o5 = new V { Y = 5 };
			var o6 = new V { Y = 6 };
			var o7 = new V { Y = 7 };
			var o8 = new V { Y = 8 };
			var o9 = new V { Y = 9 };
			var o10 = new V { Y = 10 };
			var o11 = new V { Y = 11 };
			var o12 = new V { Y = 12 };
			var o13 = new V { Y = 13 };
			var o14 = new V { Y = 14 };
			var o15 = new V { Y = 15 };
			var c = new C
			{
				E = new Y { X = new X { O = o0 } },
				F = new X { O = o1 },
				G = new[] { new X { O = o2 } },
				H = new[] { new X { O = o3 } },
				I = new[] { new X { O = o4 } },
				J = new[] { new X { O = o5 } },
				K = new[] { new X { O = o6 } },
				GL = new List<X> { new X { O = o9 } },
				HL = new List<X> { new X { O = o10 } },
				IL = new List<X> { new X { O = o11 } },
				JL = new List<X> { new X { O = o12 } },
				KL = new List<X> { new X { O = o13 } }
			};

			c.L[0] = new X { O = o7 };
			c.M[0] = new X { O = o8 };
			c.LL.Add(new X { O = o14 });
			c.ML.Add(new X { O = o15 });

			var m = InitializeModel(c);

			_hasConstructorRun = false;
			Create(m);

			StateFormulas.ShouldBeEmpty();
			RootComponents.Length.ShouldBe(1);

			var root = RootComponents[0];
			root.ShouldBeOfType<C>();
			c = (C)root;
			c.E.X.O.Y.ShouldBe(0);
			c.E.Null.ShouldBe(null);
			c.F.O.Y.ShouldBe(1);
			c.G[0].O.Y.ShouldBe(2);
			c.H[0].O.Y.ShouldBe(3);
			c.I[0].O.Y.ShouldBe(4);
			c.J[0].O.Y.ShouldBe(5);
			c.K[0].O.Y.ShouldBe(6);
			c.L[0].O.Y.ShouldBe(7);
			c.M[0].O.Y.ShouldBe(8);
			c.GL[0].O.Y.ShouldBe(9);
			c.HL[0].O.Y.ShouldBe(10);
			c.IL[0].O.Y.ShouldBe(11);
			c.JL[0].O.Y.ShouldBe(12);
			c.KL[0].O.Y.ShouldBe(13);
			c.LL[0].O.Y.ShouldBe(14);
			c.ML[0].O.Y.ShouldBe(15);
			root.GetSubcomponents().ShouldBeEmpty();

			_hasConstructorRun.ShouldBe(false);
		}

		private class C : Component
		{
			public Y E;
			public X F;
			public X[] G;

			public List<X> GL;

			[Hidden]
			public X[] H;

			[Hidden]
			public List<X> HL;

			[Hidden(HideElements = true)]
			public X[] I;

			[Hidden(HideElements = true)]
			public List<X> IL;

			public C()
			{
				_hasConstructorRun = true;
			}

			[Hidden]
			public X[] J { get; set; }

			[Hidden(HideElements = true)]
			public X[] K { get; set; }

			[Hidden]
			public X[] L { get; } = new X[1];

			[Hidden(HideElements = true)]
			public X[] M { get; } = new X[1];

			[Hidden]
			public List<X> JL { get; set; }

			[Hidden(HideElements = true)]
			public List<X> KL { get; set; }

			[Hidden]
			public List<X> LL { get; } = new List<X>();

			[Hidden(HideElements = true)]
			public List<X> ML { get; } = new List<X>();
		}

		private struct X
		{
			public V O;
		}

		private struct Y
		{
			public X X;
			public V Null;
		}

		private class V
		{
			public int Y;
		}
	}
}