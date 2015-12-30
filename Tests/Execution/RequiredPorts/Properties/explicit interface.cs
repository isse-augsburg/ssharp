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
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal interface I1 : IComponent
	{
		[Required]
		int R1 { get; }

		[Required]
		int R2 { set; }

		[Required]
		int R3 { get; set; }

		[Provided]
		int P1 { get; }

		[Provided]
		int P2 { set; }

		[Provided]
		int P3 { get; set; }
	}

	internal class ExplicitInterface : TestObject
	{
		protected override void Check()
		{
			var c = new C();
			I1 i = c;

			Component.Bind(nameof(i.R1), nameof(i.P1));
			Component.Bind(nameof(i.R2), nameof(i.P2));
			Component.Bind<Action<int>>(nameof(i.R3), nameof(i.P3));
			Component.Bind<Func<int>>(nameof(i.R3), nameof(i.P3));

			c._x = 10;
			i.R1.ShouldBe(5);

			i.R2 = 12;
			c._x.ShouldBe(24);

			i.R3.ShouldBe(48);
			i.R3 = 12;
			c._x.ShouldBe(6);
		}

		private class C : Component, I1
		{
			extern int I1.R1 { get; }
			extern int I1.R2 { set; }
			extern int I1.R3 { get; set; }

			public int _x;

			int I1.P1 => _x / 2;

			int I1.P2
			{
				set { _x = value * 2; }
			}

			int I1.P3
			{
				get { return _x * 2; }
				set { _x = value / 2; }
			}
		}
	}
}