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

namespace Tests.Execution.StateMachines
{
	using SafetySharp.Modeling;
	using Shouldly;
	using Utilities;

	internal class NestedTransitions : TestObject
	{
		private readonly StateMachine<S> _sm = new StateMachine<S>(S.A);

		private StateMachine<S> GetStateMachine() => _sm; 

		protected override void Check()
		{
			_sm.Transition(
				from: S.A,
				to: S.B,
				action: ChangeState);

			(_sm == S.C).ShouldBe(true);

			_sm.Transition(
				from: S.C,
				to: S.B,
				action: () =>
				{
					_sm.Transition(
						from: S.B,
						to: S.D);
				});

			(_sm == S.D).ShouldBe(true);

			_sm.Transition(
				from: S.D,
				to: S.B,
				action: () => _sm.Transition(from: S.B, to: S.A));

			(_sm == S.A).ShouldBe(true);

			GetStateMachine().Transition(
				from: S.A,
				to: S.B,
				action: () =>
				{
					if (_sm.State == S.B)
						return;

					_sm.Transition(
						from: S.B,
						to: S.A);
				});

			(_sm == S.B).ShouldBe(true);

			_sm.Transition(
				from: S.B,
				to: S.C,
				action: () =>
				{
					if (_sm.State == S.B)
						return;

					_sm.Transition(
						from: S.C,
						to: S.A);
				});

			(_sm == S.A).ShouldBe(true);
		}

		private void ChangeState()
		{
			GetStateMachine().Transition(
				from: S.B,
				to: S.C);
		}

		private enum S
		{
			A,
			B,
			C,
			D
		}
	}
}