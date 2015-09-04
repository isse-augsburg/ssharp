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

	public class MultipleTransition : TestObject
	{
		private readonly StateMachine _stateMachine = StateMachine.Create(S.A);
		private int _x;
		private int _getCount;

		private StateMachine GetStateMachine(int i)
		{
			if (i != 17)
				throw new InvalidOperationException();

			++_getCount;
			return _stateMachine;
		}

		private void Reset()
		{
			_x = 0;
		}

		protected override void Check()
		{
			for (var i = 0; i < 100; ++i)
			{
				var oldX = _x;

				GetStateMachine(17)
					.Transition(
						from: S.A,
						to: S.B)
					.Transition(
						from: S.B,
						to: S.A,
						guard: false)
					.Transition(
						from: S.C,
						to: S.A)
					.Transition(
						from: S.B,
						to: S.C,
						guard: _x < 10,
						action: () => ++_x)
					.Transition(
						from: S.B,
						to: S.C,
						guard: _x == 10,
						action: Reset);

				switch (i % 3)
				{
					case 0:
						(_stateMachine == S.B).ShouldBe(true);
						break;
					case 1:
						(_stateMachine == S.C).ShouldBe(true);
						if (oldX < 10)
							_x.ShouldBe(oldX + 1);
						else
							_x.ShouldBe(0);
						break;
					case 2:
						(_stateMachine == S.A).ShouldBe(true);
						break;
				}

				_getCount.ShouldBe(i + 1);
			}
		}

		private enum S
		{
			A,
			B,
			C
		}
	}
}