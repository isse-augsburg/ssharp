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

namespace Tests.Execution.Bindings.Components
{
	using Shouldly;
	using Utilities;

	namespace NewSystemNamespaceIsProblematic.System
	{
		using global::System;

		internal class Properties : TestComponent
		{
			private int _x;
			private extern int R1 { get; }
			private extern int R2 { set; }

			private int P1 => 17;

			private int P2
			{
				set { _x = value; }
			}

			int M(object o, bool b, char c, sbyte b1, byte b2, short s1, ushort s2, int i1, uint i3, long l1, ulong l2, float f, double d, string s,
				  decimal dec, IntPtr i)
			{
				return 1;
			}

			extern int N(object o, bool b, char c, sbyte b1, byte b2, short s1, ushort s2, int i1, uint i3, long l1, ulong l2, float f, double d,
						 string s, decimal dec, IntPtr i);

			protected override void Check()
			{
				Bind(nameof(R1), nameof(P1));
				Bind(nameof(R2), nameof(P2));
				Bind(nameof(N), nameof(M));

				R1.ShouldBe(17);
				R2 = 33;
				_x.ShouldBe(33);
			}
		}
	}
}