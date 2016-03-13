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

namespace Tests.Normalization.LineCounts
{
	using SafetySharp.Modeling;

	public class StateMachine : LineCountTestObject
	{
		private class C : Component
		{
			public enum S
			{
				A,
				B
			}

			private int _x;
			private readonly StateMachine<S> _state = S.A;

			public override void Update()
			{
				var a = 0;

				_state
					.Transition(
						from: S.A,
						to: S.B,
						guard: true,
						action: () => ++_x)
					.Transition(
						from: S.B,
						to: S.A,
						guard: true,
						action: () => ++_x);

				var b = a + 1;
				++b;
			}

			public class Z
			{
			}
		}

		protected override void CheckLines()
		{
			CheckField("_x", expectedLine: 37, occurrence: 0);
			CheckField("_state", expectedLine: 38, occurrence: 0);

			CheckVariableDeclaration("a", expectedLine: 42);
			CheckVariableDeclaration("b", expectedLine: 56);

			CheckMethod("Update", expectedLine: 40, occurrence: 0);

			CheckClass("Z", expectedLine: 60, occurrence: 0);
			CheckClass("StateMachine", expectedLine: 27, occurrence: 0);
			CheckClass("C", expectedLine: 29, occurrence: 0);
		}
	}
}