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

namespace Tests.Execution.ProvidedPorts.Properties
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal abstract class X2 : TestComponent
	{
		protected int _x;
		protected virtual int P1 => _x * 2;
		protected virtual int P2 { get; } = 77;

		protected virtual int P3
		{
			get { return _x / 2; }
		}

		protected virtual int P4 { get; set; }

		protected virtual int P5
		{
			set { _x = value; }
		}

		protected virtual int P6
		{
			get { return _x * 2; }
			set { _x = value / 2; }
		}

		protected abstract int P7 { get; set; }
	}

	internal class X3 : X2
	{
		protected override int P1 => _x * 4;
		protected override int P2 { get; } = 771;

		protected override int P3
		{
			get { return _x / 4; }
		}

		protected override int P4 { get; set; }

		protected override int P5
		{
			set { _x = value + 1; }
		}

		protected override int P6
		{
			get { return _x * 4; }
			set { _x = value / 4; }
		}

		protected override void Check()
		{
			_x = 2;
			base.P1.ShouldBe(4);
			base.P3.ShouldBe(1);

			_x = 10;
			base.P1.ShouldBe(20);
			base.P3.ShouldBe(5);

			base.P2.ShouldBe(77);

			base.P4 = 9;
			base.P4.ShouldBe(9);

			base.P4 = 11;
			base.P4.ShouldBe(11);

			base.P5 = 13;
			_x.ShouldBe(13);

			base.P5 = 131;
			_x.ShouldBe(131);

			base.P6 = 16;
			base.P6.ShouldBe(16);

			base.P6 = 8;
			base.P6.ShouldBe(8);

			_x = 4;
			P1.ShouldBe(16);
			P3.ShouldBe(1);

			_x = 16;
			P1.ShouldBe(64);
			P3.ShouldBe(4);

			P2.ShouldBe(771);

			P4 = 19;
			P4.ShouldBe(19);
			base.P4.ShouldBe(11);

			P4 = 111;
			P4.ShouldBe(111);
			base.P4.ShouldBe(11);

			P5 = 13;
			_x.ShouldBe(14);

			P5 = 131;
			_x.ShouldBe(132);

			P6 = 16;
			_x.ShouldBe(4);
			P6.ShouldBe(16);

			P6 = 8;
			_x.ShouldBe(2);
			P6.ShouldBe(8);

			P7 = -1;
			P7.ShouldBe(-1);
		}

		protected override int P7 { get; set; }
	}
}