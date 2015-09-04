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

namespace SafetySharp.Runtime.Reflection
{
	using System.Runtime.CompilerServices;
	using Modeling;

	/// <summary>
	///   Provides access to the internals of <see cref="StateMachine" /> instances.
	/// </summary>
	public static class StateMachineExtensions
	{
		/// <summary>
		///   Changes the state of the <paramref name="stateMachine" /> to the <paramref name="newState" />.
		/// </summary>
		/// <param name="stateMachine">The state machine whose state should be changed.</param>
		/// <param name="newState">The new state of the state machine.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ChangeState(this StateMachine stateMachine, int newState)
		{
			stateMachine.State = newState;
		}

		/// <summary>
		///   Gets the current state of the <paramref name="stateMachine" />.
		/// </summary>
		/// <param name="stateMachine">The state machine whose state should be returned.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetState(this StateMachine stateMachine)
		{
			return stateMachine.State;
		}

		/// <summary>
		///   Gets the <paramref name="stateMachine" />'s <see cref="Choice" /> instance.
		/// </summary>
		/// <param name="stateMachine">The state machine the <see cref="Choice" /> instance should be returned for.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Choice GetChoice(this StateMachine stateMachine)
		{
			return stateMachine.Choice;
		}
	}
}