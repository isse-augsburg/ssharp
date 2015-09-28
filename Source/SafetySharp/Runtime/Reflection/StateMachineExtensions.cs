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
	///   Provides access to additional methods for <see cref="StateMachine{TState}" /> instances.
	/// </summary>
	public static class StateMachineExtensions
	{
		/// <summary>
		///   Changes the state of the <paramref name="stateMachine" /> to the <paramref name="newState" />.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine whose state should be changed.</param>
		/// <param name="newState">The new state of the state machine.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ChangeState<TState>(this StateMachine<TState> stateMachine, TState newState)
		{
			stateMachine.State = newState;
		}

		/// <summary>
		///   Gets the <paramref name="stateMachine" />'s <see cref="Choice" /> instance.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine the <see cref="Choice" /> instance should be returned for.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Choice GetChoice<TState>(this StateMachine<TState> stateMachine)
		{
			return stateMachine.Choice;
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="stateMachine" /> currently is in the given <paramref name="state" />.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine whose state should be checked.</param>
		/// <param name="state">The state that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInState<TState>(this StateMachine<TState> stateMachine, TState state)
		{
			return stateMachine == state;
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="stateMachine" /> currently is in <paramref name="state1" /> or
		///   <paramref name="state2" />.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine whose state should be checked.</param>
		/// <param name="state1">The first state that should be checked.</param>
		/// <param name="state2">The second state that should be checked.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInState<TState>(this StateMachine<TState> stateMachine, TState state1, TState state2)
		{
			return stateMachine == state1 || stateMachine == state2;
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="stateMachine" /> currently is in <paramref name="state1" />,
		///   <paramref name="state2" />, or <paramref name="state3" />.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine whose state should be checked.</param>
		/// <param name="state1">The first state that should be checked.</param>
		/// <param name="state2">The second state that should be checked.</param>
		/// <param name="state3">The third state that should be checked.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInState<TState>(this StateMachine<TState> stateMachine, TState state1, TState state2, TState state3)
		{
			return stateMachine == state1 || stateMachine == state2 || stateMachine == state3;
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="stateMachine" /> currently is in <paramref name="state1" />,
		///   <paramref name="state2" />, <paramref name="state3" />, or <paramref name="state4" />.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine whose state should be checked.</param>
		/// <param name="state1">The first state that should be checked.</param>
		/// <param name="state2">The second state that should be checked.</param>
		/// <param name="state3">The third state that should be checked.</param>
		/// <param name="state4">The fourth state that should be checked.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInState<TState>(this StateMachine<TState> stateMachine, TState state1, TState state2, TState state3, TState state4)
		{
			return stateMachine == state1 || stateMachine == state2 || stateMachine == state3 || stateMachine == state4;
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="stateMachine" /> currently is in <paramref name="state1" />,
		///   <paramref name="state2" />, <paramref name="state3" />, <paramref name="state4" />, or <paramref name="state5" />.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine whose state should be checked.</param>
		/// <param name="state1">The first state that should be checked.</param>
		/// <param name="state2">The second state that should be checked.</param>
		/// <param name="state3">The third state that should be checked.</param>
		/// <param name="state4">The fourth state that should be checked.</param>
		/// <param name="state5">The fifth state that should be checked.</param>
		/// <remarks>This method is a performance optimization.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInState<TState>(this StateMachine<TState> stateMachine, TState state1, TState state2, TState state3,
											 TState state4, TState state5)
		{
			return stateMachine == state1 || stateMachine == state2 || stateMachine == state3 || stateMachine == state4 || stateMachine == state5;
		}

		/// <summary>
		///   Gets a value indicating whether the <paramref name="stateMachine" /> currently is in any of the given
		///   <paramref name="states" />.
		/// </summary>
		/// <typeparam name="TState">The type of the state machine's states.</typeparam>
		/// <param name="stateMachine">The state machine whose state should be checked.</param>
		/// <param name="states">The states that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsInState<TState>(this StateMachine<TState> stateMachine, params TState[] states)
		{
			// Not using LINQ for performance reasons
			// ReSharper disable once LoopCanBeConvertedToQuery
			foreach (var state in states)
			{
				if (stateMachine == state)
					return true;
			}

			return false;
		}
	}
}