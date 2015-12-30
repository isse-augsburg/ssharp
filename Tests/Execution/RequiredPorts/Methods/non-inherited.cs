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

namespace Tests.Execution.RequiredPorts.Methods
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class X1 : TestComponent
	{
		private readonly X2 _b = new X2 { _i = 5 };
		private int _i;

		private X2 A { get; } = new X2 { _i = 4 };

		private int M(int i)
		{
			return i * _i;
		}

		private extern int N(int i);

		protected override void Check()
		{
			_i = 2;
			Bind(nameof(N), nameof(M));

			N(2).ShouldBe(4);
			N(10).ShouldBe(20);

			var x = new X1 { _i = 3 };
			Bind(nameof(x.N), nameof(x.M));

			x.N(2).ShouldBe(6);
			x.N(10).ShouldBe(30);

			Bind(nameof(A.N), nameof(A.M));

			A.N(2).ShouldBe(8);
			A.N(10).ShouldBe(40);

			Bind(nameof(_b.N), nameof(_b.M));

			_b.N(2).ShouldBe(10);
			_b.N(10).ShouldBe(50);

			Bind(nameof(this._b.N), nameof(this.A.M));

			this._b.N(2).ShouldBe(8);
			this._b.N(10).ShouldBe(40);
		}

		private class X2 : Component
		{
			public int _i;

			public int M(int i)
			{
				return i * _i;
			}

			public extern int N(int i);
		}
	}
}