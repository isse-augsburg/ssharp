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
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	public class SingleTransition : TestObject
	{
		protected override void Check()
		{
			var sm = StateMachine.Create(S.A);

			sm.Transition(
				from: S.A,
				to: S.B);

			(sm == S.B).ShouldBe(true);

			sm.Transition(
				from: S.B,
				to: S.A,
				guard: false);

			(sm == S.B).ShouldBe(true);

			sm.Transition(
				from: S.A,
				to: S.A);

			(sm == S.B).ShouldBe(true);

			var x = 0;
			sm.Transition(
				from: S.B,
				to: S.C,
				guard: x == 0,
				action: () => x = 17);

			(sm == S.C).ShouldBe(true);
			x.ShouldBe(17);
		}

		private enum S
		{
			A,
			B,
			C
		}
	}
}