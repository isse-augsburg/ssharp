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

namespace Tests.Execution.ProvidedPorts.Indexers
{
	using Shouldly;
	using Utilities;

	internal abstract class X2 : TestComponent
	{
		protected int _x;
		protected virtual int this[int i] => i * 2;

		protected virtual int this[int i, int j]
		{
			get { return i + j; }
		}

		protected virtual int this[int i, int j, int k]
		{
			set { _x = i + j + k + value; }
		}

		protected virtual int this[int i, int j, int k, int l]
		{
			get { return i + j + k + l; }
			set { _x = i + j + k + l + value; }
		}

		protected abstract int this[int i, int j, int k, int l, int m] { get; }
	}

	internal class X3 : X2
	{
		protected override int this[int i] => i * 3;

		protected override int this[int i, int j]
		{
			get { return i * j; }
		}

		protected override int this[int i, int j, int k]
		{
			set { _x = i * j * k * value; }
		}

		protected override int this[int i, int j, int k, int l]
		{
			get { return i * j * k * l; }
			set { _x = i * j * k * l * value; }
		}

		protected override int this[int i, int j, int k, int l, int m] => i + j + l + k + m;

		protected override void Check()
		{
			base[2].ShouldBe(4);
			base[2, 3].ShouldBe(5);

			base[1, 2, 4] = 23;
			_x.ShouldBe(1 + 2 + 4 + 23);

			base[1, 2, 4, 8] = 23;
			_x.ShouldBe(1 + 2 + 4 + 8 + 23);
			base[1, 2, 4, 8].ShouldBe(1 + 2 + 4 + 8);

			base[2].ShouldBe(4);
			base[2, 3].ShouldBe(5);

			base[1, 2, 4] = 23;
			_x.ShouldBe(1 + 2 + 4 + 23);

			base[1, 2, 4, 8] = 23;
			_x.ShouldBe(1 + 2 + 4 + 8 + 23);
			base[1, 2, 4, 8].ShouldBe(1 + 2 + 4 + 8);

			this[2].ShouldBe(6);
			this[2, 4].ShouldBe(8);

			this[1, 2, 4] = 23;
			_x.ShouldBe(1 * 2 * 4 * 23);

			this[1, 2, 4, 8] = 23;
			_x.ShouldBe(1 * 2 * 4 * 8 * 23);
			this[1, 2, 4, 8].ShouldBe(1 * 2 * 4 * 8);

			this[2, 4, 7, 25, 2].ShouldBe(2 + 4 + 7 + 25 + 2);
		}
	}
}