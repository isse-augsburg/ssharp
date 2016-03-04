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
	using System.Runtime.CompilerServices;
	using Utilities;

	/// <summary>
	///   Caches an activation-minimal set of transitions.
	/// </summary>
	internal sealed unsafe class TransitionSet : DisposableObject
	{
		private readonly MemoryBuffer _stateBuffer = new MemoryBuffer();
		private readonly byte* _stateMemory;
		private readonly int _stateVectorSize;
		private readonly MemoryBuffer _transitionBuffer = new MemoryBuffer();
		private readonly TransitionInfo* _transitions;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="model">The model the successors are computed for.</param>
		/// <param name="capacity">The maximum number of successors that can be cached.</param>
		public TransitionSet(RuntimeModel model, int capacity)
		{
			Requires.NotNull(model, nameof(model));

			_stateVectorSize = model.StateVectorSize;
			_transitionBuffer.Resize(capacity * sizeof(TransitionInfo), zeroMemory: false);
			_transitions = (TransitionInfo*)_transitionBuffer.Pointer;
			_stateBuffer.Resize(capacity * model.StateVectorSize, zeroMemory: true);
			_stateMemory = _stateBuffer.Pointer;
		}

		/// <summary>
		///   Gets the number of successor states.
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		///   Gets the first byte of the next state that can be stored.
		/// </summary>
		private byte* NextState => _stateMemory + _stateVectorSize * Count;

		/// <summary>
		///   Gets the transition stored at the <paramref name="index" />.
		/// </summary>
		public TransitionInfo* this[int index] => &_transitions[index];

		/// <summary>
		///   Adds a transition to the <paramref name="model" />'s current state.
		/// </summary>
		/// <param name="model">The model the transition should be added for.</param>
		public void Add(RuntimeModel model)
		{
			// 1. Serialize the model's computed state; that is the successor state of the transition's source state
			//    modulo any changes resulting from notifications of fault activations
			var successorState = NextState;
			model.Serialize(successorState);

			// 2. Determine whether there already is a transition to the successor state
			//var slot = GetFaultInfo(successorState);
			// Idea: use hash table that looks up fault info for each known successor state

			// if no fault info -> add state 
			// if fault info ->
			//         if subset of activated faults already known -> ignore successor
			//         otherwise, add new fault set, and add successor fault

			// execute activation notifications, serialize state again, and store state


			_transitions[Count] = new TransitionInfo { State = successorState };
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
		internal struct TransitionInfo
		{
			/// <summary>
			///   The transition's target state.
			/// </summary>
			public byte* State;

			/// <summary>
			///   The faults activated by the transition.
			/// </summary>
			public int Faults;

			/// <summary>
			///   The state formulas holding in the target state.
			/// </summary>
			public int Formulas;
		}
	}
}