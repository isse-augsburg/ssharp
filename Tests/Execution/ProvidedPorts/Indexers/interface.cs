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

namespace Tests.Execution.ProvidedPorts.Indexers
{
	using Shouldly;
	using Utilities;

	internal interface I
	{
		int this[int i] { get; set; }
		int this[int i, int j] { get; }
		int this[int i, int j, int k] { set; }
	}

	internal abstract class BaseInterface : TestComponent, I
	{
		public int _x;

		public int this[int i]
		{
			get { return i * 2; }
			set { _x = i * value; }
		}

		int I.this[int i, int j] => i * j;

		public virtual int this[int i, int j, int k]
		{
			set { _x = i * j * k * value; }
		}
	}

	internal class DerivedInterface : BaseInterface
	{
		public override int this[int i, int j, int k]
		{
			set { _x = i + j + k + value; }
		}

		protected override void Check()
		{
			this[4].ShouldBe(8);
			this[4] = 8;
			_x.ShouldBe(32);

			this[2, 3, 4] = 33;
			_x.ShouldBe(2 + 3 + 4 + 33);

			base[2, 3, 4] = 33;
			_x.ShouldBe(2 * 3 * 4 * 33);

			((I)this)[3, 5].ShouldBe(15);

			I x = this;

			x[4].ShouldBe(8);
			x[4] = 8;
			_x.ShouldBe(32);

			x[2, 3, 4] = 33;
			_x.ShouldBe(2 + 3 + 4 + 33);

			x[3, 5].ShouldBe(15);
		}
	}
}