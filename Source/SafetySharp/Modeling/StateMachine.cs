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

namespace SafetySharp.Modeling
{
	using System;
	using System.Globalization;
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   Represents a state machine that transitions between various states.
	/// </summary>
	public sealed class StateMachine
	{
		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		private StateMachine()
		{
		}

		/// <summary>
		///   Gets the state machine's choice object that is used to resolve nondeterministic transitions.
		/// </summary>
		internal Choice Choice { get; } = new Choice();

		/// <summary>
		///   Gets or sets the current state of the state machine.
		/// </summary>
		internal int State { get; set; }

		/// <summary>
		///   Transitions the state machine to the target state executing the <paramref name="action" />, provided that the state
		///   machine is in the source state and the <paramref name="guard" /> holds.
		/// </summary>
		/// <typeparam name="TSourceState">The type of the source state.</typeparam>
		/// <typeparam name="TTargetState">The type of the target state.</typeparam>
		/// <param name="from">The source state that should be left by the transition.</param>
		/// <param name="to">The target state that should be entered by the transition.</param>
		/// <param name="guard">
		///   The guard that determines whether the transition can be taken. <c>true</c> by default.
		/// </param>
		/// <param name="action">
		///   The action that should be executed when the transition is taken. A value of <c>null</c> indicates that
		///   no action should be performed when the transition is taken.
		/// </param>
		public StateMachine Transition<TSourceState, TTargetState>(TSourceState from, TTargetState to, bool guard = true, Action action = null)
			where TSourceState : struct, IConvertible
			where TTargetState : struct, IConvertible
		{
			Requires.CompilationTransformation();
			return this;
		}

		/// <summary>
		///   Creates a new state machine with the given <paramref name="initialState" />.
		/// </summary>
		/// <param name="initialState">The initial state of the state machine.</param>
		public static StateMachine Create<TState>(TState initialState)
			where TState : struct, IConvertible
		{
			return new StateMachine { State = initialState.ToInt32(CultureInfo.InvariantCulture) };
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is in the given <paramref name="state" />.
		/// </summary>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		/// <param name="state">The state the state machine should be in.</param>
		public static bool operator ==(StateMachine stateMachine, IConvertible state)
		{
			Requires.CompilationTransformation();
			return false;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is not in the given <paramref name="state" />.
		/// </summary>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		/// <param name="state">The state the state machine should not be in.</param>
		public static bool operator !=(StateMachine stateMachine, IConvertible state)
		{
			Requires.CompilationTransformation();
			return false;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is in the given <paramref name="state" />.
		/// </summary>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		/// <param name="state">The state the state machine should be in.</param>
		public static bool operator ==(StateMachine stateMachine, int state)
		{
			return stateMachine.State == state;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is not in the given <paramref name="state" />.
		/// </summary>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		/// <param name="state">The state the state machine should not be in.</param>
		public static bool operator !=(StateMachine stateMachine, int state)
		{
			return stateMachine.State != state;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is in the given <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the state machine should be in.</param>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		public static bool operator ==(IConvertible state, StateMachine stateMachine)
		{
			Requires.CompilationTransformation();
			return false;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is not in the given <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the state machine should not be in.</param>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		public static bool operator !=(IConvertible state, StateMachine stateMachine)
		{
			Requires.CompilationTransformation();
			return false;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is in the given <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the state machine should be in.</param>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		public static bool operator ==(int state, StateMachine stateMachine)
		{
			return stateMachine.State == state;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is not in the given <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the state machine should not be in.</param>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		public static bool operator !=(int state, StateMachine stateMachine)
		{
			return stateMachine.State != state;
		}

		/// <summary>
		///   Determines whether the specified object is equal to the current object.
		/// </summary>
		public override bool Equals(object obj)
		{
			return ReferenceEquals(this, obj);
		}

		/// <summary>
		///   Computes a hash for the current object.
		/// </summary>
		public override int GetHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}
	}
}