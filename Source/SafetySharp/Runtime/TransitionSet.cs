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

namespace SafetySharp.Runtime
{
	using System;
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   Caches an activation-minimal set of transitions.
	/// </summary>
	internal sealed unsafe class TransitionSet : DisposableObject
	{
		private readonly Func<bool>[] _formulas;
		private readonly MemoryBuffer _stateBuffer = new MemoryBuffer();
		private readonly byte* _stateMemory;
		private readonly int _stateVectorSize;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly Transition* _transitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the successors are computed for.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		/// <param name="formulas">The formulas that should be checked for all successor states.</param>
		public TransitionSet(RuntimeModel model, int capacity, params Func<bool>[] formulas)
		{
			Requires.NotNull(model, nameof(model));
			Requires.NotNull(formulas, nameof(formulas));

			_stateVectorSize = model.StateVectorSize;
			_formulas = formulas;
			_transitionBuffer.Resize(capacity * sizeof(Transition), zeroMemory: false);
			_transitions = (Transition*)_transitionBuffer.Pointer;
			_stateBuffer.Resize(capacity * model.StateVectorSize, zeroMemory: true);
			_stateMemory = _stateBuffer.Pointer;
		}

		/// <summary>
		///   Gets the number of successor states.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		///   Gets the transition stored at the <paramref name="index" />.
		/// </summary>
		public Transition* this[int index] => &_transitions[index];

		/// <summary>
		///   Adds a transition to the <paramref name="model" />'s current state.
		/// </summary>
		/// <param name="model">The model the transition should be added for.</param>
		public void Add(RuntimeModel model)
		{
			// 1. Serialize the model's computed state; that is the successor state of the transition's source state
			//    modulo any changes resulting from notifications of fault activations
			var successorState = _stateMemory + _stateVectorSize * Count;
			var activatedFaults = GetActivatedFaults(model);
			model.Serialize(successorState);

			// 2. Determine whether there already is a transition to the successor state
			//var slot = GetFaultInfo(successorState);
			// Idea: use hash table that looks up fault info for each known successor state

			// if no fault info -> add state 
			// if fault info ->
			//         if subset of activated faults already known -> ignore successor
			//         otherwise, add new fault set, and add successor fault

			// execute activation notifications, serialize state again, and store state

			// 3. Execute fault activation notifications, serialize the updated state if necessary, and store the transition
			if (model.NotifyFaultActivations())
				model.Serialize(successorState);

			_transitions[Count] = new Transition
			{
				TargetState = successorState,
				ActivatedFaults = activatedFaults,
				Formulas = EvaluateFormulas()
			};

			++Count;
		}

		/// <summary>
		///   Clears the cache, removing all cached states.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			// TODO: clear fault info hash table
			Count = 0;
		}

		/// <summary>
		///   Gets the faults that were activated by the transition. The returned bit mask has a bit n set if fault n
		///   was activated.
		/// </summary>
		private static int GetActivatedFaults(RuntimeModel model)
		{
			var faults = model.Faults;
			var mask = 0;

			for (var i = 0; i < faults.Length; ++i)
				mask |= faults[i].IsActivated ? 1 << i : 0;

			return mask;
		}

		/// <summary>
		///   Evaluates all of the model's state formulas. The returned bit mask has a bit n set if formula n holds.
		/// </summary>
		private int EvaluateFormulas()
		{
			var mask = 0;

			for (var i = 0; i < _formulas.Length; ++i)
				mask |= _formulas[i]() ? 1 << i : 0;

			return mask;
		}

		/// <summary>
		///   Disposes the object, releasing all managed and unmanaged resources.
		/// </summary>
		/// <param name="disposing">If true, indicates that the object is disposed; otherwise, the object is finalized.</param>
		protected override void OnDisposing(bool disposing)
		{
			if (!disposing)
				return;

			_transitionBuffer.SafeDispose();
			_stateBuffer.SafeDispose();
		}

		/// <summary>
		///   Represents a transition.
		/// </summary>
		internal struct Transition
		{
			/// <summary>
			///   The transition's target state.
			/// </summary>
			public byte* TargetState;

			/// <summary>
			///   The faults activated by the transition.
			/// </summary>
			public int ActivatedFaults;

			/// <summary>
			///   The state formulas holding in the target state.
			/// </summary>
			public int Formulas;
		}
	}
}