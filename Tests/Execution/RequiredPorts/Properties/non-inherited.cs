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

namespace Tests.Execution.RequiredPorts.Properties
{
	using System;
	using Shouldly;
	using Utilities;

	internal class X1 : TestComponent
	{
		private int _x;

		private extern int R1 { get; }
		private extern int R2 { set; }
		private extern int R3 { get; set; }

		private int P1 => _x / 2;

		private int P2
		{
			set { _x = value * 2; }
		}

		private int P3
		{
			get { return _x * 2; }
			set { _x = value / 2; }
		}

		protected override void Check()
		{
			Bind(nameof(R1), nameof(P1));
			Bind(nameof(R2), nameof(P2));
			Bind<Action<int>>(nameof(R3), nameof(P3));
			Bind<Func<int>>(nameof(R3), nameof(P3));

			_x = 10;
			R1.ShouldBe(5);

			R2 = 12;
			_x.ShouldBe(24);

			R3.ShouldBe(48);
			R3 = 12;
			_x.ShouldBe(6);
		}
	}
}