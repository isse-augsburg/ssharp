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

namespace Tests.Execution.StateMachines
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class ConflictingVariables : TestObject
	{
		private readonly StateMachine<S> _stateMachine = new StateMachine<S>(S.A);
		private int _x;
		private int _y;

		protected override void Check()
		{
			Transition();
			(_stateMachine == S.B).ShouldBe(true);
			_x.ShouldBe(0);
			_y.ShouldBe(8);

			Transition();
			(_stateMachine == S.A).ShouldBe(true);
			_x.ShouldBe(0);
			_y.ShouldBe(4);
		}

		private void Transition()
		{
			_stateMachine
				.Transition(
					from: S.A,
					to: S.B,
					action: () =>
					{
						var _x = 4;
						var x = _x / 2;
						_x += x;
						_y = _x + x;
					})
				.Transition(
					from: S.B,
					to: S.A,
					action: () =>
					{
						var _x = 2;
						var x = _x / 2;
						_x += x;
						_y = _x + x;
					});
		}

		private enum S
		{
			A,
			B,
			C
		}
	}
}