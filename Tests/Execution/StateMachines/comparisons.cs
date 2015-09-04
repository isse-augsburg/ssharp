// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
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

namespace Tests.Execution.StateMachines
{
	using System;
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	public class Comparisons : TestObject
	{
		protected override void Check()
		{
			var sm = StateMachine.Create(17);
			(sm == 17).ShouldBe(true);
			(sm == 13).ShouldBe(false);
			(sm != 17).ShouldBe(false);
			(sm != 13).ShouldBe(true);

			(17 == sm).ShouldBe(true);
			(13 == sm).ShouldBe(false);
			(17 != sm).ShouldBe(false);
			(13 != sm).ShouldBe(true);

			sm = StateMachine.Create(E.A);
			(sm == E.A).ShouldBe(true);
			(sm == E.B).ShouldBe(false);
			(sm != E.A).ShouldBe(false);
			(sm != E.B).ShouldBe(true);

			(E.A == sm).ShouldBe(true);
			(E.B == sm).ShouldBe(false);
			(E.A != sm).ShouldBe(false);
			(E.B != sm).ShouldBe(true);

			sm = StateMachine.Create(E.B);
			(sm == E.B).ShouldBe(true);
			(sm == E.A).ShouldBe(false);
			(sm != E.B).ShouldBe(false);
			(sm != E.A).ShouldBe(true);

			(E.B == sm).ShouldBe(true);
			(E.A == sm).ShouldBe(false);
			(E.B != sm).ShouldBe(false);
			(E.A != sm).ShouldBe(true);

			var e = E.B;
			(sm == e).ShouldBe(true);
			(sm != e).ShouldBe(false);
			(e == sm).ShouldBe(true);
			(e != sm).ShouldBe(false);

			IConvertible o = E.B;
			(sm == o).ShouldBe(true);
			(sm != o).ShouldBe(false);
			(o == sm).ShouldBe(true);
			(o != sm).ShouldBe(false);
		}

		private enum E
		{
			A,
			B
		}
	}
}