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
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   Represents a state machine that transitions between various states.
	/// </summary>
	/// <typeparam name="TState">The type of the state machine's states.</typeparam>
	public sealed class StateMachine<TState> : IInitializable
	{
		/// <summary>
		///   The initial states of the state machine.
		/// </summary>
		[NonDiscoverable]
		private readonly TState[] _initialStates;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="initialStates">The initial states of the state machine.</param>
		public StateMachine(params TState[] initialStates)
		{
			Requires.NotNull(initialStates, nameof(initialStates));
			Requires.That(initialStates.Length > 0, nameof(initialStates), "The state machine must have at least one initial state.");

			// Copy the initial states to the property in order to avoid external modifications
			_initialStates = initialStates.ToArray();
			State = _initialStates[0];
		}

		/// <summary>
		///   Gets the initial states of the state machine.
		/// </summary>
		public IEnumerable<TState> InitialStates => _initialStates;

		/// <summary>
		///   Gets the state machine's choice object that is used to resolve nondeterministic transitions.
		/// </summary>
		internal Choice Choice { get; } = new Choice();

		/// <summary>
		///   Gets the current state of the state machine.
		/// </summary>
		public TState State { get; internal set; }

		/// <summary>
		///   Nondeterministically chooses an initial state.
		/// </summary>
		void IInitializable.Initialize()
		{
			State = Choice.Choose(_initialStates);
		}

		/// <summary>
		///   Transitions the state machine to the target state executing the <paramref name="action" />, provided that the state
		///   machine is in the source state and the <paramref name="guard" /> holds.
		/// </summary>
		/// <param name="from">The source state that should be left by the transition.</param>
		/// <param name="to">The target state that should be entered by the transition.</param>
		/// <param name="guard">
		///   The guard that determines whether the transition can be taken. <c>true</c> by default.
		/// </param>
		/// <param name="action">
		///   The action that should be executed when the transition is taken. A value of <c>null</c> indicates that
		///   no action should be performed when the transition is taken.
		/// </param>
		public StateMachine<TState> Transition(TState from, TState to, bool guard = true, Action action = null)
		{
			Requires.CompilationTransformation();
			return this;
		}

		/// <summary>
		///   Transitions the state machine to any of the target states executing the <paramref name="action" />, provided that the
		///   state machine is in the source state and the <paramref name="guard" /> holds.
		/// </summary>
		/// <param name="from">The source state that should be left by the transition.</param>
		/// <param name="to">The target states that should be entered by the transition.</param>
		/// <param name="guard">
		///   The guard that determines whether the transition can be taken. <c>true</c> by default.
		/// </param>
		/// <param name="action">
		///   The action that should be executed when the transition is taken. A value of <c>null</c> indicates that
		///   no action should be performed when the transition is taken.
		/// </param>
		public StateMachine<TState> Transition(TState from, TState[] to, bool guard = true, Action action = null)
		{
			Requires.CompilationTransformation();
			return this;
		}

		/// <summary>
		///   Transitions the state machine to the target state executing the <paramref name="action" />, provided that the state
		///   machine is in any of the source states and the <paramref name="guard" /> holds.
		/// </summary>
		/// <param name="from">The source states that should be left by the transition.</param>
		/// <param name="to">The target state that should be entered by the transition.</param>
		/// <param name="guard">
		///   The guard that determines whether the transition can be taken. <c>true</c> by default.
		/// </param>
		/// <param name="action">
		///   The action that should be executed when the transition is taken. A value of <c>null</c> indicates that
		///   no action should be performed when the transition is taken.
		/// </param>
		public StateMachine<TState> Transition(TState[] from, TState to, bool guard = true, Action action = null)
		{
			Requires.CompilationTransformation();
			return this;
		}

		/// <summary>
		///   Transitions the state machine to any of the target states executing the <paramref name="action" />, provided that the
		///   state machine is in any of the source states and the <paramref name="guard" /> holds.
		/// </summary>
		/// <param name="from">The source states that should be left by the transition.</param>
		/// <param name="to">The target states that should be entered by the transition.</param>
		/// <param name="guard">
		///   The guard that determines whether the transition can be taken. <c>true</c> by default.
		/// </param>
		/// <param name="action">
		///   The action that should be executed when the transition is taken. A value of <c>null</c> indicates that
		///   no action should be performed when the transition is taken.
		/// </param>
		public StateMachine<TState> Transition(TState[] from, TState[] to, bool guard = true, Action action = null)
		{
			Requires.CompilationTransformation();
			return this;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is in the given <paramref name="state" />.
		/// </summary>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		/// <param name="state">The state the state machine should be in.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(StateMachine<TState> stateMachine, TState state)
		{
			return EqualityComparer<TState>.Default.Equals(stateMachine.State, state);
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is not in the given <paramref name="state" />.
		/// </summary>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		/// <param name="state">The state the state machine should not be in.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(StateMachine<TState> stateMachine, TState state)
		{
			return !(stateMachine == state);
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is in the given <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the state machine should be in.</param>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(TState state, StateMachine<TState> stateMachine)
		{
			return stateMachine == state;
		}

		/// <summary>
		///   Gets a value indicating whether the state machine is not in the given <paramref name="state" />.
		/// </summary>
		/// <param name="state">The state the state machine should not be in.</param>
		/// <param name="stateMachine">The state machine that should be checked.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(TState state, StateMachine<TState> stateMachine)
		{
			return !(stateMachine == state);
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